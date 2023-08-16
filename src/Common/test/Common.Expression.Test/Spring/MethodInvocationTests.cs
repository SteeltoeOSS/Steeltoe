// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Test.Spring.TestResources;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Spring;

public sealed class MethodInvocationTests : AbstractExpressionTests
{
    [Fact]
    public void TestSimpleAccess01()
    {
        Evaluate("PlaceOfBirth.City", "SmilJan", typeof(string));
    }

    [Fact]
    public void TestStringClass()
    {
        Evaluate("new String('hello')[2]", "l", typeof(string));
        Evaluate("new String('hello')[2].Equals('l'[0])", true, typeof(bool));
        Evaluate("'HELLO'.ToLowerInvariant()", "hello", typeof(string));
        Evaluate("'   abcba '.Trim()", "abcba", typeof(string));
    }

    [Fact]
    public void TestNonExistentMethods()
    {
        // name is ok but madeup() does not exist
        EvaluateAndCheckError("Name.MadeUp()", SpelMessage.MethodNotFound, 5);
    }

    [Fact]
    public void TestWidening01()
    {
        // widening of int 8 to double 8 is OK
        Evaluate("PrintDouble(8)", "8.00", typeof(string));
    }

    [Fact]
    public void TestArgumentConversion01()
    {
        // Rely on Double>String conversion for calling startsWith()
        Evaluate("new String('hello 2.0 to you').StartsWith(7.0d)", false, typeof(bool));
        Evaluate("new String('7.0 foobar').StartsWith(7.0d)", true, typeof(bool));
    }

    [Fact]
    public void TestMethodThrowingException_SPR6760()
    {
        // Test method on inventor: throwException()
        // On 1 it will throw an IllegalArgumentException
        // On 2 it will throw a RuntimeException
        // On 3 it will exit normally
        // In each case it increments the Inventor field 'counter' when invoked
        var parser = new SpelExpressionParser();
        IExpression expr = parser.ParseExpression("ThrowException(#bar)");

        // Normal exit
        StandardEvaluationContext eContext = TestScenarioCreator.GetTestEvaluationContext();
        eContext.SetVariable("bar", 3);
        object o = expr.GetValue(eContext);
        Assert.Equal(3, o);
        Assert.Equal(1, parser.ParseExpression("Counter").GetValue(eContext));

        // Now the expression has cached that throwException(int) is the right thing to call
        // Let's change 'bar' to be a PlaceOfBirth which indicates the cached reference is
        // out of date.
        eContext.SetVariable("bar", new PlaceOfBirth("London"));
        o = expr.GetValue(eContext);
        Assert.Equal("London", o);

        // That confirms the logic to mark the cached reference stale and retry is working
        // Now let's cause the method to exit via exception and ensure it doesn't cause a retry.
        // First, switch back to throwException(int)
        eContext.SetVariable("bar", 3);
        o = expr.GetValue(eContext);
        Assert.Equal(3, o);
        Assert.Equal(2, parser.ParseExpression("Counter").GetValue(eContext));

        // Now cause it to throw an exception:
        eContext.SetVariable("bar", 1);
        var ex = Assert.Throws<ArgumentException>(() => expr.GetValue(eContext));
        Assert.IsNotType<SpelEvaluationException>(ex);

        // If counter is 4 then the method got called twice!
        Assert.Equal(3, parser.ParseExpression("Counter").GetValue(eContext));

        eContext.SetVariable("bar", 4);
        Assert.Throws<ExpressionInvocationTargetException>(() => expr.GetValue(eContext));

        // If counter is 5 then the method got called twice!
        Assert.Equal(4, parser.ParseExpression("Counter").GetValue(eContext));
    }

    // Check on first usage (when the cachedExecutor in MethodReference is null) that the exception is not wrapped.
    [Fact]
    public void TestMethodThrowingException_SPR6941()
    {
        // Test method on inventor: throwException()
        // On 1 it will throw an IllegalArgumentException
        // On 2 it will throw a RuntimeException
        // On 3 it will exit normally
        // In each case it increments the Inventor field 'counter' when invoked
        var parser = new SpelExpressionParser();
        IExpression expr = parser.ParseExpression("ThrowException(#bar)");

        Context.SetVariable("bar", 2);
        var ex = Assert.Throws<SystemException>(() => expr.GetValue(Context));
        Assert.IsNotType<SpelEvaluationException>(ex);
    }

    [Fact]
    public void TestMethodThrowingException_SPR6941_2()
    {
        // Test method on inventor: throwException()
        // On 1 it will throw an IllegalArgumentException
        // On 2 it will throw a RuntimeException
        // On 3 it will exit normally
        // In each case it increments the Inventor field 'counter' when invoked
        var parser = new SpelExpressionParser();
        IExpression expr = parser.ParseExpression("ThrowException(#bar)");

        Context.SetVariable("bar", 4);
        var ex = Assert.Throws<ExpressionInvocationTargetException>(() => expr.GetValue(Context));
        Assert.Contains("TestException", ex.InnerException.GetType().Name, StringComparison.Ordinal);
    }

    [Fact]
    public void TestMethodFiltering_SPR6764()
    {
        var parser = new SpelExpressionParser();
        var context = new StandardEvaluationContext();
        context.SetRootObject(new TestObject());
        var filter = new LocalFilter();
        context.RegisterMethodFilter(typeof(TestObject), filter);

        // Filter will be called but not do anything, so first doit() will be invoked
        var expr = (SpelExpression)parser.ParseExpression("DoIt(1)");
        string result = expr.GetValue<string>(context);
        Assert.Equal("1", result);
        Assert.True(filter.FilterCalled);

        // Filter will now remove non @Anno annotated methods
        filter.RemoveIfNotAnnotated = true;
        filter.FilterCalled = false;
        expr = (SpelExpression)parser.ParseExpression("DoIt(1)");
        result = expr.GetValue<string>(context);
        Assert.Equal("double 1.0", result);
        Assert.True(filter.FilterCalled);

        // check not called for other types
        filter.FilterCalled = false;
        context.SetRootObject("abc".Clone());
        expr = (SpelExpression)parser.ParseExpression("[0]");
        result = expr.GetValue<string>(context);
        Assert.Equal("a", result);
        Assert.False(filter.FilterCalled);

        // check de-registration works
        filter.FilterCalled = false;
        context.RegisterMethodFilter(typeof(TestObject), null); // clear filter
        context.SetRootObject(new TestObject());
        expr = (SpelExpression)parser.ParseExpression("DoIt(1)");
        result = expr.GetValue<string>(context);
        Assert.Equal("1", result);
        Assert.False(filter.FilterCalled);
    }

    [Fact]
    public void TestAddingMethodResolvers()
    {
        var ctx = new StandardEvaluationContext();

        // reflective method accessor is the only one by default
        List<IMethodResolver> methodResolvers = ctx.MethodResolvers;
        Assert.Single(methodResolvers);

        var dummy = new DummyMethodResolver();
        ctx.AddMethodResolver(dummy);
        Assert.Equal(2, ctx.MethodResolvers.Count);

        var copy = new List<IMethodResolver>(ctx.MethodResolvers);
        Assert.True(ctx.RemoveMethodResolver(dummy));
        Assert.False(ctx.RemoveMethodResolver(dummy));
        Assert.Single(ctx.MethodResolvers);

        ctx.MethodResolvers = copy;
        Assert.Equal(2, ctx.MethodResolvers.Count);
    }

    [Fact]
    public void TestVarargsInvocation01()
    {
        // Calling 'public int aVarargsMethod(String... strings)'
        Evaluate("AVarargsMethod()", 0, typeof(int));
        Evaluate("AVarargsMethod(1,2,3)", 3, typeof(int)); // all need converting to strings
        Evaluate("AVarargsMethod(1)", 1, typeof(int)); // needs string conversion
        Evaluate("AVarargsMethod(1,'a',3.0d)", 3, typeof(int)); // first and last need conversion
    }

    [Fact]
    public void TestVarargsInvocation02()
    {
        // Calling 'public int aVarargsMethod2(int i, String... strings)' - returns int+length_of_strings
        Evaluate("AVarargsMethod2(5,'a','b','c')", 8, typeof(int));
        Evaluate("AVarargsMethod2(2,'a')", 3, typeof(int));
        Evaluate("AVarargsMethod2(4)", 4, typeof(int));
        Evaluate("AVarargsMethod2(8,2,3)", 10, typeof(int));
        Evaluate("AVarargsMethod2(9)", 9, typeof(int));
        Evaluate("AVarargsMethod2(2,'a',3.0d)", 4, typeof(int));
    }

    [Fact]
    public void TestInvocationOnNullContextObject()
    {
        EvaluateAndCheckError("null.ToString()", SpelMessage.MethodCallOnNullObjectNotAllowed);
    }

    [Fact]
    public void TestMethodOfClass()
    {
        IExpression expression = Parser.ParseExpression("FullName");
        object value = expression.GetValue(new StandardEvaluationContext(typeof(string)));
        Assert.Equal("System.String", value);
    }

    [Fact]
    public void InvokeMethodWithoutConversion()
    {
        byte[] bytes = new byte[100];

        var context = new StandardEvaluationContext(bytes)
        {
            ServiceResolver = new TestServiceResolver()
        };

        IExpression expression = Parser.ParseExpression("@service.HandleBytes(#root)");
        byte[] outBytes = expression.GetValue<byte[]>(context);
        Assert.Same(bytes, outBytes);
    }

    public sealed class TestServiceResolver : IServiceResolver
    {
        public BytesService Service => new();

        public object Resolve(IEvaluationContext context, string serviceName)
        {
            return serviceName == "service" ? Service : null;
        }
    }

    // Simple filter
    public sealed class LocalFilter : IMethodFilter
    {
        public bool RemoveIfNotAnnotated { get; set; }
        public bool FilterCalled { get; set; }

        public List<MethodInfo> Filter(List<MethodInfo> methods)
        {
            FilterCalled = true;
            var forRemoval = new List<MethodInfo>();

            foreach (MethodInfo method in methods)
            {
                if (RemoveIfNotAnnotated && !IsAnnotated(method))
                {
                    forRemoval.Add(method);
                }
            }

            foreach (MethodInfo method in forRemoval)
            {
                methods.Remove(method);
            }

            return methods;
        }

        private bool IsAnnotated(MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<AnnotationAttribute>();
            return attribute != null;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AnnotationAttribute : Attribute
    {
    }

    public sealed class TestObject
    {
        public int DoIt(int i)
        {
            return i;
        }

        [Annotation]
        public string DoIt(double d)
        {
            return FormattableString.Invariant($"double {d:F1}");
        }
    }

    public sealed class DummyMethodResolver : IMethodResolver
    {
        public IMethodExecutor Resolve(IEvaluationContext context, object targetObject, string name, List<Type> argumentTypes)
        {
            throw new InvalidOperationException();
        }
    }

    public sealed class BytesService
    {
        public byte[] HandleBytes(byte[] bytes)
        {
            return bytes;
        }
    }
}
