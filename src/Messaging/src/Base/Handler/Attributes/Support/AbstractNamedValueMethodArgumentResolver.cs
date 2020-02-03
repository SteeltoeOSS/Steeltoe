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

using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public abstract class AbstractNamedValueMethodArgumentResolver : IHandlerMethodArgumentResolver
    {
        private readonly IConversionService _conversionService;
        private readonly ConcurrentDictionary<ParameterInfo, NamedValueInfo> _namedValueInfoCache = new ConcurrentDictionary<ParameterInfo, NamedValueInfo>();

        protected AbstractNamedValueMethodArgumentResolver(IConversionService conversionService)
        {
            _conversionService = conversionService;
        }

        public virtual object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var namedValueInfo = GetNamedValueInfo(parameter);

            var resolvedName = ResolveEmbeddedValuesAndExpressions(namedValueInfo.Name);
            if (resolvedName == null)
            {
                throw new InvalidOperationException(
                        "Specified name must not resolve to null: [" + namedValueInfo.Name + "]");
            }

            var arg = ResolveArgumentInternal(parameter, message, resolvedName.ToString());
            if (arg == null)
            {
                if (namedValueInfo.DefaultValue != null)
                {
                    arg = ResolveEmbeddedValuesAndExpressions(namedValueInfo.DefaultValue);
                }
                else if (namedValueInfo.Required && !parameter.HasDefaultValue)
                {
                    HandleMissingValue(namedValueInfo.Name, parameter, message);
                }

                arg = HandleNullValue(namedValueInfo.Name, arg, parameter.ParameterType);
            }
            else if (string.Empty.Equals(arg) && namedValueInfo.DefaultValue != null)
            {
                arg = ResolveEmbeddedValuesAndExpressions(namedValueInfo.DefaultValue);
            }

            if (!parameter.ParameterType.IsInstanceOfType(arg))
            {
                arg = _conversionService.Convert(arg, arg?.GetType(), parameter.ParameterType);
            }

            HandleResolvedValue(arg, namedValueInfo.Name, parameter, message);

            return arg;
        }

        public virtual bool SupportsParameter(ParameterInfo parameter)
        {
            throw new NotImplementedException();
        }

        protected abstract NamedValueInfo CreateNamedValueInfo(ParameterInfo parameter);

        protected abstract object ResolveArgumentInternal(ParameterInfo parameter, IMessage message, string name);

        protected abstract void HandleMissingValue(string name, ParameterInfo parameter, IMessage message);

        protected virtual void HandleResolvedValue(object arg, string name, ParameterInfo parameter, IMessage message)
        {
        }

        private NamedValueInfo GetNamedValueInfo(ParameterInfo parameter)
        {
            if (!_namedValueInfoCache.TryGetValue(parameter, out var namedValueInfo))
            {
                namedValueInfo = CreateNamedValueInfo(parameter);
                namedValueInfo = UpdateNamedValueInfo(parameter, namedValueInfo);
                if (!_namedValueInfoCache.TryAdd(parameter, namedValueInfo))
                {
                    _namedValueInfoCache.TryGetValue(parameter, out namedValueInfo);
                }
            }

            return namedValueInfo;
        }

        private NamedValueInfo UpdateNamedValueInfo(ParameterInfo parameter, NamedValueInfo info)
        {
            var name = info.Name;
            if (string.IsNullOrEmpty(info.Name))
            {
                name = parameter.Name;
                if (name == null)
                {
                    var type = parameter.ParameterType;
                    throw new InvalidOperationException(
                            "Name for argument of type [" + type.Name + "] not specified, " +
                                    "and parameter name information not found in class file either.");
                }
            }

            return new NamedValueInfo(name, info.Required, info.DefaultValue);
        }

        private object HandleNullValue(string name, object value, Type paramType)
        {
            if (value == null)
            {
                if (typeof(bool) == paramType)
                {
                    return false;
                }
                else if (paramType.IsPrimitive)
                {
                    throw new InvalidOperationException("Optional " + paramType + " parameter '" + name +
                            "' is present but cannot be translated into a null value due to being " +
                            "declared as a primitive type. Consider declaring it as object wrapper " +
                            "for the corresponding primitive type.");
                }
            }

            return value;
        }

        private object ResolveEmbeddedValuesAndExpressions(string value)
        {
            // if (this.configurableBeanFactory == null || this.expressionContext == null)
            // {
            //    return value;
            // }

            // String placeholdersResolved = this.configurableBeanFactory.resolveEmbeddedValue(value);
            // BeanExpressionResolver exprResolver = this.configurableBeanFactory.getBeanExpressionResolver();
            // if (exprResolver == null)
            // {
            //    return value;
            // }
            // return exprResolver.evaluate(placeholdersResolved, this.expressionContext);
            return value;
        }

        protected class NamedValueInfo
        {
            public readonly string Name;

            public readonly bool Required;

            public readonly string DefaultValue;

            public NamedValueInfo(string name, bool required, string defaultValue = null)
            {
                Name = name;
                Required = required;
                DefaultValue = defaultValue;
            }
        }
    }
}
