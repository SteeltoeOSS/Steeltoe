// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Transformer
{
    public class MessageTransformingHandler : AbstractReplyProducingMessageHandler, ILifecycle
    {
        public MessageTransformingHandler(IApplicationContext context, ITransformer transformer)
            : base(context)
        {
            Transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
            RequiresReply = true;
        }

        public ITransformer Transformer { get; }

        public bool IsRunning
        {
            get
            {
                if (Transformer is ILifecycle asLifecycle)
                {
                    return asLifecycle.IsRunning;
                }

                return false;
            }
        }

        protected override bool ShouldCopyRequestHeaders => false;

        public override void Initialize()
        {
            // Nothing to do
        }

        public override void AddNotPropagatedHeaders(params string[] headers)
        {
            base.AddNotPropagatedHeaders(headers);
            PopulateNotPropagatedHeadersIfAny();
        }

        public Task Start()
        {
            if (Transformer is ILifecycle asLifecycle)
            {
                return asLifecycle.Start();
            }

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (Transformer is ILifecycle asLifecycle)
            {
                return asLifecycle.Stop();
            }

            return Task.CompletedTask;
        }

        protected override object HandleRequestMessage(IMessage requestMessage)
        {
            try
            {
                return Transformer.Transform(requestMessage);
            }
            catch (Exception e)
            {
                if (e is MessageTransformationException)
                {
                    throw;
                }

                throw new MessageTransformationException(requestMessage, "Failed to transform Message in " + this, e);
            }
        }

        private void PopulateNotPropagatedHeadersIfAny()
        {
            var notPropagatedHeaders = NotPropagatedHeaders;

            // if (Transformer is AbstractMessageProcessingTransformer && notPropagatedHeaders.Count != 0)
            // {
            //    ((AbstractMessageProcessingTransformer)this.Transformer)
            //            .setNotPropagatedHeaders(notPropagatedHeaders.toArray(new String[0]));
            // }
        }
    }
}
