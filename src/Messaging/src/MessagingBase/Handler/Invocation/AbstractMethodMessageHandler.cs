// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public abstract class AbstractMethodMessageHandler<T> : IMessageHandler
    {
        private readonly List<string> _destinationPrefixes = new List<string>();
        private readonly List<IHandlerMethodArgumentResolver> _customArgumentResolvers = new List<IHandlerMethodArgumentResolver>();
        private readonly List<IHandlerMethodReturnValueHandler> _customReturnValueHandlers = new List<IHandlerMethodReturnValueHandler>();
        private readonly HandlerMethodArgumentResolverComposite _argumentResolvers = new HandlerMethodArgumentResolverComposite();
        private readonly HandlerMethodReturnValueHandlerComposite _returnValueHandlers = new HandlerMethodReturnValueHandlerComposite();
        private readonly Dictionary<T, HandlerMethod> _handlerMethods = new Dictionary<T, HandlerMethod>(64);
        private readonly Dictionary<string, List<T>> _destinationLookup = new Dictionary<string, List<T>>(64);
        private readonly ConcurrentDictionary<Type, AbstractExceptionHandlerMethodResolver> _exceptionHandlerCache = new ConcurrentDictionary<Type, AbstractExceptionHandlerMethodResolver>();
        private readonly ILogger _logger;

        protected AbstractMethodMessageHandler(ILogger logger = null)
        {
            ServiceName = GetType().Name + "@" + GetHashCode();
            _logger = logger;
        }

        public virtual string ServiceName { get; set; }

        public virtual IList<string> DestinationPrefixes
        {
            get
            {
                return _destinationPrefixes;
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                _destinationPrefixes.Clear();
                if (value != null)
                {
                    foreach (var prefix in value)
                    {
                        _destinationPrefixes.Add(prefix.Trim());
                    }
                }
            }
        }

        public virtual IList<IHandlerMethodArgumentResolver> CustomArgumentResolvers
        {
            get
            {
                return _customArgumentResolvers;
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                _customArgumentResolvers.Clear();
                if (value != null)
                {
                    _customArgumentResolvers.AddRange(value);
                }
            }
        }

        public virtual IList<IHandlerMethodArgumentResolver> ArgumentResolvers
        {
#pragma warning disable S4275 // Getters and setters should access the expected fields
            get
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                return MethodArgumentResolvers.Resolvers;
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                if (value == null)
                {
                    MethodArgumentResolvers.Clear();
                    return;
                }

                MethodArgumentResolvers.AddResolvers(value);
            }
        }

        public virtual IList<IHandlerMethodReturnValueHandler> ReturnValueHandlers
        {
#pragma warning disable S4275 // Getters and setters should access the expected fields
            get
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                return MethodReturnValueHandlers.ReturnValueHandlers;
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                if (value == null)
                {
                    MethodReturnValueHandlers.Clear();
                    return;
                }

                MethodReturnValueHandlers.AddHandlers(value);
            }
        }

        public virtual IList<IHandlerMethodReturnValueHandler> CustomReturnValueHandlers
        {
            get
            {
                return _customReturnValueHandlers;
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                _customReturnValueHandlers.Clear();
                if (value != null)
                {
                    _customReturnValueHandlers.AddRange(value);
                }
            }
        }

        public virtual IDictionary<T, HandlerMethod> HandlerMethods
        {
            get
            {
                return new Dictionary<T, HandlerMethod>(_handlerMethods);
            }
        }

        public virtual void HandleMessage(IMessage message)
        {
            var destination = GetDestination(message);
            if (destination == null)
            {
                return;
            }

            var lookupDestination = GetLookupDestination(destination);
            if (lookupDestination == null)
            {
                return;
            }

            var headerAccessor = MessageHeaderAccessor.GetMutableAccessor(message);
            headerAccessor.SetHeader(DestinationPatternsMessageCondition.LOOKUP_DESTINATION_HEADER, lookupDestination);
            headerAccessor.LeaveMutable = true;
            message = MessageBuilder.CreateMessage(message.Payload, headerAccessor.MessageHeaders);

            HandleMessageInternal(message, lookupDestination);
            headerAccessor.SetImmutable();
        }

        public override string ToString()
        {
            return GetType().Name + "[prefixes=" + string.Join(",", DestinationPrefixes) + "]";
        }

        protected HandlerMethodArgumentResolverComposite MethodArgumentResolvers
        {
            get
            {
                if (_argumentResolvers.Resolvers.Count == 0)
                {
                    _argumentResolvers.AddResolvers(InitArgumentResolvers());
                }

                return _argumentResolvers;
            }
        }

        protected HandlerMethodReturnValueHandlerComposite MethodReturnValueHandlers
        {
            get
            {
                if (_returnValueHandlers.ReturnValueHandlers.Count == 0)
                {
                    _returnValueHandlers.AddHandlers(InitReturnValueHandlers());
                }

                return _returnValueHandlers;
            }
        }

        protected abstract IList<IHandlerMethodArgumentResolver> InitArgumentResolvers();

        protected abstract IList<IHandlerMethodReturnValueHandler> InitReturnValueHandlers();

        protected abstract T GetMappingForMethod(MethodInfo method, Type handlerType);

        protected abstract string GetDestination(IMessage message);

        protected abstract ISet<string> GetDirectLookupDestinations(T mapping);

        protected abstract T GetMatchingMapping(T mapping, IMessage message);

        protected abstract IComparer<T> GetMappingComparer(IMessage message);

        protected abstract AbstractExceptionHandlerMethodResolver CreateExceptionHandlerMethodResolverFor(Type beanType);

        protected void DetectHandlerMethods(object handler)
        {
            Type handlerType = null;
            if (handler is string)
            {
                // ApplicationContext context = getApplicationContext();
                // Assert.state(context != null, "ApplicationContext is required for resolving handler bean names");
                // handlerType = context.getType((String)handler);
            }
            else
            {
                handlerType = handler.GetType();
            }

            if (handlerType != null)
            {
                var results = new Dictionary<MethodInfo, T>();
                var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    var mapping = GetMappingForMethod(method, handlerType);
                    if (mapping != null)
                    {
                        results.Add(method, mapping);
                    }
                }

                foreach (var entry in results)
                {
                    RegisterHandlerMethod(handler, entry.Key, entry.Value);
                }
            }
        }

        protected virtual string GetLookupDestination(string destination)
        {
            if (destination == null)
            {
                return null;
            }

            if (_destinationPrefixes.Count == 0)
            {
                return destination;
            }

            for (var i = 0; i < _destinationPrefixes.Count; i++)
            {
                var prefix = _destinationPrefixes[i];
                if (destination.StartsWith(prefix))
                {
                    return destination[prefix.Length..];
                }
            }

            return null;
        }

        protected virtual void RegisterHandlerMethod(object handler, MethodInfo method, T mapping)
        {
            if (mapping == null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            var newHandlerMethod = CreateHandlerMethod(handler, method);
            _handlerMethods.TryGetValue(mapping, out var oldHandlerMethod);

            if (oldHandlerMethod != null && !oldHandlerMethod.Equals(newHandlerMethod))
            {
                throw new InvalidOperationException("Ambiguous mapping found. Cannot map '" + newHandlerMethod.Bean +
                        "' bean method \n" + newHandlerMethod + "\nto " + mapping + ": There is already '" +
                        oldHandlerMethod.Bean + "' bean method\n" + oldHandlerMethod + " mapped.");
            }

            _handlerMethods[mapping] = newHandlerMethod;

            foreach (var pattern in GetDirectLookupDestinations(mapping))
            {
                if (_destinationLookup.TryGetValue(pattern, out var list))
                {
                    list.Add(mapping);
                }
                else
                {
                    _destinationLookup.Add(pattern, new List<T> { mapping });
                }
            }
        }

        protected virtual HandlerMethod CreateHandlerMethod(object handler, MethodInfo method)
        {
            HandlerMethod handlerMethod;
            handlerMethod = new HandlerMethod(handler, method);
            return handlerMethod;
        }

        protected virtual void HandleMessageInternal(IMessage message, string lookupDestination)
        {
            var matches = new List<Match>();

            _destinationLookup.TryGetValue(lookupDestination, out var mappingsByUrl);
            if (mappingsByUrl != null)
            {
                AddMatchesToCollection(mappingsByUrl, message, matches);
            }

            if (matches.Count == 0)
            {
                // No direct hits, go through all mappings
                var allMappings = _handlerMethods.Keys;
                AddMatchesToCollection(allMappings, message, matches);
            }

            if (matches.Count == 0)
            {
                HandleNoMatch(_handlerMethods.Keys, lookupDestination, message);
                return;
            }

            var comparator = new MatchComparer(GetMappingComparer(message));
            matches.Sort(comparator);

            var bestMatch = matches[0];
            if (matches.Count > 1)
            {
                var secondBestMatch = matches[1];
                if (comparator.Compare(bestMatch, secondBestMatch) == 0)
                {
                    var m1 = bestMatch.HandlerMethod.Method;
                    var m2 = secondBestMatch.HandlerMethod.Method;
                    throw new InvalidOperationException("Ambiguous handler methods mapped for destination '" +
                            lookupDestination + "': {" + m1 + ", " + m2 + "}");
                }
            }

            HandleMatch(bestMatch.Mapping, bestMatch.HandlerMethod, lookupDestination, message);
        }

        protected virtual void HandleMatch(T mapping, HandlerMethod handlerMethod, string lookupDestination, IMessage message)
        {
            handlerMethod = handlerMethod.CreateWithResolvedBean();
            var invocable = new InvocableHandlerMethod(handlerMethod, _logger)
            {
                MessageMethodArgumentResolvers = MethodArgumentResolvers
            };
            try
            {
                var returnValue = invocable.Invoke(message);
                var returnType = handlerMethod.ReturnType;
                if (returnType.ParameterType == typeof(void))
                {
                    return;
                }

                if (returnValue != null && MethodReturnValueHandlers.IsAsyncReturnValue(returnValue, returnType))
                {
                    // TODO: Async; var task = returnValue as Task;
                    throw new NotImplementedException("Async still todo");
                }
                else
                {
                    MethodReturnValueHandlers.HandleReturnValue(returnValue, returnType, message);
                }
            }
            catch (Exception ex)
            {
                Exception handlingException =
                        new MessageHandlingException(message, "Unexpected handler method invocation error", ex);
                ProcessHandlerMethodException(handlerMethod, handlingException, message);
            }
        }

        protected virtual void ProcessHandlerMethodException(HandlerMethod handlerMethod, Exception exception, IMessage message)
        {
            var invocable = GetExceptionHandlerMethod(handlerMethod, exception);
            if (invocable == null)
            {
                _logger?.LogError(exception, "Unhandled exception from message handler method");
                return;
            }

            invocable.MessageMethodArgumentResolvers = MethodArgumentResolvers;

            try
            {
                var cause = exception.InnerException;
                var returnValue = cause != null ?
                        invocable.Invoke(message, exception, cause, handlerMethod) :
                        invocable.Invoke(message, exception, handlerMethod);
                var returnType = invocable.ReturnType;
                if (returnType.ParameterType == typeof(void))
                {
                    return;
                }

                MethodReturnValueHandlers.HandleReturnValue(returnValue, returnType, message);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error while processing handler method exception");
            }
        }

        protected virtual InvocableHandlerMethod GetExceptionHandlerMethod(HandlerMethod handlerMethod, Exception exception)
        {
            var beanType = handlerMethod.BeanType;
            _exceptionHandlerCache.TryGetValue(beanType, out var resolver);
            if (resolver == null)
            {
                resolver = CreateExceptionHandlerMethodResolverFor(beanType);
                _exceptionHandlerCache[beanType] = resolver;
            }

            var method = resolver.ResolveMethod(exception);
            if (method != null)
            {
                return new InvocableHandlerMethod(handlerMethod.Bean, method);
            }

            return null;
        }

        protected virtual Task HandleNoMatch(ICollection<T> ts, string lookupDestination, IMessage message)
        {
            return Task.CompletedTask;
        }

        private void AddMatchesToCollection(ICollection<T> mappingsToCheck, IMessage message, List<Match> matches)
        {
            foreach (var mapping in mappingsToCheck)
            {
                var match = GetMatchingMapping(mapping, message);
                if (match != null)
                {
                    matches.Add(new Match(match, _handlerMethods[mapping]));
                }
            }
        }

        protected class Match
        {
            public readonly T Mapping;

            public readonly HandlerMethod HandlerMethod;

            public Match(T mapping, HandlerMethod handlerMethod)
            {
                Mapping = mapping;
                HandlerMethod = handlerMethod;
            }

            public override string ToString()
            {
                return Mapping.ToString();
            }
        }

        protected class MatchComparer : IComparer<Match>
        {
            private readonly IComparer<T> _comparator;

            public MatchComparer(IComparer<T> comparator)
            {
                _comparator = comparator;
            }

            public int Compare(Match x, Match y)
            {
                return _comparator.Compare(x.Mapping, y.Mapping);
            }
        }
    }
}
