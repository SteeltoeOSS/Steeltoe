// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Steeltoe.Messaging.Handler.Invocation.Test
{
    public class MethodMessageHandlerTest
    {
        private const string _destinationHeader = "destination";

        private readonly TestMethodMessageHandler _messageHandler;

        private readonly TestController _testController;

        public MethodMessageHandlerTest()
        {
            var destinationPrefixes = new List<string>() { "/test" };

            _messageHandler = new TestMethodMessageHandler
            {
                // this.messageHandler.setApplicationContext(new StaticApplicationContext());
                DestinationPrefixes = destinationPrefixes
            };

            // this.messageHandler.afterPropertiesSet();
            _testController = new TestController();
            _messageHandler.RegisterHandler(_testController);
        }

        [Fact]
        public void DuplicateMapping()
        {
            Assert.Throws<InvalidOperationException>(() => _messageHandler.RegisterHandler(new DuplicateMappingsController()));
        }

        [Fact]
        public void RegisteredMappings()
        {
            var handlerMethods = _messageHandler.HandlerMethods;

            Assert.NotNull(handlerMethods);
            Assert.Equal(3, handlerMethods.Count);
        }

        [Fact]
        public void PatternMatch()
        {
            var method = _testController.GetType().GetMethod("HandlerPathMatchWildcard");
            _messageHandler.RegisterHandlerMethodPublic(_testController, method, "/handlerPathMatch*");

            _messageHandler.HandleMessage(ToDestination("/test/handlerPathMatchFoo"));

            Assert.Equal("PathMatchWildcard", _testController.Method);
        }

        [Fact]
        public void BestMatch()
        {
            var method = _testController.GetType().GetMethod("BestMatch");
            _messageHandler.RegisterHandlerMethodPublic(_testController, method, "/bestmatch/{foo}/path");

            method = _testController.GetType().GetMethod("SecondBestMatch");
            _messageHandler.RegisterHandlerMethodPublic(_testController, method, "/bestmatch/*/*");

            _messageHandler.HandleMessage(ToDestination("/test/bestmatch/bar/path"));

            Assert.Equal("BestMatch", _testController.Method);
        }

        [Fact]
        public void ArgumentResolution()
        {
            _messageHandler.HandleMessage(ToDestination("/test/HandlerArgumentResolver"));

            Assert.Equal("HandlerArgumentResolver", _testController.Method);
            Assert.NotNull(_testController.Arguments["message"]);
        }

        [Fact]
        public void HandleException()
        {
            _messageHandler.HandleMessage(ToDestination("/test/HandlerThrowsExc"));

            Assert.Equal("InvalidOperationException", _testController.Method);
            Assert.NotNull(_testController.Arguments["exception"]);
        }

        private IMessage ToDestination(string destination)
        {
            return MessageBuilder.WithPayload(Array.Empty<byte>()).SetHeader(_destinationHeader, destination).Build();
        }

        internal class TestController
        {
            public Dictionary<string, object> Arguments { get; } = new Dictionary<string, object>();

            public string Method { get; set; }

            public void HandlerPathMatchWildcard()
            {
                Method = "PathMatchWildcard";
            }

            public void HandlerArgumentResolver(IMessage message)
            {
                Method = "HandlerArgumentResolver";
                Arguments.Add("message", message);
            }

            public void HandlerThrowsExc()
            {
                throw new InvalidOperationException();
            }

            public void BestMatch()
            {
                Method = "BestMatch";
            }

            public void SecondBestMatch()
            {
                Method = "SecondBestMatch";
            }

            public void HandleInvalidOperationException(InvalidOperationException exception)
            {
                Method = "InvalidOperationException";
                Arguments.Add("exception", exception);
            }
        }

        internal class DuplicateMappingsController
        {
            public void HandlerFoo()
            {
            }

            public void HandlerFoo(string arg)
            {
            }
        }

        internal class TestMethodMessageHandler : AbstractMethodMessageHandler<string>
        {
            private readonly IPathMatcher _pathMatcher = new AntPathMatcher();

            public void RegisterHandler(object handler)
            {
                DetectHandlerMethods(handler);
            }

            public void RegisterHandlerMethodPublic(object handler, MethodInfo method, string mapping)
            {
                RegisterHandlerMethod(handler, method, mapping);
            }

            protected override IList<IHandlerMethodArgumentResolver> InitArgumentResolvers()
            {
                var resolvers = new List<IHandlerMethodArgumentResolver>
                {
                    new MessageMethodArgumentResolver(new SimpleMessageConverter())
                };
                resolvers.AddRange(CustomArgumentResolvers);
                return resolvers;
            }

            protected override IList<IHandlerMethodReturnValueHandler> InitReturnValueHandlers()
            {
                var handlers = new List<IHandlerMethodReturnValueHandler>();
                handlers.AddRange(CustomReturnValueHandlers);
                return handlers;
            }

            protected bool IsHandler(Type beanType)
            {
                return beanType.Name.Contains("Controller");
            }

            protected override string GetMappingForMethod(MethodInfo method, Type handlerType)
            {
                var methodName = method.Name;
                if (methodName.StartsWith("Handler"))
                {
                    return "/" + methodName;
                }

                return null;
            }

            protected override ISet<string> GetDirectLookupDestinations(string mapping)
            {
                ISet<string> result = new HashSet<string>();
                if (!_pathMatcher.IsPattern(mapping))
                {
                    result.Add(mapping);
                }

                return result;
            }

            protected override string GetDestination(IMessage message)
            {
                return (string)message.Headers[_destinationHeader];
            }

            protected override string GetMatchingMapping(string mapping, IMessage message)
            {
                var destination = GetLookupDestination(GetDestination(message));
                Assert.NotNull(destination);
                return mapping.Equals(destination) || _pathMatcher.Match(mapping, destination) ? mapping : null;
            }

            protected override IComparer<string> GetMappingComparer(IMessage message)
            {
                return new MappingComparer(message);
            }

            protected override AbstractExceptionHandlerMethodResolver CreateExceptionHandlerMethodResolverFor(Type beanType)
            {
                return new TestExceptionResolver(beanType);
            }

            internal class MappingComparer : IComparer<string>
            {
                private readonly IMessage _message;

                public MappingComparer(IMessage message)
                {
                    _message = message;
                }

                public int Compare(string info1, string info2)
                {
                    var cond1 = new DestinationPatternsMessageCondition(info1);
                    var cond2 = new DestinationPatternsMessageCondition(info2);
                    return cond1.CompareTo(cond2, _message);
                }
            }
        }
    }
}
