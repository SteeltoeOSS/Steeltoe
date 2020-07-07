// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
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

        protected MessageProducerSupportEndpoint(IApplicationContext context)
            : base(context)
        {
            Phase = int.MaxValue / 2;
            _messagingTemplate = new MessagingTemplate(context);
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
