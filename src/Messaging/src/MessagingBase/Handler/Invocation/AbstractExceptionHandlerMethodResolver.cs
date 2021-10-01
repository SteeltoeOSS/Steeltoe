// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public abstract class AbstractExceptionHandlerMethodResolver
    {
        private readonly Dictionary<Type, MethodInfo> _mappedMethods = new ();

        private readonly ConcurrentDictionary<Type, MethodInfo> _exceptionLookupCache = new ();

        protected AbstractExceptionHandlerMethodResolver(IDictionary<Type, MethodInfo> mappedMethods)
        {
            if (mappedMethods == null)
            {
                throw new ArgumentNullException(nameof(mappedMethods));
            }

            foreach (var entry in mappedMethods)
            {
                _mappedMethods[entry.Key] = entry.Value;
            }
        }

        protected static List<Type> GetExceptionsFromMethodSignature(MethodInfo method)
        {
            var result = new List<Type>();

            foreach (var param in method.GetParameters())
            {
                var paramType = param.ParameterType;
                if (typeof(Exception).IsAssignableFrom(paramType))
                {
                    result.Add(paramType);
                }
            }

            if (result.Count == 0)
            {
                throw new InvalidOperationException("No exception types mapped to " + method);
            }

            return result;
        }

        public bool HasExceptionMappings
        {
            get { return _mappedMethods.Count > 0; }
        }

        public MethodInfo ResolveMethod(Exception exception)
        {
            var method = ResolveMethodByExceptionType(exception.GetType());
            if (method == null)
            {
                var cause = exception.InnerException;
                if (cause != null)
                {
                    method = ResolveMethodByExceptionType(cause.GetType());
                }
            }

            return method;
        }

        public MethodInfo ResolveMethodByExceptionType(Type exceptionType)
        {
            _exceptionLookupCache.TryGetValue(exceptionType, out var method);
            if (method == null)
            {
                method = GetMappedMethod(exceptionType);
                if (_exceptionLookupCache.TryAdd(exceptionType, method))
                {
                    return method;
                }

                _exceptionLookupCache.TryGetValue(exceptionType, out method);
            }

            return method;
        }

        private MethodInfo GetMappedMethod(Type exceptionType)
        {
            var matches = new List<Type>();
            foreach (var mappedException in _mappedMethods.Keys)
            {
                if (mappedException.IsAssignableFrom(exceptionType))
                {
                    matches.Add(mappedException);
                }
            }

            if (matches.Count > 0)
            {
                matches.Sort(new ExceptionDepthComparator(exceptionType));
                return _mappedMethods[matches[0]];
            }
            else
            {
                return null;
            }
        }
    }
}