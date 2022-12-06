// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Test.Spring.TestResources;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Spring;

public class ConstructorInvocationTests : AbstractExpressionTests
{
    [Fact]
    public void TestTypeConstructors()
    {
        Evaluate("new String('hello world')", "hello world", typeof(string));
    }

    [Fact]
    public void TestNonExistentType()
    {
        EvaluateAndCheckError("new FooBar()", SpelMessage.ConstructorInvocationProblem);
    }

    [Fact]
    public void TestConstructorThrowingException_SPR6760()
    {
        // Test ctor on inventor:
        // On 1 it will throw an IllegalArgumentException
        // On 2 it will throw a RuntimeException
        // On 3 it will exit normally
        // In each case it increments the Tester field 'counter' when invoked
        var parser = new SpelExpressionParser();
        IExpression expr = parser.ParseExpression($"new {typeof(ConstructorInvocationTests).FullName}$Tester(#bar).I");

        // Normal exit
        StandardEvaluationContext eContext = TestScenarioCreator.GetTestEvaluationContext();
        eContext.SetRootObject(new Tester());
        eContext.SetVariable("bar", 3);
        object o = expr.GetValue(eContext);
        Assert.Equal(3, o);
        Assert.Equal(1, parser.ParseExpression("Counter").GetValue(eContext));

        // Now the expression has cached that throwException(int) is the right thing to
        // call. Let's change 'bar' to be a PlaceOfBirth which indicates the cached
        // reference is out of date.
        eContext.SetVariable("bar", new PlaceOfBirth("London"));
        o = expr.GetValue(eContext);
        Assert.Equal(0, o);

        // That confirms the logic to mark the cached reference stale and retry is working
        // Now let's cause the method to exit via exception and ensure it doesn't cause
        // a retry.
        // First, switch back to throwException(int)
        eContext.SetVariable("bar", 3);
        o = expr.GetValue(eContext);
        Assert.Equal(3, o);
        Assert.Equal(2, parser.ParseExpression("Counter").GetValue(eContext));

        // 4 will make it throw a checked exception - this will be wrapped by spel on the
        // way out
        eContext.SetVariable("bar", 4);
        var ex = Assert.Throws<SpelEvaluationException>(() => expr.GetValue(eContext));
        Assert.Contains("Tester", ex.Message, StringComparison.Ordinal);

        // A problem occurred whilst attempting to construct an object of type
        // 'org.springframework.expression.spel.ConstructorInvocationTests$Tester'
        // using arguments '(int)'
        // If counter is 4 then the method got called twice!
        Assert.Equal(3, parser.ParseExpression("Counter").GetValue(eContext));

        // 2 will make it throw a SystemException - SpEL will let this through
        eContext.SetVariable("bar", 2);
        var ex2 = Assert.Throws<SystemException>(() => expr.GetValue(eContext));
        Assert.IsNotType<SpelEvaluationException>(ex2);

        // A problem occurred whilst attempting to construct an object of type
        // 'org.springframework.expression.spel.ConstructorInvocationTests$Tester'
        // using arguments '(int)'
        // If counter is 5 then the method got called twice!
        Assert.Equal(4, parser.ParseExpression("Counter").GetValue(eContext));
    }

    [Fact]
    public void TestAddingConstructorResolvers()
    {
        var ctx = new StandardEvaluationContext();

        // reflective constructor accessor is the only one by default
        List<IConstructorResolver> constructorResolvers = ctx.ConstructorResolvers;
        Assert.Single(constructorResolvers);

        var dummy = new DummyConstructorResolver();
        ctx.AddConstructorResolver(dummy);
        Assert.Equal(2, ctx.ConstructorResolvers.Count);

        var copy = new List<IConstructorResolver>(ctx.ConstructorResolvers);
        Assert.True(ctx.RemoveConstructorResolver(dummy));
        Assert.False(ctx.RemoveConstructorResolver(dummy));
        Assert.Single(ctx.ConstructorResolvers);

        ctx.ConstructorResolvers = copy;
        Assert.Equal(2, ctx.ConstructorResolvers.Count);
    }

    [Fact]
    public void TestVarargsInvocation01()
    {
        // Calling 'Fruit(String... strings)'
        Evaluate($"new {typeof(Fruit).FullName}('a','b','c').StringsCount", 3, typeof(int));
        Evaluate($"new {typeof(Fruit).FullName}('a').StringsCount", 1, typeof(int));
        Evaluate($"new {typeof(Fruit).FullName}().StringsCount", 0, typeof(int));

        // all need converting to strings
        Evaluate($"new {typeof(Fruit).FullName}(1,2,3).StringsCount", 3, typeof(int));

        // needs string conversion
        Evaluate($"new {typeof(Fruit).FullName}(1).StringsCount", 1, typeof(int));

        // first and last need conversion
        Evaluate($"new {typeof(Fruit).FullName}(1,'a',3.0d).StringsCount", 3, typeof(int));
    }

    [Fact]
    public void TestVarargsInvocation02()
    {
        // Calling 'Fruit(int i, String... strings)' - returns int+length_of_strings
        Evaluate($"new {typeof(Fruit).FullName}(5,'a','b','c').StringsCount", 8, typeof(int));
        Evaluate($"new {typeof(Fruit).FullName}(2,'a').StringsCount", 3, typeof(int));
        Evaluate($"new {typeof(Fruit).FullName}(4).StringsCount", 4, typeof(int));
        Evaluate($"new {typeof(Fruit).FullName}(8,2,3).StringsCount", 10, typeof(int));
        Evaluate($"new {typeof(Fruit).FullName}(9).StringsCount", 9, typeof(int));
        Evaluate($"new {typeof(Fruit).FullName}(2,'a',3.0d).StringsCount", 4, typeof(int));
        Evaluate($"new {typeof(Fruit).FullName}(8,StringArrayOfThreeItems).StringsCount", 11, typeof(int));
    }

    [Fact]
    public void TestWidening01()
    {
        // widening of int 3 to double 3 is OK
        Evaluate($"new {typeof(WidenDouble).FullName}(3).D", 3.0d, typeof(double));

        // widening of int 3 to long 3 is OK
        Evaluate($"new {typeof(WidenLong).FullName}(3).L", 3L, typeof(long));
    }

    [Fact]
    public void TestArgumentConversion01()
    {
        // Closest ctor will be new Company(String) and converter supports Double>String
        Evaluate($"new {typeof(Company).FullName}(1.1d).Address", "1.1", typeof(string));
    }

    public class DummyConstructorResolver : IConstructorResolver
    {
        public IConstructorExecutor Resolve(IEvaluationContext context, string typeName, List<Type> argumentTypes)
        {
            throw new InvalidOperationException("Auto-generated method stub");
        }
    }

    public class TestException : Exception
    {
    }

    public class Tester
    {
        public static int Counter { get; set; }
        public int I { get; }

        public Tester()
        {
        }

        public Tester(int i)
        {
            Counter++;

            if (i == 1)
            {
                throw new ArgumentException("ArgumentException for 1", nameof(i));
            }

            if (i == 2)
            {
                throw new SystemException("SystemException for 2");
            }

            if (i == 4)
            {
                throw new TestException();
            }

            I = i;
        }

        public Tester(PlaceOfBirth pob)
        {
        }
    }
}
