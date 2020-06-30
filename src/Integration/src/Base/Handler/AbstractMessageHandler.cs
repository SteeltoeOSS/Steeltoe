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

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Order;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Handler
{
    public abstract class AbstractMessageHandler : IMessageHandler, IOrdered
    {
        protected IApplicationContext _context;
        private IIntegrationServices _integrationServices;

        protected AbstractMessageHandler(IApplicationContext context)
        {
            _context = context;
        }

        public IIntegrationServices IntegrationServices
        {
            get
            {
                if (_integrationServices == null)
                {
                    _integrationServices = _context.GetService<IIntegrationServices>();
                }

                return _integrationServices;
            }
        }

        public virtual string ComponentType
        {
            get { return "message-handler"; }
        }

        public virtual string Name { get; set; }

        public virtual string ComponentName { get; set; }

        public int Order => AbstractOrdered.LOWEST_PRECEDENCE - 1;

        public virtual void HandleMessage(IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.Payload == null)
            {
                throw new ArgumentNullException("Message payload is null");
            }

            try
            {
                HandleMessageInternal(message);
            }
            catch (Exception e)
            {
                var wrapped = IntegrationUtils.WrapInHandlingExceptionIfNecessary(message, "error occurred in message handler [" + this + "]", e);
                if (wrapped != e)
                {
                    throw wrapped;
                }

                throw;
            }
        }

        protected abstract void HandleMessageInternal(IMessage message);
    }
}
