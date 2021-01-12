// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class IndexingTests
    {
        [FieldAnnotation]
        public object Property;
        public IList ListOfMapsNotGeneric;
        public Dictionary<int, int> ParameterizedMap;
        public List<int> ParameterizedList;
        public List<List<int>> ParameterizedListOfList;
        public IList Property2;

        [FieldAnnotation]
        public IList ListNotGeneric;

        [FieldAnnotation]
        public IDictionary MapNotGeneric;

        public IList ListOfScalarNotGeneric;

        [Fact]
        public void IndexIntoGenericPropertyContainingMap()
        {
            var property = new Dictionary<string, string>();
            property.Add("foo", "bar");
            Property = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("Property");
            Assert.Equal(property.GetType(), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            Assert.Equal(property, expression.GetValue(this, typeof(IDictionary)));
            expression = parser.ParseExpression("Property['foo']");
            Assert.Equal("bar", expression.GetValue(this));
        }

        [Fact]
        public void IndexIntoGenericPropertyContainingMapobject()
        {
            var property = new Dictionary<string, Dictionary<string, string>>();
            var map = new Dictionary<string, string>();
            map.Add("foo", "bar");
            property.Add("property", map);
            var parser = new SpelExpressionParser();
            var context = new StandardEvaluationContext();
            context.AddPropertyAccessor(new MapAccessor());
            context.SetRootObject(property);
            var expression = parser.ParseExpression("property");
            Assert.Equal(typeof(Dictionary<string, string>), expression.GetValueType(context));
            Assert.Equal(map, expression.GetValue(context));
            Assert.Equal(map, expression.GetValue(context, typeof(IDictionary)));
            expression = parser.ParseExpression("property['foo']");
            Assert.Equal("bar", expression.GetValue(context));
        }

        [Fact]
        public void SetGenericPropertyContainingMap()
        {
            var property = new Dictionary<string, string>();
            property.Add("foo", "bar");
            Property = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("Property");
            Assert.Equal(typeof(Dictionary<string, string>), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("Property['foo']");
            Assert.Equal("bar", expression.GetValue(this));
            expression.SetValue(this, "baz");
            Assert.Equal("baz", expression.GetValue(this));
        }

        [Fact]
        public void SetPropertyContainingMap()
        {
            var property = new Dictionary<int, int>();
            property.Add(9, 3);
            ParameterizedMap = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ParameterizedMap");
            Assert.Equal(typeof(Dictionary<int, int>), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("ParameterizedMap['9']");
            Assert.Equal(3, expression.GetValue(this));
            expression.SetValue(this, "37");
            Assert.Equal(37, expression.GetValue(this));
        }

        [Fact]
        public void SetPropertyContainingMapAutoGrow()
        {
            var parser = new SpelExpressionParser(new SpelParserOptions(true, false));
            var expression = parser.ParseExpression("ParameterizedMap");
            Assert.Equal(typeof(Dictionary<int, int>), expression.GetValueType(this));
            Assert.Equal(Property, expression.GetValue(this));
            expression = parser.ParseExpression("ParameterizedMap['9']");
            Assert.Null(expression.GetValue(this));
            expression.SetValue(this, "37");
            Assert.Equal(37, expression.GetValue(this));
        }

        [Fact]
        public void IndexIntoGenericPropertyContainingList()
        {
            var property = new List<string>();
            property.Add("bar");
            Property = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("Property");
            Assert.Equal(typeof(List<string>), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("Property[0]");
            Assert.Equal("bar", expression.GetValue(this));
        }

        [Fact]
        public void SetGenericPropertyContainingList()
        {
            var property = new List<int>();
            property.Add(3);
            Property = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("Property");
            Assert.Equal(typeof(List<int>), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("Property[0]");
            Assert.Equal(3, expression.GetValue(this));
            expression.SetValue(this, "4");
            Assert.Equal(4, expression.GetValue(this));
        }

        [Fact]
        public void SetGenericPropertyContainingListAutogrow()
        {
            var property = new List<int>();
            Property = property;
            var parser = new SpelExpressionParser(new SpelParserOptions(true, true));
            var expression = parser.ParseExpression("Property");
            Assert.Equal(typeof(List<int>), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("Property[0]");
            try
            {
                expression.SetValue(this, "4");
            }
            catch (EvaluationException ex)
            {
                Assert.StartsWith("EL1053E", ex.Message);
            }
        }

        [Fact]
        public void IndexIntoPropertyContainingList()
        {
            var property = new List<int>();
            property.Add(3);
            ParameterizedList = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ParameterizedList");
            Assert.Equal(typeof(List<int>), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("ParameterizedList[0]");
            Assert.Equal(3, expression.GetValue(this));
        }

        [Fact]
        public void IndexIntoPropertyContainingListOfList()
        {
            var property = new List<List<int>>();
            property.Add(new List<int>() { 3 });
            ParameterizedListOfList = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ParameterizedListOfList[0]");
            Assert.Equal(typeof(List<int>), expression.GetValueType(this));
            Assert.Equal(property[0], expression.GetValue(this));
            expression = parser.ParseExpression("ParameterizedListOfList[0][0]");
            Assert.Equal(3, expression.GetValue(this));
        }

        [Fact]
        public void SetPropertyContainingList()
        {
            var property = new List<int>();
            property.Add(3);
            ParameterizedList = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ParameterizedList");
            Assert.Equal(typeof(List<int>), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("ParameterizedList[0]");
            Assert.Equal(3, expression.GetValue(this));
            expression.SetValue(this, "4");
            Assert.Equal(4, expression.GetValue(this));
        }

        [Fact]
        public void IndexIntoGenericPropertyContainingNullList()
        {
            var configuration = new SpelParserOptions(true, true);
            var parser = new SpelExpressionParser(configuration);
            var expression = parser.ParseExpression("Property");
            Assert.Equal(typeof(object), expression.GetValueType(this));
            Assert.Equal(Property, expression.GetValue(this));
            expression = parser.ParseExpression("Property[0]");
            try
            {
                Assert.Equal("bar", expression.GetValue(this));
            }
            catch (EvaluationException ex)
            {
                Assert.StartsWith("EL1027E", ex.Message);
            }
        }

        [Fact]
        public void IndexIntoGenericPropertyContainingGrowingList()
        {
            var property = new ArrayList();
            Property = property;
            var configuration = new SpelParserOptions(true, true);
            var parser = new SpelExpressionParser(configuration);
            var expression = parser.ParseExpression("Property");
            Assert.Equal(typeof(ArrayList), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("Property[0]");
            try
            {
                Assert.Equal("bar", expression.GetValue(this));
            }
            catch (EvaluationException ex)
            {
                Assert.StartsWith("EL1053E", ex.Message);
            }
        }

        [Fact]
        public void IndexIntoGenericPropertyContainingGrowingList2()
        {
            var property2 = new ArrayList();
            Property2 = property2;
            var configuration = new SpelParserOptions(true, true);
            var parser = new SpelExpressionParser(configuration);
            var expression = parser.ParseExpression("Property2");
            Assert.Equal(typeof(ArrayList), expression.GetValueType(this));
            Assert.Equal(property2, expression.GetValue(this));
            expression = parser.ParseExpression("Property2[0]");
            try
            {
                Assert.Equal("bar", expression.GetValue(this));
            }
            catch (EvaluationException ex)
            {
                Assert.StartsWith("EL1053E", ex.Message);
            }
        }

        [Fact]
        public void IndexIntoGenericPropertyContainingArray()
        {
            var property = new string[] { "bar" };
            Property = property;
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("Property");
            Assert.Equal(typeof(string[]), expression.GetValueType(this));
            Assert.Equal(property, expression.GetValue(this));
            expression = parser.ParseExpression("Property[0]");
            Assert.Equal("bar", expression.GetValue(this));
        }

        [Fact]
        public void EmptyList()
        {
            ListOfScalarNotGeneric = new ArrayList();
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ListOfScalarNotGeneric");
            Assert.Equal(typeof(ArrayList), expression.GetValueType(this));
            Assert.Equal(string.Empty, expression.GetValue(this, typeof(string)));
        }

        [Fact]
        public void ResolveCollectionElementType()
        {
            ListNotGeneric = new ArrayList(2);
            ListNotGeneric.Add(5);
            ListNotGeneric.Add(6);
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ListNotGeneric");
            Assert.Equal(typeof(ArrayList), expression.GetValueType(this));
            Assert.Equal("5,6", expression.GetValue(this, typeof(string)));
        }

        [Fact]
        public void ResolveCollectionElementTypeNull()
        {
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ListNotGeneric");
            Assert.Equal(typeof(IList), expression.GetValueType(this));
        }

        [Fact]
        public void ResolveMapKeyValueTypes()
        {
            MapNotGeneric = new Hashtable();
            MapNotGeneric.Add("baseAmount", 3.11);
            MapNotGeneric.Add("bonusAmount", 7.17);
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("MapNotGeneric");
            Assert.Equal(typeof(Hashtable), expression.GetValueType(this));
        }

        [Fact]
        public void TestListOfScalar()
        {
            ListOfScalarNotGeneric = new ArrayList(1);
            ListOfScalarNotGeneric.Add("5");
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ListOfScalarNotGeneric[0]");
            Assert.Equal(5, expression.GetValue(this, typeof(int)));
        }

        [Fact]
        public void TestListsOfMap()
        {
            ListOfMapsNotGeneric = new ArrayList();
            var map = new Hashtable();
            map.Add("fruit", "apple");
            ListOfMapsNotGeneric.Add(map);
            var parser = new SpelExpressionParser();
            var expression = parser.ParseExpression("ListOfMapsNotGeneric[0]['fruit']");
            Assert.Equal("apple", expression.GetValue(this, typeof(string)));
        }

        public class MapAccessor : IPropertyAccessor
        {
            public bool CanRead(IEvaluationContext context, object target, string name)
            {
                return ((IDictionary)target).Contains(name);
            }

            public ITypedValue Read(IEvaluationContext context, object target, string name)
            {
                return new TypedValue(((IDictionary)target)[name]);
            }

            public bool CanWrite(IEvaluationContext context, object target, string name)
            {
                return true;
            }

            public void Write(IEvaluationContext context, object target, string name, object newValue)
            {
                ((IDictionary)target).Add(name, newValue);
            }

            public IList<Type> GetSpecificTargetClasses()
            {
                return new List<Type>() { typeof(IDictionary) };
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class FieldAnnotation : Attribute
        {
        }
    }
}
