// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Messaging.RabbitMQ.Attributes;
using Steeltoe.Messaging.RabbitMQ.Listener;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class RabbitListenerMetadata
{
    internal static readonly Dictionary<Type, RabbitListenerMetadata> TypeCache = new();
    internal static readonly HashSet<string> Groups = new();

    internal List<ListenerMethod> ListenerMethods { get; }

    internal List<MethodInfo> HandlerMethods { get; }

    internal List<RabbitListenerAttribute> ClassAnnotations { get; }

    internal Type TargetClass { get; }

    internal RabbitListenerMetadata(Type targetClass, List<ListenerMethod> methods, List<MethodInfo> multiMethods,
        List<RabbitListenerAttribute> classLevelListeners)
    {
        TargetClass = targetClass;
        ListenerMethods = methods;
        HandlerMethods = multiMethods;
        ClassAnnotations = classLevelListeners;
    }

    internal RabbitListenerMetadata()
    {
        ListenerMethods = new List<ListenerMethod>();
        HandlerMethods = new List<MethodInfo>();
        ClassAnnotations = new List<RabbitListenerAttribute>();
    }

    internal static RabbitListenerMetadata BuildMetadata(IServiceCollection services, Type targetClass)
    {
        ArgumentGuard.NotNull(targetClass);

        if (TypeCache.TryGetValue(targetClass, out _))
        {
            return null;
        }

        List<RabbitListenerAttribute> classLevelListeners = targetClass.GetCustomAttributes<RabbitListenerAttribute>().ToList();
        Validate(services, classLevelListeners);
        var methods = new List<ListenerMethod>();
        var multiMethods = new List<MethodInfo>();
        MethodInfo[] reflectMethods = targetClass.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (MethodInfo m in reflectMethods)
        {
            List<RabbitListenerAttribute> methodLevelListeners = m.GetCustomAttributes<RabbitListenerAttribute>().ToList();

            if (methodLevelListeners.Count > 0)
            {
                Validate(services, methodLevelListeners);
                methods.Add(new ListenerMethod(m, methodLevelListeners));
            }

            if (classLevelListeners.Count > 0)
            {
                IEnumerable<RabbitHandlerAttribute> handlerAttributes = m.GetCustomAttributes<RabbitHandlerAttribute>();

                if (handlerAttributes.Any())
                {
                    multiMethods.Add(m);
                }
            }
        }

        if (methods.Count == 0 && multiMethods.Count == 0)
        {
            return null;
        }

        return new RabbitListenerMetadata(targetClass, methods, multiMethods, classLevelListeners);
    }

    private static void Validate(IServiceCollection services, List<RabbitListenerAttribute> listenerAttributes)
    {
        foreach (RabbitListenerAttribute listener in listenerAttributes)
        {
            string[] queues = listener.Queues;
            string[] bindings = listener.Bindings;

            if (bindings.Length > 0 && queues.Length > 0)
            {
                throw new InvalidOperationException("RabbitListenerAttribute can have either 'Queues' or 'Bindings' set, but not both");
            }

            if (!string.IsNullOrEmpty(listener.Group) && !Groups.Contains(listener.Group))
            {
                Groups.Add(listener.Group);
                services.AddSingleton<IMessageListenerContainerCollection>(new MessageListenerContainerCollection(listener.Group));
            }
        }
    }

    internal static void Reset()
    {
        TypeCache.Clear();
    }

    internal sealed class ListenerMethod
    {
        public MethodInfo Method { get; }

        public List<RabbitListenerAttribute> Attributes { get; }

        public ListenerMethod(MethodInfo method, List<RabbitListenerAttribute> attributes)
        {
            Method = method;
            Attributes = attributes;
        }
    }
}
