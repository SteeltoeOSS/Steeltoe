// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using System;
using System.Reflection;
using Xunit;

namespace Steeltoe.Messaging.Handler.Invocation.Test
{
    public class InvocableHandlerMethodTest
    {
        private HandlerMethodArgumentResolverComposite resolvers;
        private IMessage message;

        [Fact]
        public void ResolveArg()
        {
            var messageMock = new Mock<IMessage>();
            message = messageMock.Object;

            resolvers = new HandlerMethodArgumentResolverComposite();
            resolvers.AddResolver(new StubArgumentResolver(99));
            resolvers.AddResolver(new StubArgumentResolver("value"));
            var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
            var value = Invoke(new Handler(), method);

            Assert.Single(GetStubResolver(0).ResolvedParameters);
            Assert.Single(GetStubResolver(1).ResolvedParameters);
            Assert.Equal("99-value", value);
            Assert.Equal("intArg", GetStubResolver(0).ResolvedParameters[0].Name);
            Assert.Equal("stringArg", GetStubResolver(1).ResolvedParameters[0].Name);
        }

        [Fact]
        public void ResolveNoArgValue()
        {
            var messageMock = new Mock<IMessage>();
            message = messageMock.Object;

            resolvers = new HandlerMethodArgumentResolverComposite();
            resolvers.AddResolver(new StubArgumentResolver(typeof(int?)));
            resolvers.AddResolver(new StubArgumentResolver(typeof(string)));
            var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });

            var value = Invoke(new Handler(), method);

            Assert.Single(GetStubResolver(0).ResolvedParameters);
            Assert.Single(GetStubResolver(1).ResolvedParameters);
            Assert.Equal("null-null", value);
        }

        [Fact]
        public void CannotResolveArg()
        {
            var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
            var messageMock = new Mock<IMessage>();
            message = messageMock.Object;

            resolvers = new HandlerMethodArgumentResolverComposite();
            var ex = Assert.Throws<MethodArgumentResolutionException>(() => Invoke(new Handler(), method));
            Assert.Contains("Could not resolve parameter [0]", ex.Message);
        }

        [Fact]
        public void ResolveProvidedArg()
        {
            var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
            var messageMock = new Mock<IMessage>();
            message = messageMock.Object;
            resolvers = new HandlerMethodArgumentResolverComposite();

            var value = Invoke(new Handler(), method, 99, "value");

            Assert.NotNull(value);
            Assert.IsType<string>(value);
            Assert.Equal("99-value", value);
        }

        [Fact]
        public void ResolveProvidedArgFirst()
        {
            var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
            var messageMock = new Mock<IMessage>();
            message = messageMock.Object;
            resolvers = new HandlerMethodArgumentResolverComposite();

            resolvers.AddResolver(new StubArgumentResolver(1));
            resolvers.AddResolver(new StubArgumentResolver("value1"));
            var value = Invoke(new Handler(), method, 2, "value2");

            Assert.Equal("2-value2", value);
        }

        [Fact]
        public void ExceptionInResolvingArg()
        {
            var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
            var messageMock = new Mock<IMessage>();
            message = messageMock.Object;
            resolvers = new HandlerMethodArgumentResolverComposite();

            resolvers.AddResolver(new ExceptionRaisingArgumentResolver());
            Assert.Throws<ArgumentException>(() => Invoke(new Handler(), method));

            // expected -  allow HandlerMethodArgumentResolver exceptions to propagate
        }

        [Fact]
        public void IllegalArgumentException()
        {
            var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
            var messageMock = new Mock<IMessage>();
            message = messageMock.Object;
            resolvers = new HandlerMethodArgumentResolverComposite();

            resolvers.AddResolver(new StubArgumentResolver(typeof(int?), "__not_an_int__"));
            resolvers.AddResolver(new StubArgumentResolver("value"));
            var ex = Assert.Throws<InvalidOperationException>(() => Invoke(new Handler(), method));
            Assert.IsType<ArgumentException>(ex.InnerException);
            Assert.Contains("Endpoint [", ex.Message);
            Assert.Contains("Method [", ex.Message);
            Assert.Contains("with argument values:", ex.Message);
            Assert.Contains("[0] [type=System.String] [value=__not_an_int__]", ex.Message);
            Assert.Contains("[1] [type=System.String] [value=value", ex.Message);
        }

        [Fact]
        public void InvocationTargetException()
        {
            var handler = new Handler();
            var messageMock = new Mock<IMessage>();
            message = messageMock.Object;
            resolvers = new HandlerMethodArgumentResolverComposite();
            var method = typeof(Handler).GetMethod("HandleWithException");

            var runtimeException = new Exception("error");
            var ex = Assert.Throws<Exception>(() => Invoke(handler, method, runtimeException));
            Assert.Same(runtimeException, ex);

            var error = new IndexOutOfRangeException("error");
            var ex2 = Assert.Throws<IndexOutOfRangeException>(() => Invoke(handler, method, error));
            Assert.Same(error, ex2);
        }

        private object Invoke(object handler, MethodInfo method, params object[] providedArgs)
        {
            var handlerMethod = new InvocableHandlerMethod(handler, method)
            {
                MessageMethodArgumentResolvers = resolvers
            };
            return handlerMethod.Invoke(message, providedArgs);
        }

        private StubArgumentResolver GetStubResolver(int index)
        {
            return (StubArgumentResolver)resolvers.Resolvers[index];
        }

        internal class Handler
        {
            public string Handle(int? intArg, string stringArg)
            {
                return (intArg == null ? "null" : intArg.Value.ToString()) + "-" + (stringArg == null ? "null" : stringArg);
            }

            public void Handle(double amount)
            {
            }

            public void HandleWithException(Exception ex)
            {
                throw ex;
            }
        }

        internal class ExceptionRaisingArgumentResolver : IHandlerMethodArgumentResolver
        {
            public bool SupportsParameter(ParameterInfo parameter)
            {
                return true;
            }

            public object ResolveArgument(ParameterInfo parameter, IMessage message)
            {
                throw new ArgumentException("oops, can't read");
            }
        }
    }
}
