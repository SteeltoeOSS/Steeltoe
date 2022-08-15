// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser;

public class CollapsedRequest<TRequestResponse, TRequestArgument> : ICollapsedRequest<TRequestResponse, TRequestArgument>
{
    private readonly ConcurrentQueue<CancellationToken> _linkedTokens = new();
    private TRequestResponse _response;
    private Exception _exception;
    private bool _complete;

    internal CancellationToken Token { get; }

    internal TaskCompletionSource<TRequestResponse> CompletionSource { get; set; }

    public TRequestArgument Argument { get; }

    public bool Complete
    {
        get => _complete;

        set
        {
            _complete = value;

            if (!CompletionSource.Task.IsCompleted)
            {
                Response = default;
            }
        }
    }

    public Exception Exception
    {
        get => _exception;

        set
        {
            _exception = value;
            _complete = true;

            if (!CompletionSource.TrySetException(value))
            {
                throw new InvalidOperationException($"Task has already terminated so exception can not be set : {value}");
            }
        }
    }

    public TRequestResponse Response
    {
        get => _response;

        set
        {
            _response = value;
            _complete = true;

            if (!CompletionSource.TrySetResult(value))
            {
                throw new InvalidOperationException($"Task has already terminated so response can not be set : {value}");
            }
        }
    }

    internal CollapsedRequest(TRequestArgument arg, CancellationToken token)
    {
        Argument = arg;
        Token = token;
        CompletionSource = null;
        _response = default;
        _exception = null;
        _complete = false;
    }

    internal void AddLinkedToken(CancellationToken token)
    {
        _linkedTokens.Enqueue(token);
    }

    internal void SetExceptionIfResponseNotReceived(Exception e)
    {
        if (!_complete)
        {
            Exception = e;
        }
    }

    internal Exception SetExceptionIfResponseNotReceived(Exception e, string exceptionMessage)
    {
        Exception newException = e;

        if (!_complete)
        {
            if (e == null)
            {
                newException = new InvalidOperationException(exceptionMessage);
            }

            SetExceptionIfResponseNotReceived(newException);
        }

        // return any exception that was generated
        return newException;
    }

    internal bool IsRequestCanceled()
    {
        foreach (CancellationToken linkedToken in _linkedTokens)
        {
            if (!linkedToken.IsCancellationRequested)
            {
                return false;
            }
        }

        // All linked tokens have been cancelled
        return Token.IsCancellationRequested;
    }
}
