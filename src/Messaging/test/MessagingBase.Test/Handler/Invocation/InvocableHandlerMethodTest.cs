// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Messaging.Handler.Invocation.Test;

public class InvocableHandlerMethodTest
{
    private HandlerMethodArgumentResolverComposite _resolvers;
    private IMessage _message;
    private ITestOutputHelper _outputHelper;

    public InvocableHandlerMethodTest(ITestOutputHelper output)
    {
        _outputHelper = output;
    }

    [Fact]
    public void ResolveArg()
    {
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;

        _resolvers = new HandlerMethodArgumentResolverComposite();
        _resolvers.AddResolver(new StubArgumentResolver(99));
        _resolvers.AddResolver(new StubArgumentResolver("value"));
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
        _message = messageMock.Object;

        _resolvers = new HandlerMethodArgumentResolverComposite();
        _resolvers.AddResolver(new StubArgumentResolver(typeof(int?)));
        _resolvers.AddResolver(new StubArgumentResolver(typeof(string)));
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
        _message = messageMock.Object;

        _resolvers = new HandlerMethodArgumentResolverComposite();
        var ex = Assert.Throws<MethodArgumentResolutionException>(() => Invoke(new Handler(), method));
        Assert.Contains("Could not resolve parameter [0]", ex.Message);
    }

    [Fact]
    public void ResolveProvidedArg()
    {
        var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();

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
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();

        _resolvers.AddResolver(new StubArgumentResolver(1));
        _resolvers.AddResolver(new StubArgumentResolver("value1"));
        var value = Invoke(new Handler(), method, 2, "value2");

        Assert.Equal("2-value2", value);
    }

    [Fact]
    public void ExceptionInResolvingArg()
    {
        var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();

        _resolvers.AddResolver(new ExceptionRaisingArgumentResolver());
        Assert.Throws<ArgumentException>(() => Invoke(new Handler(), method));

        // expected -  allow HandlerMethodArgumentResolver exceptions to propagate
    }

    [Fact]
    public void IllegalArgumentException()
    {
        var method = typeof(Handler).GetMethod("Handle", new Type[] { typeof(int?), typeof(string) });
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();

        _resolvers.AddResolver(new StubArgumentResolver(typeof(int?), "__not_an_int__"));
        _resolvers.AddResolver(new StubArgumentResolver("value"));
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
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler).GetMethod("HandleWithException");

        var runtimeException = new Exception("error");
        var ex = Assert.Throws<Exception>(() => Invoke(handler, method, runtimeException));
        Assert.Same(runtimeException, ex);

        var error = new IndexOutOfRangeException("error");
        var ex2 = Assert.Throws<IndexOutOfRangeException>(() => Invoke(handler, method, error));
        Assert.Same(error, ex2);
    }

    [Fact]
    public void HandleSinglePrimitiveReturnVoid()
    {
        var handler = new Handler2();
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler2).GetMethod("HandleSinglePrimitiveReturnVoid");
        Invoke(handler, method, 1.0d);
        Assert.Equal(1.0d, handler.DoubleValue);
    }

    [Fact]
    public void HandleSinglePrimitiveReturnVoidPerf()
    {
        var handler = new Handler2();
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler2).GetMethod("HandleSinglePrimitiveReturnVoid");

        Invoke(handler, method, 1.0d);
        Assert.Equal(1.0d, handler.DoubleValue);
    }

    [Fact]
    public void HandleMultiPrimitiveReturnVoid()
    {
        var handler = new Handler2();
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler2).GetMethod("HandleMultiPrimitiveReturnVoid");
        Invoke(handler, method, 1.0d, 2);
        Assert.Equal(1.0d, handler.DoubleValue);
        Assert.Equal(2, handler.IntValue);
    }

    [Fact]
    public void HandleMultiReturnVoid()
    {
        var handler = new Handler2();
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler2).GetMethod("HandleMultiReturnVoid");
        Invoke(handler, method, 1.0d, 2, handler);
        Assert.Equal(1.0d, handler.DoubleValue);
        Assert.Equal(2, handler.IntValue);
        Assert.Same(handler, handler.ObjectValue);
    }

    [Fact]
    public void HandleNullablePrimitive()
    {
        var handler = new Handler2();
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler2).GetMethod("HandleNullablePrimitive");
        var result = Invoke(handler, method, 10, "stringArg");
        Assert.Equal(1, handler.InvocationCount);
        Assert.Equal("10-stringArg", result);
    }

    [Fact]
    public void ComparePerfHandleSinglePrimitiveReturnVoid()
    {
        var handler = new Handler2();
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler2).GetMethod("HandleSinglePrimitiveReturnVoid");
        var ticks1 = TimedInvoke(handler, method, 100_000, 1.0d);
        Assert.Equal(100_000, handler.InvocationCount);
        var ticks2 = TimedReflectionInvoke(handler, method, 100_000, 1.0d);
        Assert.Equal(200_000, handler.InvocationCount);
        Assert.True(ticks2 > ticks1);
    }

    [Fact]
    public async Task HandleAsyncVoidMethod()
    {
        var handler = new Handler2();
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler2).GetMethod("HandleAsyncVoidMethod");
        var result = Invoke(handler, method, 1.0d) as Task;
        await result;
        Assert.Equal(1, handler.InvocationCount);
        Assert.Equal(1.0d, handler.DoubleValue);
    }

    [Fact]
    public async Task HandleAsyncStringMethod()
    {
        var handler = new Handler2();
        var messageMock = new Mock<IMessage>();
        _message = messageMock.Object;
        _resolvers = new HandlerMethodArgumentResolverComposite();
        var method = typeof(Handler2).GetMethod("HandleAsyncStringMethod");
        var result = Invoke(handler, method, 10, "stringArg") as Task<string>;
        var str = await result;
        Assert.Equal(1, handler.InvocationCount);
        Assert.Equal("10-stringArg", str);
    }

    private object Invoke(object handler, MethodInfo method, params object[] providedArgs)
    {
        var handlerMethod = new InvocableHandlerMethod(handler, method)
        {
            MessageMethodArgumentResolvers = _resolvers
        };
        return handlerMethod.Invoke(_message, providedArgs);
    }

    private long TimedInvoke(object handler, MethodInfo method, int count, params object[] providedArgs)
    {
        var handlerMethod = new InvocableHandlerMethod(handler, method);
        var invoker = handlerMethod.HandlerInvoker;

        var start = DateTime.Now.Ticks;
        for (var i = 0; i < count; i++)
        {
            invoker(handler, providedArgs);
        }

        var end = DateTime.Now.Ticks;
        _outputHelper.WriteLine("Ticks: " + (end - start));
        return end - start;
    }

    private long TimedReflectionInvoke(object handler, MethodInfo method, int count, params object[] providedArgs)
    {
        var start = DateTime.Now.Ticks;
        for (var i = 0; i < count; i++)
        {
            method.Invoke(handler, providedArgs);
        }

        var end = DateTime.Now.Ticks;
        _outputHelper.WriteLine("Ticks: " + (end - start));
        return end - start;
    }

    private StubArgumentResolver GetStubResolver(int index)
    {
        return (StubArgumentResolver)_resolvers.Resolvers[index];
    }

    internal class Handler
    {
        public string Handle(int? intArg, string stringArg) => (intArg.HasValue ? intArg.Value.ToString() : "null") + "-" + (stringArg ?? "null");

        public void Handle(double amount)
        {
        }

        public void HandleWithException(Exception ex)
        {
            throw ex;
        }
    }

    internal class Handler2
    {
        public long InvocationCount;
        public double DoubleValue;
        public int IntValue;
        public object ObjectValue;

        public string HandleNullablePrimitive(int? intArg, string stringArg)
        {
            InvocationCount++;
            return (intArg == null ? "null" : intArg.Value.ToString()) + "-" + (stringArg ?? "null");
        }

        public void HandleSinglePrimitiveReturnVoid(double value)
        {
            InvocationCount++;
            DoubleValue = value;
        }

        public void HandleMultiPrimitiveReturnVoid(double value1, int value2)
        {
            InvocationCount++;
            DoubleValue = value1;
            IntValue = value2;
        }

        public void HandleMultiReturnVoid(double value1, int value2, Handler2 value3)
        {
            InvocationCount++;
            DoubleValue = value1;
            IntValue = value2;
            ObjectValue = value3;
        }

        public void HandleWithException(Exception ex)
        {
            InvocationCount++;
            throw ex;
        }

        public Task HandleAsyncVoidMethod(double value)
        {
            InvocationCount++;
            DoubleValue = value;
            return Task.CompletedTask;
        }

        public Task<string> HandleAsyncStringMethod(int? intArg, string stringArg)
        {
            InvocationCount++;
            var result = (intArg == null ? "null" : intArg.Value.ToString()) + "-" + (stringArg ?? "null");
            return Task.FromResult(result);
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