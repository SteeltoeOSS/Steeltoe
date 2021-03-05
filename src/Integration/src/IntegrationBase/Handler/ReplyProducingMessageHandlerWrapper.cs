// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Handler
{
    public class ReplyProducingMessageHandlerWrapper : AbstractReplyProducingMessageHandler, ILifecycle
    {
        private readonly IMessageHandler _target;

        public ReplyProducingMessageHandlerWrapper(IApplicationContext context, IMessageHandler target)
            : base(context)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            _target = target;
        }

        public Task Start()
        {
            if (_target is ILifecycle lifeCycle)
            {
                return lifeCycle.Start();
            }

            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (_target is ILifecycle lifeCycle)
            {
                return lifeCycle.Stop();
            }

            return Task.CompletedTask;
        }

        public bool IsRunning => _target is not ILifecycle lifecycle || lifecycle.IsRunning;

        public override void Initialize()
        {
            // Nothing to do
        }

        protected override object HandleRequestMessage(IMessage requestMessage)
        {
            _target.HandleMessage(requestMessage);
            return null;
        }
    }
}
