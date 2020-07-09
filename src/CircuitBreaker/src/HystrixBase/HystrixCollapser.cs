// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public abstract class HystrixCollapser<BatchReturnType, RequestResponseType, RequestArgumentType> : HystrixCollapserBase, IHystrixExecutable<RequestResponseType>
    {
        protected internal CancellationToken _token;

        private readonly RequestCollapserFactory _collapserFactory;
        private readonly HystrixRequestCache _requestCache;
        private readonly HystrixCollapserMetrics _metrics;

        protected HystrixCollapser()
            : this(null, RequestCollapserScope.REQUEST)
        {
        }

        protected HystrixCollapser(IHystrixCollapserKey collapserKey)
            : this(collapserKey, RequestCollapserScope.REQUEST)
        {
        }

        protected HystrixCollapser(IHystrixCollapserKey collapserKey, RequestCollapserScope scope)
            : this(new HystrixCollapserOptions(collapserKey, scope))
        {
        }

        protected HystrixCollapser(IHystrixCollapserOptions options)
            : this(options.CollapserKey, options.Scope, new RealCollapserTimer(), options, null)
        {
        }

        protected HystrixCollapser(IHystrixCollapserKey collapserKey, RequestCollapserScope scope, ICollapserTimer timer, IHystrixCollapserOptions options)
            : this(collapserKey, scope, timer, options, null)
        {
        }

        protected HystrixCollapser(IHystrixCollapserKey collapserKey, RequestCollapserScope scope, ICollapserTimer timer, IHystrixCollapserOptions optionsDefault, HystrixCollapserMetrics metrics)
        {
            if (collapserKey == null || collapserKey.Name.Trim().Equals(string.Empty))
            {
                string defaultKeyName = GetDefaultNameFromClass(GetType());
                collapserKey = HystrixCollapserKeyDefault.AsKey(defaultKeyName);
            }

            IHystrixCollapserOptions options = HystrixOptionsFactory.GetCollapserOptions(collapserKey, optionsDefault);
            _collapserFactory = new RequestCollapserFactory(collapserKey, scope, timer, options);
            _requestCache = HystrixRequestCache.GetInstance(collapserKey);

            if (metrics == null)
            {
                this._metrics = HystrixCollapserMetrics.GetInstance(collapserKey, options);
            }
            else
            {
                this._metrics = metrics;
            }

            HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCollapser(collapserKey, this._metrics, options);
        }

        public virtual IHystrixCollapserKey CollapserKey => _collapserFactory.CollapserKey;

        public virtual RequestCollapserScope Scope => _collapserFactory.Scope;

        public virtual HystrixCollapserMetrics Metrics => _metrics;

        public abstract RequestArgumentType RequestArgument { get; }

        private IHystrixCollapserOptions Properties => _collapserFactory.Properties;

        public RequestResponseType Execute()
        {
            try
            {
                return ExecuteAsync().GetAwaiter().GetResult();
            }
            catch (Exception e) when (!(e is HystrixRuntimeException))
            {
                throw DecomposeException(e);
            }
        }

        public Task<RequestResponseType> ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        public Task<RequestResponseType> ExecuteAsync(CancellationToken token)
        {
            _token = token;
            Task<RequestResponseType> toStart = ToTask();
            return toStart;
        }

        public Task<RequestResponseType> ToTask()
        {
            RequestCollapser<BatchReturnType, RequestResponseType, RequestArgumentType> requestCollapser = _collapserFactory.GetRequestCollapser(this);
            CollapsedRequest<RequestResponseType, RequestArgumentType> request = null;

            if (AddCacheEntryIfAbsent(CacheKey, out HystrixCachedTask<RequestResponseType> entry))
            {
                _metrics.MarkResponseFromCache();
                var origTask = entry.CachedTask;
                request = entry.CachedTask.AsyncState as CollapsedRequest<RequestResponseType, RequestArgumentType>;
                request.AddLinkedToken(_token);
                var continued = origTask.ContinueWith<RequestResponseType>(
                    (parent) =>
                    {
                        if (parent.AsyncState is CollapsedRequest<RequestResponseType, RequestArgumentType> req)
                        {
                            if (req.Exception != null)
                            {
                                throw req.Exception;
                            }

                            return req.Response;
                        }
                        else
                        {
                            throw new InvalidOperationException("Missing AsyncState from parent task");
                        }
                    },
                    _token,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Current);
                return continued;
            }

            try
            {
                request = requestCollapser.SubmitRequest(RequestArgument, _token);
                entry.CachedTask = request.CompletionSource.Task;
                return entry.CachedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException<RequestResponseType>(ex);
            }
        }

        public IObservable<RequestResponseType> Observe()
        {
            ReplaySubject<RequestResponseType> subject = new ReplaySubject<RequestResponseType>();
            IObservable<RequestResponseType> observable = ToObservable();
            var disposable = observable.Subscribe(subject);
            return subject.Finally(() => disposable.Dispose());
        }

        public IObservable<RequestResponseType> Observe(CancellationToken token)
        {
            ReplaySubject<RequestResponseType> subject = new ReplaySubject<RequestResponseType>();
            IObservable<RequestResponseType> observable = ToObservable();
            observable.Subscribe(subject, token);
            return observable;
        }

        public IObservable<RequestResponseType> ToObservable()
        {
            IObservable<RequestResponseType> observable = Observable.FromAsync<RequestResponseType>((ct) =>
            {
                _token = ct;
                Task<RequestResponseType> toStart = ToTask();
                return toStart;
            });
            return observable;
        }

        protected abstract HystrixCommand<BatchReturnType> CreateCommand(ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> requests);

        protected abstract void MapResponseToRequests(BatchReturnType batchResponse, ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> requests);

        protected virtual ICollection<ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>>> ShardRequests(ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> requests)
        {
            return new List<ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>>>() { requests };
        }

        protected bool AddCacheEntryIfAbsent(string cacheKey, out HystrixCachedTask<RequestResponseType> entry)
        {
            var newEntry = new HystrixCachedTask<RequestResponseType>();
            if (Properties.RequestCacheEnabled && cacheKey != null)
            {
                entry = _requestCache.PutIfAbsent(cacheKey, newEntry);
                if (entry != null)
                {
                    return true;
                }
            }

            entry = newEntry;
            return false;
        }

        protected virtual string CacheKey => null;

        internal static void Reset()
        {
            RequestCollapserFactory.Reset();
        }

        internal ICollection<ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>>> DoShardRequests(ICollection<CollapsedRequest<RequestResponseType, RequestArgumentType>> requests)
        {
            ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> theRequests = new List<ICollapsedRequest<RequestResponseType, RequestArgumentType>>(requests);
            ICollection<ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>>> shards = ShardRequests(theRequests);
            _metrics.MarkShards(shards.Count);
            return shards;
        }

        internal HystrixCommand<BatchReturnType> DoCreateObservableCommand(ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> requests)
        {
            HystrixCommand<BatchReturnType> command = CreateCommand(requests);

            command.MarkAsCollapsedCommand(CollapserKey, requests.Count);
            _metrics.MarkBatch(requests.Count);

            return command;
        }

        internal void DoMapResponseToRequests(BatchReturnType batchResponse, ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> requests)
        {
            MapResponseToRequests(batchResponse, requests);
        }

        protected virtual Exception DecomposeException(Exception e)
        {
            string message = GetType() + " HystrixCollapser failed while executing.";

            // logger.debug(message, e); // debug only since we're throwing the exception and someone higher will do something with it
            return new HystrixRuntimeException(FailureType.COMMAND_EXCEPTION, GetType(), message, e, null);
        }

        private static string GetDefaultNameFromClass(Type cls)
        {
            if (_defaultNameCache.TryGetValue(cls, out string fromCache))
            {
                return fromCache;
            }

            string name = cls.Name;
            _defaultNameCache.TryAdd(cls, name);
            return name;
        }
    }
}
