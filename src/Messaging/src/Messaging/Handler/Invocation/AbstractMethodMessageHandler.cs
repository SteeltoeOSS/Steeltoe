// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Messaging.Handler.Invocation;

public abstract class AbstractMethodMessageHandler<T> : IMessageHandler
{
    private readonly List<string> _destinationPrefixes = new();
    private readonly List<IHandlerMethodArgumentResolver> _customArgumentResolvers = new();
    private readonly List<IHandlerMethodReturnValueHandler> _customReturnValueHandlers = new();
    private readonly HandlerMethodArgumentResolverComposite _argumentResolvers = new();
    private readonly HandlerMethodReturnValueHandlerComposite _returnValueHandlers = new();
    private readonly Dictionary<T, HandlerMethod> _handlerMethods = new(64);
    private readonly Dictionary<string, List<T>> _destinationLookup = new(64);
    private readonly ConcurrentDictionary<Type, AbstractExceptionHandlerMethodResolver> _exceptionHandlerCache = new();
    private readonly ILogger _logger;

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

    public virtual string ServiceName { get; set; }

    public virtual IList<string> DestinationPrefixes
    {
        get => _destinationPrefixes;
        set
        {
            _destinationPrefixes.Clear();

            if (value != null)
            {
                foreach (string prefix in value)
                {
                    _destinationPrefixes.Add(prefix.Trim());
                }
            }
        }
    }

    public virtual IList<IHandlerMethodArgumentResolver> CustomArgumentResolvers
    {
        get => _customArgumentResolvers;
        set
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
        get => MethodArgumentResolvers.Resolvers;
        set
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
        get => MethodReturnValueHandlers.ReturnValueHandlers;
        set
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
        get => _customReturnValueHandlers;
        set
        {
            _customReturnValueHandlers.Clear();

            if (value != null)
            {
                _customReturnValueHandlers.AddRange(value);
            }
        }
    }

    public virtual IDictionary<T, HandlerMethod> HandlerMethods => new Dictionary<T, HandlerMethod>(_handlerMethods);

    protected AbstractMethodMessageHandler(ILogger logger = null)
    {
        ServiceName = $"{GetType().Name}@{GetHashCode()}";
        _logger = logger;
    }

    public virtual void HandleMessage(IMessage message)
    {
        string destination = GetDestination(message);

        if (destination == null)
        {
            return;
        }

        string lookupDestination = GetLookupDestination(destination);

        if (lookupDestination == null)
        {
            return;
        }

        MessageHeaderAccessor headerAccessor = MessageHeaderAccessor.GetMutableAccessor(message);
        headerAccessor.SetHeader(DestinationPatternsMessageCondition.LookupDestinationHeader, lookupDestination);
        headerAccessor.LeaveMutable = true;
        message = MessageBuilder.CreateMessage(message.Payload, headerAccessor.MessageHeaders);

        HandleMessageInternal(message, lookupDestination);
        headerAccessor.SetImmutable();
    }

    public override string ToString()
    {
        return $"{GetType().Name}[prefixes={string.Join(",", DestinationPrefixes)}]";
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
            MethodInfo[] methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            foreach (MethodInfo method in methods)
            {
                T mapping = GetMappingForMethod(method, handlerType);

                if (mapping != null)
                {
                    results.Add(method, mapping);
                }
            }

            foreach (KeyValuePair<MethodInfo, T> entry in results)
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

        foreach (string prefix in _destinationPrefixes)
        {
            if (destination.StartsWith(prefix))
            {
                return destination[prefix.Length..];
            }
        }

        return null;
    }

    protected virtual void RegisterHandlerMethod(object handler, MethodInfo method, T mapping)
    {
        ArgumentGuard.NotNull(mapping);

        HandlerMethod newHandlerMethod = CreateHandlerMethod(handler, method);
        _handlerMethods.TryGetValue(mapping, out HandlerMethod oldHandlerMethod);

        if (oldHandlerMethod != null && !oldHandlerMethod.Equals(newHandlerMethod))
        {
            throw new InvalidOperationException(
                $"Ambiguous mapping found. Cannot map '{newHandlerMethod.Handler}' bean method \n{newHandlerMethod}\nto {mapping}: There is already '{oldHandlerMethod.Handler}' bean method\n{oldHandlerMethod} mapped.");
        }

        _handlerMethods[mapping] = newHandlerMethod;

        foreach (string pattern in GetDirectLookupDestinations(mapping))
        {
            if (_destinationLookup.TryGetValue(pattern, out List<T> list))
            {
                list.Add(mapping);
            }
            else
            {
                _destinationLookup.Add(pattern, new List<T>
                {
                    mapping
                });
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

        _destinationLookup.TryGetValue(lookupDestination, out List<T> mappingsByUrl);

        if (mappingsByUrl != null)
        {
            AddMatchesToCollection(mappingsByUrl, message, matches);
        }

        if (matches.Count == 0)
        {
            // No direct hits, go through all mappings
            Dictionary<T, HandlerMethod>.KeyCollection allMappings = _handlerMethods.Keys;
            AddMatchesToCollection(allMappings, message, matches);
        }

        if (matches.Count == 0)
        {
            HandleNoMatchAsync(_handlerMethods.Keys, lookupDestination, message);
            return;
        }

        var comparator = new MatchComparer(GetMappingComparer(message));
        matches.Sort(comparator);

        Match bestMatch = matches[0];

        if (matches.Count > 1)
        {
            Match secondBestMatch = matches[1];

            if (comparator.Compare(bestMatch, secondBestMatch) == 0)
            {
                MethodInfo m1 = bestMatch.HandlerMethod.Method;
                MethodInfo m2 = secondBestMatch.HandlerMethod.Method;
                throw new InvalidOperationException($"Ambiguous handler methods mapped for destination '{lookupDestination}': {{{m1}, {m2}}}");
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
            object returnValue = invocable.Invoke(message);
            ParameterInfo returnType = handlerMethod.ReturnType;

            if (returnType.ParameterType == typeof(void))
            {
                return;
            }

            if (returnValue != null && MethodReturnValueHandlers.IsAsyncReturnValue(returnValue, returnType))
            {
                // TODO: Async; var task = returnValue as Task;
                throw new NotImplementedException("Async still todo");
            }

            MethodReturnValueHandlers.HandleReturnValue(returnValue, returnType, message);
        }
        catch (Exception ex)
        {
            Exception handlingException = new MessageHandlingException(message, "Unexpected handler method invocation error", ex);
            ProcessHandlerMethodException(handlerMethod, handlingException, message);
        }
    }

    protected virtual void ProcessHandlerMethodException(HandlerMethod handlerMethod, Exception exception, IMessage message)
    {
        InvocableHandlerMethod invocable = GetExceptionHandlerMethod(handlerMethod, exception);

        if (invocable == null)
        {
            _logger?.LogError(exception, "Unhandled exception from message handler method");
            return;
        }

        invocable.MessageMethodArgumentResolvers = MethodArgumentResolvers;

        try
        {
            Exception cause = exception.InnerException;

            object returnValue =
                cause != null ? invocable.Invoke(message, exception, cause, handlerMethod) : invocable.Invoke(message, exception, handlerMethod);

            ParameterInfo returnType = invocable.ReturnType;

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
        Type beanType = handlerMethod.HandlerType;
        _exceptionHandlerCache.TryGetValue(beanType, out AbstractExceptionHandlerMethodResolver resolver);

        if (resolver == null)
        {
            resolver = CreateExceptionHandlerMethodResolverFor(beanType);
            _exceptionHandlerCache[beanType] = resolver;
        }

        MethodInfo method = resolver.ResolveMethod(exception);

        if (method != null)
        {
            return new InvocableHandlerMethod(handlerMethod.Handler, method);
        }

        return null;
    }

    protected virtual Task HandleNoMatchAsync(ICollection<T> ts, string lookupDestination, IMessage message)
    {
        return Task.CompletedTask;
    }

    private void AddMatchesToCollection(ICollection<T> mappingsToCheck, IMessage message, List<Match> matches)
    {
        foreach (T mapping in mappingsToCheck)
        {
            T match = GetMatchingMapping(mapping, message);

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
