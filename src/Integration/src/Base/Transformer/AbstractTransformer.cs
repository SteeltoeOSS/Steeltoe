using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Integration.Transformer
{
    public abstract class AbstractTransformer : ITransformer
    {
        private IMessageBuilderFactory _messageBuilderFactory;

        protected AbstractTransformer(IApplicationContext context)
        {
            ApplicationContext = context;
        }

        public IApplicationContext ApplicationContext { get; }

        public virtual IMessageBuilderFactory MessageBuilderFactory
        {
            get
            {
                if (_messageBuilderFactory == null)
                {
                    _messageBuilderFactory = ApplicationContext.GetService<IMessageBuilderFactory>();
                    if (_messageBuilderFactory == null)
                    {
                        _messageBuilderFactory = new DefaultMessageBuilderFactory();
                    }
                }

                return _messageBuilderFactory;
            }

            set
            {
                _messageBuilderFactory = value;
            }
        }

        public IMessage Transform(IMessage message)
        {
            try
            {
                var result = DoTransform(message);
                if (result == null)
                {
                    return null;
                }

                return (result is IMessage) ? (IMessage)result : MessageBuilderFactory.WithPayload(result).CopyHeaders(message.Headers).Build();
            }
            catch (MessageTransformationException)
            { 
                throw;
            }
            catch (Exception e)
            {
                throw new MessageTransformationException(message, "failed to transform message", e);
            }
        }

        protected abstract object DoTransform(IMessage message);
    }
}
