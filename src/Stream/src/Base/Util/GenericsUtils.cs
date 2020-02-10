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

using Steeltoe.Stream.Binder;
using System;

namespace Steeltoe.Stream.Util
{
    internal static class GenericsUtils
    {
        internal static Type GetParameterType(Type evaluatedClass, Type interfaceClass, int position)
        {
            Type bindableType = null;
            if (!interfaceClass.IsInterface)
            {
                throw new ArgumentException(nameof(interfaceClass) + " is not an interface");
            }

            var currentType = evaluatedClass;
            while (!typeof(object).Equals(currentType) && bindableType == null)
            {
                var interfaces = currentType.GetInterfaces();
                Type resolvableType = null;
                foreach (var interfaceType in interfaces)
                {
                    var typeToCheck = interfaceType;
                    if (interfaceType.IsGenericType)
                    {
                        typeToCheck = interfaceType.GetGenericTypeDefinition();
                    }

                    if (interfaceClass == typeToCheck)
                    {
                        resolvableType = interfaceType;
                        break;
                    }
                }

                if (resolvableType == null)
                {
                    currentType = currentType.BaseType;
                }
                else
                {
                    if (resolvableType.IsGenericType)
                    {
                        var genArgs = resolvableType.GetGenericArguments();
                        bindableType = genArgs[position];
                    }
                    else
                    {
                        bindableType = typeof(object);
                    }
                }
            }

            if (bindableType == null)
            {
                throw new InvalidOperationException("Cannot find parameter of " + evaluatedClass.Name + " for " + interfaceClass + " at position " + position);
            }

            return bindableType;
        }

        internal static bool CheckCompatiblePollableBinder(IBinder binderInstance, Type bindingTargetType)
        {
            var binderInstanceType = binderInstance.GetType();
            var binderInterfaces = binderInstanceType.GetInterfaces();
            foreach (var intf in binderInterfaces)
            {
                if (typeof(IPollableConsumerBinder).IsAssignableFrom(intf))
                {
                    var targetInterfaces = bindingTargetType.GetInterfaces();
                    var psType = FindPollableSourceType(targetInterfaces);
                    if (psType != null)
                    {
                        return GetParameterType(binderInstance.GetType(), intf, 0).IsAssignableFrom(psType);
                    }
                }
            }

            return false;
        }

        internal static Type FindPollableSourceType(Type[] targetInterfaces)
        {
            foreach (var targetIntf in targetInterfaces)
            {
                if (typeof(IPollableSource).IsAssignableFrom(targetIntf))
                {
                    var supers = targetIntf.GetInterfaces();
                    foreach (var type in supers)
                    {
                        if (type.IsGenericType)
                        {
                            var resolvableType = type.GetGenericTypeDefinition();
                            if (resolvableType.Equals(typeof(IPollableSource<>)))
                            {
                                return type.GetGenericArguments()[0];
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
