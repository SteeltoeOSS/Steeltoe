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
