// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Integration.Util
{
    public class ErrorHandlingMessageHandlingRunnable : IMessageHandlingRunnable
    {
        private IMessageHandlingRunnable _runnable;
        private IErrorHandler _errorHandler;

        public ErrorHandlingMessageHandlingRunnable(IMessageHandlingRunnable runnable, IErrorHandler errorHandler)
        {
            if (runnable == null)
            {
                throw new ArgumentNullException(nameof(runnable));
            }

            if (errorHandler == null)
            {
                throw new ArgumentNullException(nameof(errorHandler));
            }

            _runnable = runnable;
            _errorHandler = errorHandler;
        }

        public IMessage Message => _runnable.Message;

        public IMessageHandler MessageHandler => _runnable.MessageHandler;

        public bool Run()
        {
            try
            {
                return _runnable.Run();
            }
            catch (Exception e)
            {
                _errorHandler.HandleError(e);
                return false;
            }
        }
    }
}

// IMessageHandlingRunnable
