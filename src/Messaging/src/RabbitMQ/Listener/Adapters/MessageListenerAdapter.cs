﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters
{
    public class MessageListenerAdapter : AbstractMessageListenerAdapter
    {
        public const string ORIGINAL_DEFAULT_LISTENER_METHOD = "HandleMessage";
        private readonly Dictionary<string, string> _queueOrTagToMethodName = new Dictionary<string, string>();

        public string DefaultListenerMethod { get; set; } = ORIGINAL_DEFAULT_LISTENER_METHOD;

        public object Instance { get; set; }

        public MessageListenerAdapter(IApplicationContext context, ILogger logger = null)
            : base(context, logger)
        {
            Instance = this;
        }

        public MessageListenerAdapter(IApplicationContext context, object delgate, ILogger logger = null)
            : base(context, logger)
        {
            Instance = delgate ?? throw new ArgumentNullException(nameof(delgate));
        }

        public MessageListenerAdapter(IApplicationContext context, object delgate, ISmartMessageConverter messageConverter, ILogger logger = null)
            : base(context, logger)
        {
            Instance = delgate ?? throw new ArgumentNullException(nameof(delgate));
            MessageConverter = messageConverter;
        }

        public MessageListenerAdapter(IApplicationContext context, object delgate, string defaultListenerMethod, ILogger logger = null)
           : this(context, delgate, logger)
        {
            DefaultListenerMethod = defaultListenerMethod;
        }

        public void SetQueueOrTagToMethodName(Dictionary<string, string> queueOrTagToMethodName)
        {
            foreach (var entry in queueOrTagToMethodName)
            {
                _queueOrTagToMethodName[entry.Key] = entry.Value;
            }
        }

        public void AddQueueOrTagToMethodName(string queueOrTag, string methodName)
        {
            _queueOrTagToMethodName[queueOrTag] = methodName;
        }

        public string RemoveQueueOrTagToMethodName(string queueOrTag)
        {
            _queueOrTagToMethodName.Remove(queueOrTag, out var previous);
            return previous;
        }

        public override void OnMessage(IMessage message, RC.IModel channel)
        {
            // Check whether the delegate is a IMessageListener impl itself.
            // In that case, the adapter will simply act as a pass-through.
            var delegateListener = this.Instance;
            if (delegateListener != this)
            {
                if (delegateListener is IChannelAwareMessageListener)
                {
                    ((IChannelAwareMessageListener)delegateListener).OnMessage(message, channel);
                    return;
                }
                else if (delegateListener is IMessageListener)
                {
                    ((IMessageListener)delegateListener).OnMessage(message);
                    return;
                }
            }

            // Regular case: find a handler method reflectively.
            var convertedMessage = ExtractMessage(message);
            var methodName = GetListenerMethodName(message, convertedMessage);
            if (methodName == null)
            {
                throw new InvalidOperationException("No default listener method specified: "
                        + "Either specify a non-null value for the 'DefaultListenerMethod' property or "
                        + "override the 'GetListenerMethodName' method.");
            }

            // Invoke the handler method with appropriate arguments.
            var listenerArguments = BuildListenerArguments(convertedMessage, channel, message);
            var result = InvokeListenerMethod(methodName, listenerArguments, message);
            if (result != null)
            {
                HandleResult(new InvocationResult(result, null, null, null, null), message, channel);
            }
            else
            {
                _logger?.LogTrace("No result object given - no result to handle");
            }
        }

        protected virtual object[] BuildListenerArguments(object extractedMessage, RC.IModel channel, IMessage message)
        {
            return BuildListenerArguments(extractedMessage);
        }

        protected virtual object[] BuildListenerArguments(object extractedMessage)
        {
            return new object[] { extractedMessage };
        }

        protected virtual string GetListenerMethodName(IMessage originalMessage, object extractedMessage)
        {
            if (_queueOrTagToMethodName.Count > 0)
            {
                var props = originalMessage.Headers;
                if (!_queueOrTagToMethodName.TryGetValue(props.ConsumerQueue(), out var methodName))
                {
                    _queueOrTagToMethodName.TryGetValue(props.ConsumerTag(), out methodName);
                }

                if (methodName != null)
                {
                    return methodName;
                }
            }

            return DefaultListenerMethod;
        }

        protected virtual object InvokeListenerMethod(string methodName, object[] arguments, IMessage originalMessage)
        {
            try
            {
                var methodInvoker = new MethodInvoker();
                methodInvoker.SetTargetObject(Instance);
                methodInvoker.TargetMethod = methodName;
                methodInvoker.SetArguments(arguments);
                methodInvoker.Prepare();
                return methodInvoker.Invoke();
            }
            catch (TargetInvocationException ex)
            {
                var targetEx = ex.InnerException;

                throw new ListenerExecutionFailedException(
                    "Listener method '" + methodName + "' threw exception", targetEx, originalMessage);
            }
            catch (Exception ex)
            {
                var arrayClass = new List<string>();
                if (arguments != null)
                {
                    foreach (var argument in arguments)
                    {
                        if (argument != null)
                        {
                            arrayClass.Add(argument.GetType().ToString());
                        }
                    }
                }

                throw new ListenerExecutionFailedException(
                    "Failed to invoke target method '" + methodName
                        + "' with argument type = [" + string.Join(",", arrayClass)
                        + "], value = [" + NullSafeToString(arguments) + "]", ex,
                    originalMessage);
            }
        }

        private string NullSafeToString(object[] values)
        {
            var sb = new StringBuilder();
            foreach (var v in values)
            {
                sb.Append(v);
                sb.Append(",");
            }

            return sb.ToString(0, sb.Length - 1);
        }
    }
}
