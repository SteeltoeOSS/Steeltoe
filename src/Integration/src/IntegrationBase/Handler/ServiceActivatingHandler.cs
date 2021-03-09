// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Attributes;
using Steeltoe.Messaging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Handler
{
    public class ServiceActivatingHandler : AbstractReplyProducingMessageHandler, ILifecycle
    {
        private readonly IMessageProcessor _processor;

        public ServiceActivatingHandler(IApplicationContext context, object instance, MethodInfo method)
        : this(context, (IMessageProcessor)new MethodInvokingMessageProcessor<object>(context, instance, method))
        {
        }

        public ServiceActivatingHandler(IApplicationContext context, IMessageProcessor processor)
            : base(context)
        {
            _processor = processor;
        }

        public override string ComponentType => "service-activator";

        public virtual bool IsRunning => _processor is not ILifecycle lifecycle || lifecycle.IsRunning;

        public override void Initialize()
        {
            // Nothing to do
        }

        public virtual Task Start()
        {
            if (_processor is ILifecycle lifecycle)
            {
                return lifecycle.Start();
            }

            return Task.CompletedTask;
        }

        public virtual Task Stop()
        {
            if (_processor is ILifecycle lifecycle)
            {
                return lifecycle.Stop();
            }

            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return "ServiceActivator for [" + _processor + "]"
                    + (ComponentName == null ? string.Empty : " (" + ComponentName + ")");
        }

        protected override object HandleRequestMessage(IMessage requestMessage)
        {
            return _processor.ProcessMessage(requestMessage);
        }
    }
}
