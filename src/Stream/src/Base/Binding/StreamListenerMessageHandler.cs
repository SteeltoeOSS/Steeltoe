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

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Invocation;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding
{
    public class StreamListenerMessageHandler : AbstractReplyProducingMessageHandler
    {
        private readonly IInvocableHandlerMethod _invocableHandlerMethod;

        private readonly bool _copyHeaders;

        public StreamListenerMessageHandler(IApplicationContext context, IInvocableHandlerMethod invocableHandlerMethod, bool copyHeaders, IList<string> notPropagatedHeaders)
            : base(context)
        {
            _invocableHandlerMethod = invocableHandlerMethod;
            _copyHeaders = copyHeaders;
            NotPropagatedHeaders = notPropagatedHeaders;
        }

        public bool IsVoid
        {
            get { return _invocableHandlerMethod.IsVoid; }
        }

        protected override bool ShouldCopyRequestHeaders
        {
            get { return _copyHeaders; }
        }

        protected override object HandleRequestMessage(IMessage requestMessage)
        {
            try
            {
                // TODO:  Look at async task type methods
                var result = _invocableHandlerMethod.Invoke(requestMessage);
                return result;
            }
            catch (Exception e)
            {
                if (e is MessagingException)
                {
                    throw;
                }
                else
                {
                    throw new MessagingException(
                        requestMessage, "Exception thrown while invoking " + _invocableHandlerMethod.ShortLogMessage, e);
                }
            }
        }
    }
}
