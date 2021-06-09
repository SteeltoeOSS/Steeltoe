// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Internal.Spring.TestResources;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class MethodInvocationTests : AbstractExpressionTests
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
            Evaluate("'HELLO'.ToLower()", "hello", typeof(string));
            Evaluate("'   abcba '.Trim()", "abcba", typeof(string));
        }

        [Fact]
        public void TestNonExistentMethods()
        {
            // name is ok but madeup() does not exist
            EvaluateAndCheckError("Name.MadeUp()", SpelMessage.METHOD_NOT_FOUND, 5);
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
            var expr = parser.ParseExpression("ThrowException(#bar)");

            // Normal exit
            var eContext = TestScenarioCreator.GetTestEvaluationContext();
            eContext.SetVariable("bar", 3);
            var o = expr.GetValue(eContext);
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

        /**
         * Check on first usage (when the cachedExecutor in MethodReference is null) that the exception is not wrapped.
         */
        [Fact]
        public void TestMethodThrowingException_SPR6941()
        {
            // Test method on inventor: throwException()
            // On 1 it will throw an IllegalArgumentException
            // On 2 it will throw a RuntimeException
            // On 3 it will exit normally
            // In each case it increments the Inventor field 'counter' when invoked
            var parser = new SpelExpressionParser();
            var expr = parser.ParseExpression("ThrowException(#bar)");

            _context.SetVariable("bar", 2);
            var ex = Assert.Throws<SystemException>(() => expr.GetValue(_context));
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
            var expr = parser.ParseExpression("ThrowException(#bar)");

            _context.SetVariable("bar", 4);
            var ex = Assert.Throws<ExpressionInvocationTargetException>(() => expr.GetValue(_context));
            Assert.Contains("TestException", ex.InnerException.GetType().Name);
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
            var result = expr.GetValue<string>(context);
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
            var methodResolvers = ctx.MethodResolvers;
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
            // Evaluate("aVarargsMethod('a','b','c')", 3, typeof(int));
            // Evaluate("aVarargsMethod('a')", 1, typeof(int));
            Evaluate("AVarargsMethod()", 0, typeof(int));
            Evaluate("AVarargsMethod(1,2,3)", 3, typeof(int)); // all need converting to strings
            Evaluate("AVarargsMethod(1)", 1, typeof(int)); // needs string conversion
            Evaluate("AVarargsMethod(1,'a',3.0d)", 3, typeof(int)); // first and last need conversion

            // Evaluate("aVarargsMethod(new String[]{'a','b','c'})", 3, typeof(int));
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

            // Evaluate("aVarargsMethod2(8,new String[]{'a','b','c'})", 11, typeof(int));
        }

        [Fact]
        public void TestInvocationOnNullContextObject()
        {
            EvaluateAndCheckError("null.ToString()", SpelMessage.METHOD_CALL_ON_NULL_OBJECT_NOT_ALLOWED);
        }

        [Fact]
        public void TestMethodOfClass()
        {
            var expression = _parser.ParseExpression("FullName");
            var value = expression.GetValue(new StandardEvaluationContext(typeof(string)));
            Assert.Equal("System.String", value);
        }

        [Fact]
        public void InvokeMethodWithoutConversion()
        {
            var bytes = new byte[100];
            var context = new StandardEvaluationContext(bytes)
            {
                ServiceResolver = new TestServiceResolver()
            };
            var expression = _parser.ParseExpression("@service.HandleBytes(#root)");
            var outBytes = expression.GetValue<byte[]>(context);
            Assert.Same(bytes, outBytes);
        }

        public class TestServiceResolver : IServiceResolver
        {
            public BytesService Service => new ();

            public object Resolve(IEvaluationContext context, string beanName)
            {
                return "service".Equals(beanName) ? Service : null;
            }
        }

        // Simple filter
        public class LocalFilter : IMethodFilter
        {
            public bool RemoveIfNotAnnotated = false;
            public bool FilterCalled = false;

            public List<MethodInfo> Filter(List<MethodInfo> methods)
            {
                FilterCalled = true;
                var forRemoval = new List<MethodInfo>();
                foreach (var method in methods)
                {
                    if (RemoveIfNotAnnotated && !IsAnnotated(method))
                    {
                        forRemoval.Add(method);
                    }
                }

                foreach (var method in forRemoval)
                {
                    methods.Remove(method);
                }

                return methods;
            }

            private bool IsAnnotated(MethodInfo method)
            {
                var anno = method.GetCustomAttribute<AnnoAttribute>();
                return anno != null;
            }
        }

        public class AnnoAttribute : Attribute
        {
        }

        public class TestObject
        {
            public int DoIt(int i)
            {
                return i;
            }

            [Anno]
            public string DoIt(double d)
            {
                return "double " + d.ToString("F1");
            }
        }

        public class DummyMethodResolver : IMethodResolver
        {
            public IMethodExecutor Resolve(IEvaluationContext context, object targetObject, string name, List<Type> argumentTypes)
            {
                throw new InvalidOperationException();
            }
        }

        public class BytesService
        {
            public byte[] HandleBytes(byte[] bytes)
            {
                return bytes;
            }
        }
    }
}
