// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix;

public abstract class HystrixCollapser<TBatchReturn, TRequestResponse, TRequestArgument> : HystrixCollapserBase, IHystrixExecutable<TRequestResponse>
{
    private readonly RequestCollapserFactory _collapserFactory;
    private readonly HystrixRequestCache _requestCache;
    private readonly HystrixCollapserMetrics _metrics;
    protected internal CancellationToken Token;

    private IHystrixCollapserOptions Properties => _collapserFactory.Properties;

    protected virtual string CacheKey => null;

    public virtual IHystrixCollapserKey CollapserKey => _collapserFactory.CollapserKey;

    public virtual RequestCollapserScope Scope => _collapserFactory.Scope;

    public virtual HystrixCollapserMetrics Metrics => _metrics;

    public abstract TRequestArgument RequestArgument { get; }

    protected HystrixCollapser()
        : this(null, RequestCollapserScope.Request)
    {
    }

    protected HystrixCollapser(IHystrixCollapserKey collapserKey)
        : this(collapserKey, RequestCollapserScope.Request)
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

    protected HystrixCollapser(IHystrixCollapserKey collapserKey, RequestCollapserScope scope, ICollapserTimer timer, IHystrixCollapserOptions optionsDefault,
        HystrixCollapserMetrics metrics)
    {
        if (collapserKey == null || string.IsNullOrWhiteSpace(collapserKey.Name))
        {
            string defaultKeyName = GetDefaultNameFromClass(GetType());
            collapserKey = HystrixCollapserKeyDefault.AsKey(defaultKeyName);
        }

        IHystrixCollapserOptions options = HystrixOptionsFactory.GetCollapserOptions(collapserKey, optionsDefault);
        _collapserFactory = new RequestCollapserFactory(collapserKey, scope, timer, options);
        _requestCache = HystrixRequestCache.GetInstance(collapserKey);
        _metrics = metrics ?? HystrixCollapserMetrics.GetInstance(collapserKey, options);

        HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCollapser(collapserKey, _metrics, options);
    }

    public TRequestResponse Execute()
    {
        try
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }
        catch (Exception e) when (e is not HystrixRuntimeException)
        {
            throw DecomposeException(e);
        }
    }

    public Task<TRequestResponse> ExecuteAsync()
    {
        return ExecuteAsync(CancellationToken.None);
    }

    public Task<TRequestResponse> ExecuteAsync(CancellationToken token)
    {
        Token = token;
        Task<TRequestResponse> toStart = ToTaskAsync();
        return toStart;
    }

    public Task<TRequestResponse> ToTaskAsync()
    {
        RequestCollapser<TBatchReturn, TRequestResponse, TRequestArgument> requestCollapser = _collapserFactory.GetRequestCollapser(this);
        CollapsedRequest<TRequestResponse, TRequestArgument> request = null;

        if (AddCacheEntryIfAbsent(CacheKey, out HystrixCachedTask<TRequestResponse> entry))
        {
            _metrics.MarkResponseFromCache();
            Task<TRequestResponse> origTask = entry.CachedTask;
            request = entry.CachedTask.AsyncState as CollapsedRequest<TRequestResponse, TRequestArgument>;
            request.AddLinkedToken(Token);

            Task<TRequestResponse> continued = origTask.ContinueWith(parent =>
            {
                if (parent.AsyncState is CollapsedRequest<TRequestResponse, TRequestArgument> req)
                {
                    if (req.Exception != null)
                    {
                        throw req.Exception;
                    }

                    return req.Response;
                }

                throw new InvalidOperationException("Missing AsyncState from parent task");
            }, Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);

            return continued;
        }

        try
        {
            request = requestCollapser.SubmitRequest(RequestArgument, Token);
            entry.CachedTask = request.CompletionSource.Task;
            return entry.CachedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException<TRequestResponse>(ex);
        }
    }

    public IObservable<TRequestResponse> Observe()
    {
        var subject = new ReplaySubject<TRequestResponse>();
        IObservable<TRequestResponse> observable = ToObservable();
        IDisposable disposable = observable.Subscribe(subject);
        return subject.Finally(() => disposable.Dispose());
    }

    public IObservable<TRequestResponse> Observe(CancellationToken token)
    {
        var subject = new ReplaySubject<TRequestResponse>();
        IObservable<TRequestResponse> observable = ToObservable();
        observable.Subscribe(subject, token);
        return observable;
    }

    public IObservable<TRequestResponse> ToObservable()
    {
        IObservable<TRequestResponse> observable = Observable.FromAsync(ct =>
        {
            Token = ct;
            Task<TRequestResponse> toStart = ToTaskAsync();
            return toStart;
        });

        return observable;
    }

    protected abstract HystrixCommand<TBatchReturn> CreateCommand(ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests);

    protected abstract void MapResponseToRequests(TBatchReturn batchResponse, ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests);

    protected virtual ICollection<ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>>> ShardRequests(
        ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests)
    {
        return new List<ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>>>
        {
            requests
        };
    }

    protected bool AddCacheEntryIfAbsent(string cacheKey, out HystrixCachedTask<TRequestResponse> entry)
    {
        var newEntry = new HystrixCachedTask<TRequestResponse>();

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

    internal static void Reset()
    {
        RequestCollapserFactory.Reset();
    }

    internal ICollection<ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>>> DoShardRequests(
        ICollection<CollapsedRequest<TRequestResponse, TRequestArgument>> requests)
    {
        ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> theRequests =
            new List<ICollapsedRequest<TRequestResponse, TRequestArgument>>(requests);

        ICollection<ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>>> shards = ShardRequests(theRequests);
        _metrics.MarkShards(shards.Count);
        return shards;
    }

    internal HystrixCommand<TBatchReturn> DoCreateObservableCommand(ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests)
    {
        HystrixCommand<TBatchReturn> command = CreateCommand(requests);

        command.MarkAsCollapsedCommand(CollapserKey, requests.Count);
        _metrics.MarkBatch(requests.Count);

        return command;
    }

    internal void DoMapResponseToRequests(TBatchReturn batchResponse, ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests)
    {
        MapResponseToRequests(batchResponse, requests);
    }

    protected virtual Exception DecomposeException(Exception e)
    {
        string message = $"{GetType()} HystrixCollapser failed while executing.";

        // logger.debug(message, e); // debug only since we're throwing the exception and someone higher will do something with it
        return new HystrixRuntimeException(FailureType.CommandException, GetType(), message, e, null);
    }

    private static string GetDefaultNameFromClass(Type cls)
    {
        if (DefaultNameCache.TryGetValue(cls, out string fromCache))
        {
            return fromCache;
        }

        string name = cls.Name;
        DefaultNameCache.TryAdd(cls, name);
        return name;
    }
}
