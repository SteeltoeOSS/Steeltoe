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
    public class ServiceActivatingHandler : AbstractReplyProducingMessageHandler, ILifecycle
    {
        private readonly IMessageProcessor _processor;

        // TODO:

        // public ServiceActivatingHandler(object instance)
        // : this(new MethodInvokingMessageProcessor(instance, typeof(ServiceActivatorAttribute)))
        // {
        // }

        // public ServiceActivatingHandler(object instance, MethodInfo method)
        // : this(new MethodInvokingMessageProcessor(instance, method))
        // {
        // }

        // public ServiceActivatingHandler(object instance, string methodName)
        // : this(new MethodInvokingMessageProcessor(instance, methodName))
        // {
        // }
        public ServiceActivatingHandler(IApplicationContext context, IMessageProcessor processor)
            : base(context)
        {
            _processor = processor;
        }

        public override string ComponentType => "service-activator";

        public virtual bool IsRunning => !(_processor is ILifecycle) || ((ILifecycle)_processor).IsRunning;

        public virtual Task Start()
        {
            if (_processor is ILifecycle)
            {
                return ((ILifecycle)_processor).Start();
            }

            return Task.CompletedTask;
        }

        public virtual Task Stop()
        {
            if (_processor is ILifecycle)
            {
                return ((ILifecycle)_processor).Stop();
            }

            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return "ServiceActivator for [" + _processor + "]"
                    + (ComponentName == null ? string.Empty : " (" + ComponentName + ")");
        }

        protected override object HandleRequestMessage(IMessage message)
        {
            return _processor.ProcessMessage(message);
        }
    }
}
