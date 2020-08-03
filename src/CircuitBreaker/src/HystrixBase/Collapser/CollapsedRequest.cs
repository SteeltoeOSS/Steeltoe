// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Collapser
{
    public class CollapsedRequest<RequestResponseType, RequestArgumentType> : ICollapsedRequest<RequestResponseType, RequestArgumentType>
    {
        private readonly ConcurrentQueue<CancellationToken> _linkedTokens = new ConcurrentQueue<CancellationToken>();
        private RequestResponseType _response;
        private Exception _exception;
        private bool _complete;

        internal CollapsedRequest(RequestArgumentType arg, CancellationToken token)
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

        internal CancellationToken Token { get; }

        internal TaskCompletionSource<RequestResponseType> CompletionSource { get; set; }

        internal void SetExceptionIfResponseNotReceived(Exception e)
        {
            if (!_complete)
            {
                Exception = e;
            }
        }

        internal Exception SetExceptionIfResponseNotReceived(Exception e, string exceptionMessage)
        {
            var newException = e;

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
            foreach (var linkedToken in _linkedTokens)
            {
                if (!linkedToken.IsCancellationRequested)
                {
                    return false;
                }
            }

            // All linked tokens have been cancelled
            return Token.IsCancellationRequested;
        }

        #region ICollapsedRequest
        public RequestArgumentType Argument { get; }

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
                    throw new InvalidOperationException("Task has already terminated so exectpion can not be set : " + value);
                }
            }
        }

        public RequestResponseType Response
        {
            get => _response;

            set
            {
                _response = value;
                _complete = true;
                if (!CompletionSource.TrySetResult(value))
                {
                    throw new InvalidOperationException("Task has already terminated so response can not be set : " + value);
                }
            }
        }

        #endregion ICollapsedRequest
    }
}
