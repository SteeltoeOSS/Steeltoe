// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Messaging.Rabbit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Config
{
    // TODO: Internal?
    public class RabbitListenerMetadata
    {
        internal static readonly Dictionary<Type, RabbitListenerMetadata> _typeCache = new Dictionary<Type, RabbitListenerMetadata>();

        internal RabbitListenerMetadata(Type targetClass, List<ListenerMethod> methods, List<MethodInfo> multiMethods, List<RabbitListenerAttribute> classLevelListeners)
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

        internal List<ListenerMethod> ListenerMethods { get; }

        internal List<MethodInfo> HandlerMethods { get; }

        internal List<RabbitListenerAttribute> ClassAnnotations { get; }

        internal Type TargetClass { get; }

        internal static RabbitListenerMetadata BuildMetadata(Type targetClass)
        {
            if (targetClass == null)
            {
                throw new ArgumentNullException(nameof(targetClass));
            }

            if (_typeCache.TryGetValue(targetClass, out _))
            {
                return null;
            }

            var classLevelListeners = targetClass.GetCustomAttributes<RabbitListenerAttribute>().ToList();
            Validate(classLevelListeners);
            var methods = new List<ListenerMethod>();
            var multiMethods = new List<MethodInfo>();
            var reflectMethods = targetClass.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in reflectMethods)
            {
                var methodLevelListeners = m.GetCustomAttributes<RabbitListenerAttribute>().ToList();
                if (methodLevelListeners.Count > 0)
                {
                    Validate(methodLevelListeners);
                    methods.Add(new ListenerMethod(m, methodLevelListeners));
                }

                if (classLevelListeners.Count > 0)
                {
                    var handlerAttributes = m.GetCustomAttributes<RabbitHandlerAttribute>();
                    if (handlerAttributes.Count() > 0)
                    {
                        multiMethods.Add(m);
                    }
                }

                if (methods.Count == 0 && multiMethods.Count == 0)
                {
                    return null;
                }
            }

            return new RabbitListenerMetadata(targetClass, methods, multiMethods, classLevelListeners);
        }

        private static void Validate(List<RabbitListenerAttribute> listenerAttributes)
        {
            // TODO:
            // foreach (var listener in listenerAttributes)
            // {
            //    var queues = listener.Queues;
            //    var queuesToDeclare = listener.QueuesToDeclare;
            //    var bindings = listener.Bindings;

            // if ((queues.Length > 0 && (queuesToDeclare.Length > 0 || bindings.Length > 0))
            //        || (queuesToDeclare.Length > 0 && (queues.Length > 0 || bindings.Length > 0))
            //        || (bindings.Length > 0 && (queues.Length > 0 || queuesToDeclare.Length > 0)))
            //    {
            //        throw new InvalidOperationException("RabbitListenerAttribute can have only one of 'Queues', 'QueuesToDeclare', or 'Bindings'");
            //    }
            // }
        }

        internal class ListenerMethod
        {
            public MethodInfo Method { get; }

            public List<RabbitListenerAttribute> Attributes { get; }

            public ListenerMethod(MethodInfo method, List<RabbitListenerAttribute> attributes)
            {
                Method = method;
                Attributes = attributes;
            }
        }

        internal static void Reset()
        {
            _typeCache.Clear();
        }
    }
}
