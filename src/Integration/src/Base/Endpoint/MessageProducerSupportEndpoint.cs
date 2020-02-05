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
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Endpoint
{
    public abstract class MessageProducerSupportEndpoint : AbstractEndpoint, IMessageProducer
    {
        protected object _lock = new object();

        private readonly MessagingTemplate _messagingTemplate;

        private IErrorMessageStrategy _errorMessageStrategy = new DefaultErrorMessageStrategy();

        private volatile IMessageChannel _outputChannel;

        private volatile string _outputChannelName;

        private volatile IMessageChannel _errorChannel;

        private volatile string _errorChannelName;

        protected MessageProducerSupportEndpoint(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            Phase = int.MaxValue / 2;
            _messagingTemplate = new MessagingTemplate(serviceProvider);
        }

        public virtual IMessageChannel OutputChannel
        {
            get
            {
                if (_outputChannelName != null)
                {
                    lock (_lock)
                    {
                        if (_outputChannelName != null)
                        {
                            _outputChannel = IntegrationServices.ChannelResolver.ResolveDestination(_outputChannelName);
                        }

                        _outputChannelName = null;
                    }
                }

                return _outputChannel;
            }

            set
            {
                _outputChannel = value;
            }
        }

        public virtual string OutputChannelName
        {
            get
            {
                return _outputChannelName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("OutputChannelName can not be null or empty");
                }

                _outputChannelName = value;
            }
        }

        public virtual IMessageChannel ErrorChannel
        {
            get
            {
                if (_errorChannelName != null)
                {
                    lock (_lock)
                    {
                        if (_errorChannelName != null)
                        {
                            _errorChannel = IntegrationServices.ChannelResolver.ResolveDestination(_errorChannelName);
                        }

                        _errorChannelName = null;
                    }
                }

                return _errorChannel;
            }

            set
            {
                _errorChannel = value;
            }
        }

        public virtual string ErrorChannelName
        {
            get
            {
                return _errorChannelName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("ErrorChannelName can not be null or empty");
                }

                _errorChannelName = value;
            }
        }

        public virtual int SendTimeout
        {
            get
            {
                return _messagingTemplate.SendTimeout;
            }

            set
            {
                _messagingTemplate.SendTimeout = value;
            }
        }

        public virtual IErrorMessageStrategy ErrorMessageStrategy
        {
            get
            {
                return _errorMessageStrategy;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("'errorMessageStrategy' cannot be null");
                }

                _errorMessageStrategy = value;
            }
        }

        protected internal virtual void SendMessage(IMessage messageArg)
        {
            var message = messageArg;
            if (message == null)
            {
                throw new MessagingException("cannot send a null message");
            }

            try
            {
                _messagingTemplate.Send(OutputChannel, message);
            }
            catch (Exception e)
            {
                if (!SendErrorMessageIfNecessary(message, e))
                {
                    throw;
                }
            }
        }

        protected virtual MessagingTemplate MessagingTemplate
        {
            get { return _messagingTemplate; }
        }

        protected override Task DoStart()
        {
            return Task.CompletedTask;
        }

        protected override Task DoStop()
        {
            return Task.CompletedTask;
        }

        protected bool SendErrorMessageIfNecessary(IMessage message, Exception exception)
        {
            var channel = ErrorChannel;
            if (channel != null)
            {
                _messagingTemplate.Send(channel, BuildErrorMessage(message, exception));
                return true;
            }

            return false;
        }

        protected ErrorMessage BuildErrorMessage(IMessage message, Exception exception)
        {
            return _errorMessageStrategy.BuildErrorMessage(exception, GetErrorMessageAttributes(message));
        }

        protected virtual IAttributeAccessor GetErrorMessageAttributes(IMessage message)
        {
            return ErrorMessageUtils.GetAttributeAccessor(message, null);
        }
    }
}
