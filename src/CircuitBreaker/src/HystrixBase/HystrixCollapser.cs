// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix;

public abstract class HystrixCollapser<TBatchReturn, TRequestResponse, TRequestArgument> : HystrixCollapserBase, IHystrixExecutable<TRequestResponse>
{
    protected internal CancellationToken Token;

    private readonly RequestCollapserFactory _collapserFactory;
    private readonly HystrixRequestCache _requestCache;
    private readonly HystrixCollapserMetrics _metrics;

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

    protected HystrixCollapser(IHystrixCollapserKey collapserKey, RequestCollapserScope scope, ICollapserTimer timer, IHystrixCollapserOptions optionsDefault, HystrixCollapserMetrics metrics)
    {
        if (collapserKey == null || string.IsNullOrWhiteSpace(collapserKey.Name))
        {
            var defaultKeyName = GetDefaultNameFromClass(GetType());
            collapserKey = HystrixCollapserKeyDefault.AsKey(defaultKeyName);
        }

        var options = HystrixOptionsFactory.GetCollapserOptions(collapserKey, optionsDefault);
        _collapserFactory = new RequestCollapserFactory(collapserKey, scope, timer, options);
        _requestCache = HystrixRequestCache.GetInstance(collapserKey);
        _metrics = metrics ?? HystrixCollapserMetrics.GetInstance(collapserKey, options);

        HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCollapser(collapserKey, _metrics, options);
    }

    public virtual IHystrixCollapserKey CollapserKey => _collapserFactory.CollapserKey;

    public virtual RequestCollapserScope Scope => _collapserFactory.Scope;

    public virtual HystrixCollapserMetrics Metrics => _metrics;

    public abstract TRequestArgument RequestArgument { get; }

    private IHystrixCollapserOptions Properties => _collapserFactory.Properties;

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
        this.Token = token;
        var toStart = ToTask();
        return toStart;
    }

    public Task<TRequestResponse> ToTask()
    {
        var requestCollapser = _collapserFactory.GetRequestCollapser(this);
        CollapsedRequest<TRequestResponse, TRequestArgument> request = null;

        if (AddCacheEntryIfAbsent(CacheKey, out var entry))
        {
            _metrics.MarkResponseFromCache();
            var origTask = entry.CachedTask;
            request = entry.CachedTask.AsyncState as CollapsedRequest<TRequestResponse, TRequestArgument>;
            request.AddLinkedToken(Token);
            var continued = origTask.ContinueWith(
                parent =>
                {
                    if (parent.AsyncState is CollapsedRequest<TRequestResponse, TRequestArgument> req)
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
                Token,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Current);
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
        var observable = ToObservable();
        var disposable = observable.Subscribe(subject);
        return subject.Finally(() => disposable.Dispose());
    }

    public IObservable<TRequestResponse> Observe(CancellationToken token)
    {
        var subject = new ReplaySubject<TRequestResponse>();
        var observable = ToObservable();
        observable.Subscribe(subject, token);
        return observable;
    }

    public IObservable<TRequestResponse> ToObservable()
    {
        var observable = Observable.FromAsync(ct =>
        {
            Token = ct;
            var toStart = ToTask();
            return toStart;
        });
        return observable;
    }

    protected abstract HystrixCommand<TBatchReturn> CreateCommand(ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests);

    protected abstract void MapResponseToRequests(TBatchReturn batchResponse, ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests);

    protected virtual ICollection<ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>>> ShardRequests(ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests)
    {
        return new List<ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>>> { requests };
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

    protected virtual string CacheKey => null;

    internal static void Reset()
    {
        RequestCollapserFactory.Reset();
    }

    internal ICollection<ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>>> DoShardRequests(ICollection<CollapsedRequest<TRequestResponse, TRequestArgument>> requests)
    {
        ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> theRequests = new List<ICollapsedRequest<TRequestResponse, TRequestArgument>>(requests);
        var shards = ShardRequests(theRequests);
        _metrics.MarkShards(shards.Count);
        return shards;
    }

    internal HystrixCommand<TBatchReturn> DoCreateObservableCommand(ICollection<ICollapsedRequest<TRequestResponse, TRequestArgument>> requests)
    {
        var command = CreateCommand(requests);

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
        var message = $"{GetType()} HystrixCollapser failed while executing.";

        // logger.debug(message, e); // debug only since we're throwing the exception and someone higher will do something with it
        return new HystrixRuntimeException(FailureType.CommandException, GetType(), message, e, null);
    }

    private static string GetDefaultNameFromClass(Type cls)
    {
        if (DefaultNameCache.TryGetValue(cls, out var fromCache))
        {
            return fromCache;
        }

        var name = cls.Name;
        DefaultNameCache.TryAdd(cls, name);
        return name;
    }
}
