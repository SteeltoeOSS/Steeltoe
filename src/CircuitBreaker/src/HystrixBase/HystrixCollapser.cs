// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public enum RequestCollapserScope
    {
        REQUEST,
        GLOBAL
    }

    public abstract class HystrixCollapser<BatchReturnType, RequestResponseType, RequestArgumentType> : IHystrixExecutable<RequestResponseType>, IHystrixObservable<RequestResponseType>
    {
        protected internal CancellationToken _token;

        private readonly RequestCollapserFactory collapserFactory;
        private readonly HystrixRequestCache requestCache;
        private readonly HystrixCollapserMetrics metrics;

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
            this.collapserFactory = new RequestCollapserFactory(collapserKey, scope, timer, options);
            this.requestCache = HystrixRequestCache.GetInstance(collapserKey);

            if (metrics == null)
            {
                this.metrics = HystrixCollapserMetrics.GetInstance(collapserKey, options);
            }
            else
            {
                this.metrics = metrics;
            }

            HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCollapser(collapserKey, this.metrics, options);
        }

        public virtual IHystrixCollapserKey CollapserKey
        {
            get { return collapserFactory.CollapserKey; }
        }

        public virtual RequestCollapserScope Scope
        {
            get { return collapserFactory.Scope; }
        }

        public virtual HystrixCollapserMetrics Metrics
        {
            get { return metrics; }
        }

        public abstract RequestArgumentType RequestArgument { get; }

        private IHystrixCollapserOptions Properties
        {
            get { return collapserFactory.Properties; }
        }

        public RequestResponseType Execute()
        {
            try
            {
                var task = ExecuteAsync();
                var result = task.Result;
                return result;
            }
            catch (Exception e)
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
            this._token = token;
            Task<RequestResponseType> toStart = ToTask();
            return toStart;
        }

        public Task<RequestResponseType> ToTask()
        {
            RequestCollapser<BatchReturnType, RequestResponseType, RequestArgumentType> requestCollapser = collapserFactory.GetRequestCollapser(this);
            CollapsedRequest<RequestResponseType, RequestArgumentType> request = null;
            HystrixCachedTask<RequestResponseType> entry = null;

            if (AddCacheEntryIfAbsent(CacheKey, out entry))
            {
                metrics.MarkResponseFromCache();
                var origTask = entry.CachedTask;
                request = entry.CachedTask.AsyncState as CollapsedRequest<RequestResponseType, RequestArgumentType>;
                request.AddLinkedToken(_token);
                var continued = origTask.ContinueWith<RequestResponseType>(
                    (parent) =>
                    {
                        CollapsedRequest<RequestResponseType, RequestArgumentType> req =
                            parent.AsyncState as CollapsedRequest<RequestResponseType, RequestArgumentType>;
                        if (req != null)
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
                this._token = ct;
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
            entry = null;
            var newEntry = new HystrixCachedTask<RequestResponseType>();
            if (Properties.RequestCacheEnabled && cacheKey != null)
            {
                entry = requestCache.PutIfAbsent<HystrixCachedTask<RequestResponseType>>(cacheKey, newEntry);
                if (entry != null)
                {
                    return true;
                }
            }

            entry = newEntry;
            return false;
        }

        protected virtual string CacheKey
        {
            get { return null; }
        }

        internal static void Reset()
        {
            RequestCollapserFactory.Reset();
        }

        private static string GetDefaultNameFromClass(Type cls)
        {
            if (defaultNameCache.TryGetValue(cls, out string fromCache))
            {
                return fromCache;
            }

            string name = cls.Name;
            defaultNameCache.TryAdd(cls, name);
            return name;
        }

        // this is a micro-optimization but saves about 1-2microseconds (on 2011 MacBook Pro)
        // on the repetitive string processing that will occur on the same classes over and over again
        private static ConcurrentDictionary<Type, string> defaultNameCache = new ConcurrentDictionary<Type, string>();

        internal ICollection<ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>>> DoShardRequests(ICollection<CollapsedRequest<RequestResponseType, RequestArgumentType>> requests)
        {
            ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> theRequests = new List<ICollapsedRequest<RequestResponseType, RequestArgumentType>>(requests);
            ICollection<ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>>> shards = this.ShardRequests(theRequests);
            this.metrics.MarkShards(shards.Count);
            return shards;
        }

        internal HystrixCommand<BatchReturnType> DoCreateObservableCommand(ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> requests)
        {
            HystrixCommand<BatchReturnType> command = this.CreateCommand(requests);

            command.MarkAsCollapsedCommand(this.CollapserKey, requests.Count);
            this.metrics.MarkBatch(requests.Count);

            return command;
        }

        internal void DoMapResponseToRequests(BatchReturnType batchResponse, ICollection<ICollapsedRequest<RequestResponseType, RequestArgumentType>> requests)
        {
            this.MapResponseToRequests(batchResponse, requests);
        }

        protected virtual Exception DecomposeException(Exception e)
        {
            if (e is HystrixRuntimeException)
            {
                return (HystrixRuntimeException)e;
            }

            // if we have an exception we know about we'll throw it directly without the wrapper exception
            if (e.InnerException is HystrixRuntimeException)
            {
                return (HystrixRuntimeException)e.InnerException;
            }

            string message = GetType() + " HystrixCollapser failed while executing.";

            // logger.debug(message, e); // debug only since we're throwing the exception and someone higher will do something with it
            return new HystrixRuntimeException(FailureType.COMMAND_EXCEPTION, this.GetType(), message, e, null);
        }
    }
}
