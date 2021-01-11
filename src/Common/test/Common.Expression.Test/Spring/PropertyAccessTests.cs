// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Internal.Spring.TestResources;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class PropertyAccessTests : AbstractExpressionTests
    {
        [Fact]
        public void TestSimpleAccess01()
        {
            Evaluate("Name", "Nikola Tesla", typeof(string));
        }

        [Fact]
        public void TestSimpleAccess02()
        {
            Evaluate("PlaceOfBirth.City", "SmilJan", typeof(string));
        }

        [Fact]
        public void TestSimpleAccess03()
        {
            Evaluate("StringArrayOfThreeItems.Length", "3", typeof(int));
        }

        [Fact]
        public void TestNonExistentPropertiesAndMethods()
        {
            // madeup does not exist as a property
            EvaluateAndCheckError("madeup", SpelMessage.PROPERTY_OR_FIELD_NOT_READABLE, 0);

            // name is ok but foobar does not exist:
            EvaluateAndCheckError("Name.foobar", SpelMessage.PROPERTY_OR_FIELD_NOT_READABLE, 5);
        }

        /**
         * The standard reflection resolver cannot find properties on null objects but some
         * supplied resolver might be able to - so null shouldn't crash the reflection resolver.
         */
        [Fact]
        public void TestAccessingOnNullObject()
        {
            var expr = (SpelExpression)parser.ParseExpression("madeup");
            var context = new StandardEvaluationContext(null);
            var ex = Assert.Throws<SpelEvaluationException>(() => expr.GetValue(context));
            Assert.Equal(SpelMessage.PROPERTY_OR_FIELD_NOT_READABLE_ON_NULL, ex.MessageCode);
            Assert.False(expr.IsWritable(context));
            ex = Assert.Throws<SpelEvaluationException>(() => expr.SetValue(context, "abc"));
            Assert.Equal(SpelMessage.PROPERTY_OR_FIELD_NOT_WRITABLE_ON_NULL, ex.MessageCode);
        }

        // Adding a new property accessor just for a particular type
        [Fact]
        public void TestAddingSpecificPropertyAccessor()
        {
            var parser = new SpelExpressionParser();
            var ctx = new StandardEvaluationContext();

            // Even though this property accessor is added after the reflection one, it specifically
            // names the String class as the type it is interested in so is chosen in preference to
            // any 'default' ones
            ctx.AddPropertyAccessor(new StringyPropertyAccessor());
            var expr = parser.ParseRaw("new String('hello').flibbles");
            var i = expr.GetValue(ctx, typeof(int));
            Assert.Equal(7, (int)i);

            // The reflection one will be used for other properties...
            expr = parser.ParseRaw("new String('hello').Length");
            var o = expr.GetValue(ctx);
            Assert.NotNull(o);

            var flibbleexpr = parser.ParseRaw("new String('hello').flibbles");
            flibbleexpr.SetValue(ctx, 99);
            i = flibbleexpr.GetValue(ctx, typeof(int));
            Assert.Equal(99, (int)i);

            // Cannot set it to a string value
            Assert.Throws<SpelEvaluationException>(() => flibbleexpr.SetValue(ctx, "not allowed"));

            // message will be: EL1063E:(pos 20): A problem occurred whilst attempting to set the property
            // 'flibbles': 'Cannot set flibbles to an object of type 'class java.lang.String''
            // System.out.println(e.getMessage());
        }

        [Fact]
        public void TestAddingRemovingAccessors()
        {
            var ctx = new StandardEvaluationContext();

            // reflective property accessor is the only one by default
            var propertyAccessors = ctx.PropertyAccessors;
            Assert.Single(propertyAccessors);

            var spa = new StringyPropertyAccessor();
            ctx.AddPropertyAccessor(spa);
            Assert.Equal(2, ctx.PropertyAccessors.Count);

            var copy = new List<IPropertyAccessor>(ctx.PropertyAccessors);
            Assert.True(ctx.RemovePropertyAccessor(spa));
            Assert.False(ctx.RemovePropertyAccessor(spa));
            Assert.Single(ctx.PropertyAccessors);

            ctx.PropertyAccessors = copy;
            Assert.Equal(2, ctx.PropertyAccessors.Count);
        }

        [Fact]
        public void TestAccessingPropertyOfClass()
        {
            var expression = parser.ParseExpression("FullName");
            var value = expression.GetValue(new StandardEvaluationContext(typeof(string)));
            Assert.Equal("System.String", value);
        }

        [Fact]
        public void ShouldAlwaysUsePropertyAccessorFromEvaluationContext()
        {
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("name");

            var context = new StandardEvaluationContext();
            context.AddPropertyAccessor(new ConfigurablePropertyAccessor(new Dictionary<string, object>() { { "name", "Ollie" } }));
            Assert.Equal("Ollie", expression.GetValue(context));

            context = new StandardEvaluationContext();
            context.AddPropertyAccessor(new ConfigurablePropertyAccessor(new Dictionary<string, object>() { { "name", "Jens" } }));
            Assert.Equal("Jens", expression.GetValue(context));
        }

        [Fact]
        public void StandardGetClassAccess()
        {
            Assert.Equal(typeof(string).FullName, parser.ParseExpression("'a'.GetType().FullName").GetValue());
        }

        [Fact]
        public void NoGetClassAccess()
        {
            var context = SimpleEvaluationContext.ForReadOnlyDataBinding().Build();
            Assert.Throws<SpelEvaluationException>(() => parser.ParseExpression("'a'.GetType().Name").GetValue(context));
        }

        [Fact]
        public void PropertyReadOnly()
        {
            var context = SimpleEvaluationContext.ForReadOnlyDataBinding().Build();

            var expr = parser.ParseExpression("Name");
            var target = new Person("p1");
            Assert.Equal("p1", expr.GetValue(context, target));
            target.Name = "p2";
            Assert.Equal("p2", expr.GetValue(context, target));

            Assert.Throws<SpelEvaluationException>(() => parser.ParseExpression("Name='p3'").GetValue(context, target));
        }

        [Fact]
        public void PropertyReadWrite()
        {
            var context = SimpleEvaluationContext.ForReadWriteDataBinding().Build();

            var expr = parser.ParseExpression("Name");
            var target = new Person("p1");
            Assert.Equal("p1", expr.GetValue(context, target));
            target.Name = "p2";
            Assert.Equal("p2", expr.GetValue(context, target));

            parser.ParseExpression("Name='p3'").GetValue(context, target);
            Assert.Equal("p3", target.Name);
            Assert.Equal("p3", expr.GetValue(context, target));

            expr.SetValue(context, target, "p4");
            Assert.Equal("p4", target.Name);
            Assert.Equal("p4", expr.GetValue(context, target));
        }

        [Fact]
        public void PropertyReadWriteWithRootObject()
        {
            var target = new Person("p1");
            var context = SimpleEvaluationContext.ForReadWriteDataBinding().WithRootObject(target).Build();
            Assert.Same(target, context.RootObject.Value);

            var expr = parser.ParseExpression("Name");
            Assert.Equal("p1", expr.GetValue(context, target));
            target.Name = "p2";
            Assert.Equal("p2", expr.GetValue(context, target));

            parser.ParseExpression("Name='p3'").GetValue(context, target);
            Assert.Equal("p3", target.Name);
            Assert.Equal("p3", expr.GetValue(context, target));

            expr.SetValue(context, target, "p4");
            Assert.Equal("p4", target.Name);
            Assert.Equal("p4", expr.GetValue(context, target));
        }

        [Fact]
        public void PropertyAccessWithoutMethodResolver()
        {
            var context = SimpleEvaluationContext.ForReadOnlyDataBinding().Build();
            var target = new Person("p1");
            Assert.Throws<SpelEvaluationException>(() => parser.ParseExpression("Name.Substring(1)").GetValue(context, target));
        }

        [Fact]
        public void PropertyAccessWithInstanceMethodResolver()
        {
            var context = SimpleEvaluationContext.ForReadOnlyDataBinding().WithInstanceMethods().Build();
            var target = new Person("p1");
            Assert.Equal("1", parser.ParseExpression("Name.Substring(1)").GetValue(context, target));
        }

        [Fact]
        public void PropertyAccessWithInstanceMethodResolverAndTypedRootObject()
        {
            var target = new Person("p1");
            var context = SimpleEvaluationContext.ForReadOnlyDataBinding().
                    WithInstanceMethods().WithTypedRootObject(target, typeof(object)).Build();

            Assert.Equal("1", parser.ParseExpression("Name.Substring(1)").GetValue(context, target));
            Assert.Same(target, context.RootObject.Value);
            Assert.Equal(typeof(object), context.RootObject.TypeDescriptor);
        }

        [Fact]
        public void PropertyAccessWithArrayIndexOutOfBounds()
        {
            var context = SimpleEvaluationContext.ForReadOnlyDataBinding().Build();
            var expression = parser.ParseExpression("StringArrayOfThreeItems[3]");
            var ex = Assert.Throws<SpelEvaluationException>(() => expression.GetValue(context, new Inventor()));
            Assert.Equal(SpelMessage.ARRAY_INDEX_OUT_OF_BOUNDS, ex.MessageCode);
        }

        private class StringyPropertyAccessor : IPropertyAccessor
        {
            private int flibbles = 7;

            public IList<Type> GetSpecificTargetClasses()
            {
                return new Type[] { typeof(string) };
            }

            public bool CanRead(IEvaluationContext context, object target, string name)
            {
                if (!(target is string))
                {
                    throw new SystemException("Assertion Failed! target should be string");
                }

                return name.Equals("flibbles");
            }

            public bool CanWrite(IEvaluationContext context, object target, string name)
            {
                if (!(target is string))
                {
                    throw new SystemException("Assertion Failed! target should be string");
                }

                return name.Equals("flibbles");
            }

            public ITypedValue Read(IEvaluationContext context, object target, string name)
            {
                if (!name.Equals("flibbles"))
                {
                    throw new SystemException("Assertion Failed! name should be flibbles");
                }

                return new TypedValue(flibbles);
            }

            public void Write(IEvaluationContext context, object target, string name, object newValue)
            {
                if (!name.Equals("flibbles"))
                {
                    throw new SystemException("Assertion Failed! name should be flibbles");
                }

                try
                {
                    flibbles = (int)context.TypeConverter.ConvertValue(newValue, newValue?.GetType(), typeof(int));
                }
                catch (EvaluationException)
                {
                    throw new AccessException("Cannot set flibbles to an object of type '" + newValue?.GetType() + "'");
                }
            }
        }

        private class ConfigurablePropertyAccessor : IPropertyAccessor
        {
            private readonly Dictionary<string, object> values;

            public ConfigurablePropertyAccessor(Dictionary<string, object> values)
            {
                this.values = values;
            }

            public IList<Type> GetSpecificTargetClasses()
            {
                return null;
            }

            public bool CanRead(IEvaluationContext context, object target, string name)
            {
                return true;
            }

            public ITypedValue Read(IEvaluationContext context, object target, string name)
            {
                return new TypedValue(this.values[name]);
            }

            public bool CanWrite(IEvaluationContext context, object target, string name)
            {
                return false;
            }

            public void Write(IEvaluationContext context, object target, string name, object newValue)
            {
            }
        }
    }
}
