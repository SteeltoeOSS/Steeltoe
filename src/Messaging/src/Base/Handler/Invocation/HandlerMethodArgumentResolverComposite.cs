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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public class HandlerMethodArgumentResolverComposite : IHandlerMethodArgumentResolver
    {
        private readonly List<IHandlerMethodArgumentResolver> _argumentResolvers = new List<IHandlerMethodArgumentResolver>();

        private readonly ConcurrentDictionary<ParameterInfo, IHandlerMethodArgumentResolver> argumentResolverCache =
                new ConcurrentDictionary<ParameterInfo, IHandlerMethodArgumentResolver>();

        public HandlerMethodArgumentResolverComposite AddResolver(IHandlerMethodArgumentResolver argumentResolver)
        {
            _argumentResolvers.Add(argumentResolver);
            return this;
        }

        public HandlerMethodArgumentResolverComposite AddResolvers(params IHandlerMethodArgumentResolver[] resolvers)
        {
            if (resolvers != null)
            {
                _argumentResolvers.AddRange(resolvers);
            }

            return this;
        }

        public HandlerMethodArgumentResolverComposite AddResolvers(IList<IHandlerMethodArgumentResolver> resolvers)
        {
            if (resolvers != null)
            {
                _argumentResolvers.AddRange(resolvers);
            }

            return this;
        }

        public int Count
        {
            get
            {
                return _argumentResolvers.Count;
            }
        }

        public List<IHandlerMethodArgumentResolver> Resolvers
        {
            get { return new List<IHandlerMethodArgumentResolver>(_argumentResolvers); }
        }

        public void Clear()
        {
            _argumentResolvers.Clear();
        }

        public bool SupportsParameter(ParameterInfo parameter)
        {
            return GetArgumentResolver(parameter) != null;
        }

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var resolver = GetArgumentResolver(parameter);
            if (resolver == null)
            {
                throw new InvalidOperationException(
                        "Unsupported parameter type [" + parameter.ParameterType.Name + "]." +
                                " supportsParameter should be called first.");
            }

            return resolver.ResolveArgument(parameter, message);
        }

        private IHandlerMethodArgumentResolver GetArgumentResolver(ParameterInfo parameter)
        {
            if (!argumentResolverCache.TryGetValue(parameter, out var result))
            {
                foreach (var resolver in _argumentResolvers)
                {
                    if (resolver.SupportsParameter(parameter))
                    {
                        result = resolver;
                        argumentResolverCache.TryAdd(parameter, result);
                        break;
                    }
                }
            }

            return result;
        }
    }
}
