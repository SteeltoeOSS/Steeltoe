// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

public class MessageListenerAdapter : AbstractMessageListenerAdapter
{
    public const string OriginalDefaultListenerMethod = "HandleMessage";
    private readonly Dictionary<string, string> _queueOrTagToMethodName = new();

    public string DefaultListenerMethod { get; set; } = OriginalDefaultListenerMethod;

    public object Instance { get; set; }

    public MessageListenerAdapter(IApplicationContext context, ILogger logger = null)
        : base(context, logger)
    {
        Instance = this;
    }

    public MessageListenerAdapter(IApplicationContext context, object @delegate, ILogger logger = null)
        : base(context, logger)
    {
        ArgumentGuard.NotNull(@delegate);

        Instance = @delegate;
    }

    public MessageListenerAdapter(IApplicationContext context, object @delegate, ISmartMessageConverter messageConverter, ILogger logger = null)
        : base(context, logger)
    {
        ArgumentGuard.NotNull(@delegate);

        Instance = @delegate;
        MessageConverter = messageConverter;
    }

    public MessageListenerAdapter(IApplicationContext context, object @delegate, string defaultListenerMethod, ILogger logger = null)
        : this(context, @delegate, logger)
    {
        DefaultListenerMethod = defaultListenerMethod;
    }

    public void SetQueueOrTagToMethodName(Dictionary<string, string> queueOrTagToMethodName)
    {
        foreach (KeyValuePair<string, string> entry in queueOrTagToMethodName)
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
        _queueOrTagToMethodName.Remove(queueOrTag, out string previous);
        return previous;
    }

    public override void OnMessage(IMessage message, RC.IModel channel)
    {
        // Check whether the delegate is a IMessageListener impl itself.
        // In that case, the adapter will simply act as a pass-through.
        object delegateListener = Instance;

        if (delegateListener != this)
        {
            switch (delegateListener)
            {
                case IChannelAwareMessageListener channelAwareMessageListener:
                    channelAwareMessageListener.OnMessage(message, channel);
                    return;
                case IMessageListener messageListener:
                    messageListener.OnMessage(message);
                    return;
            }
        }

        // Regular case: find a handler method reflectively.
        object convertedMessage = ExtractMessage(message);
        string methodName = GetListenerMethodName(message, convertedMessage);

        if (methodName == null)
        {
            throw new InvalidOperationException("No default listener method specified: " +
                "Either specify a non-null value for the 'DefaultListenerMethod' property or " + "override the 'GetListenerMethodName' method.");
        }

        // Invoke the handler method with appropriate arguments.
        object[] listenerArguments = BuildListenerArguments(convertedMessage, channel, message);
        object result = InvokeListenerMethod(methodName, listenerArguments, message);

        if (result != null)
        {
            HandleResult(new InvocationResult(result, null, null, null, null), message, channel);
        }
        else
        {
            Logger?.LogTrace("No result object given - no result to handle");
        }
    }

    protected virtual object[] BuildListenerArguments(object extractedMessage, RC.IModel channel, IMessage message)
    {
        return BuildListenerArguments(extractedMessage);
    }

    protected virtual object[] BuildListenerArguments(object extractedMessage)
    {
        return new[]
        {
            extractedMessage
        };
    }

    protected virtual string GetListenerMethodName(IMessage originalMessage, object extractedMessage)
    {
        if (_queueOrTagToMethodName.Count > 0)
        {
            IMessageHeaders props = originalMessage.Headers;

            if (!_queueOrTagToMethodName.TryGetValue(props.ConsumerQueue(), out string methodName))
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
            Exception targetEx = ex.InnerException;

            throw new ListenerExecutionFailedException($"Listener method '{methodName}' threw exception", targetEx, originalMessage);
        }
        catch (Exception ex)
        {
            var arrayClass = new List<string>();

            if (arguments != null)
            {
                foreach (object argument in arguments)
                {
                    if (argument != null)
                    {
                        arrayClass.Add(argument.GetType().ToString());
                    }
                }
            }

            throw new ListenerExecutionFailedException(
                $"Failed to invoke target method '{methodName}' with argument type = [{string.Join(",", arrayClass)}], value = [{NullSafeToString(arguments)}]",
                ex, originalMessage);
        }
    }

    private string NullSafeToString(object[] values)
    {
        var sb = new StringBuilder();

        foreach (object v in values)
        {
            sb.Append(v);
            sb.Append(',');
        }

        return sb.ToString(0, sb.Length - 1);
    }
}
