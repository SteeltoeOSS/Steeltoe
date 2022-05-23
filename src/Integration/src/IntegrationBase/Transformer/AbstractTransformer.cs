// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;

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
                _messageBuilderFactory ??=
                    ApplicationContext.GetService<IMessageBuilderFactory>() ?? new DefaultMessageBuilderFactory();
                return _messageBuilderFactory;
            }

            set => _messageBuilderFactory = value;
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

                return (result is IMessage iMessage) ? iMessage : MessageBuilderFactory.WithPayload(result).CopyHeaders(message.Headers).Build();
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
