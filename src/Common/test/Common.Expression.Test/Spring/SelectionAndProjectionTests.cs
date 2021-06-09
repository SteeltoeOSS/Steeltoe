// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class SelectionAndProjectionTests
    {
        [Fact]
        public void SelectionWithList()
        {
            var expression = new SpelExpressionParser().ParseRaw("Integers.?[#this<5]");
            var context = new StandardEvaluationContext(new ListTestBean());
            var value = expression.GetValue(context);
            var condition = value is List<object>;
            Assert.True(condition);
            var list = (List<object>)value;
            Assert.Equal(5, list.Count);
            Assert.Equal(0, list[0]);
            Assert.Equal(1, list[1]);
            Assert.Equal(2, list[2]);
            Assert.Equal(3, list[3]);
            Assert.Equal(4, list[4]);
        }

        [Fact]
        public void SelectFirstItemInList()
        {
            var expression = new SpelExpressionParser().ParseRaw("Integers.^[#this<5]");
            var context = new StandardEvaluationContext(new ListTestBean());
            var value = expression.GetValue(context);
            var condition = value is int;
            Assert.True(condition);
            Assert.Equal(0, value);
        }

        [Fact]
        public void SelectLastItemInList()
        {
            var expression = new SpelExpressionParser().ParseRaw("Integers.$[#this<5]");
            var context = new StandardEvaluationContext(new ListTestBean());
            var value = expression.GetValue(context);
            var condition = value is int;
            Assert.True(condition);
            Assert.Equal(4, value);
        }

        [Fact]
        public void SelectionWithSet()
        {
            var expression = new SpelExpressionParser().ParseRaw("Integers.?[#this<5]");
            var context = new StandardEvaluationContext(new SetTestBean());
            var value = expression.GetValue(context);
            var condition = value is List<object>;
            Assert.True(condition);
            var list = (List<object>)value;
            Assert.Equal(5, list.Count);
            Assert.Equal(0, list[0]);
            Assert.Equal(1, list[1]);
            Assert.Equal(2, list[2]);
            Assert.Equal(3, list[3]);
            Assert.Equal(4, list[4]);
        }

        [Fact]
        public void SelectFirstItemInSet()
        {
            var expression = new SpelExpressionParser().ParseRaw("Integers.^[#this<5]");
            var context = new StandardEvaluationContext(new SetTestBean());
            var value = expression.GetValue(context);
            var condition = value is int;
            Assert.True(condition);
            Assert.Equal(0, value);
        }

        [Fact]
        public void SelectLastItemInSet()
        {
            var expression = new SpelExpressionParser().ParseRaw("Integers.$[#this<5]");
            var context = new StandardEvaluationContext(new SetTestBean());
            var value = expression.GetValue(context);
            var condition = value is int;
            Assert.True(condition);
            Assert.Equal(4, value);
        }

        [Fact]
        public void SelectionWithIEnumerable()
        {
            var expression = new SpelExpressionParser().ParseRaw("Integers.?[#this<5]");
            var context = new StandardEvaluationContext(new IEnumerableTestBean());
            var value = expression.GetValue(context);
            var condition = value is List<object>;
            Assert.True(condition);
            var list = (List<object>)value;
            Assert.Equal(5, list.Count);
            Assert.Equal(0, list[0]);
            Assert.Equal(1, list[1]);
            Assert.Equal(2, list[2]);
            Assert.Equal(3, list[3]);
            Assert.Equal(4, list[4]);
        }

        [Fact]
        public void SelectionWithArray()
        {
            var expression = new SpelExpressionParser().ParseRaw("Ints.?[#this<5]");
            var context = new StandardEvaluationContext(new ArrayTestBean());
            var value = expression.GetValue(context);
            Assert.True(value.GetType().IsArray);
            var typedValue = new TypedValue(value);
            Assert.Equal(typeof(int), typedValue.TypeDescriptor.GetElementType());
            var array = (int[])value;
            Assert.Equal(5, array.Length);
            Assert.Equal(0, array[0]);
            Assert.Equal(1, array[1]);
            Assert.Equal(2, array[2]);
            Assert.Equal(3, array[3]);
            Assert.Equal(4, array[4]);
        }

        [Fact]
        public void SelectFirstItemInArray()
        {
            var expression = new SpelExpressionParser().ParseRaw("Ints.^[#this<5]");
            var context = new StandardEvaluationContext(new ArrayTestBean());
            var value = expression.GetValue(context);
            var condition = value is int;
            Assert.True(condition);
            Assert.Equal(0, value);
        }

        [Fact]
        public void SelectLastItemInArray()
        {
            var expression = new SpelExpressionParser().ParseRaw("Ints.$[#this<5]");
            var context = new StandardEvaluationContext(new ArrayTestBean());
            var value = expression.GetValue(context);
            var condition = value is int;
            Assert.True(condition);
            Assert.Equal(4, value);
        }

        [Fact]
        public void SelectionWithPrimitiveArray()
        {
            var expression = new SpelExpressionParser().ParseRaw("Ints.?[#this<5]");
            var context = new StandardEvaluationContext(new ArrayTestBean());
            var value = expression.GetValue(context);
            Assert.True(value.GetType().IsArray);
            var typedValue = new TypedValue(value);
            Assert.Equal(typeof(int), typedValue.TypeDescriptor.GetElementType());
            var array = (int[])value;
            Assert.Equal(5, array.Length);
            Assert.Equal(0, array[0]);
            Assert.Equal(1, array[1]);
            Assert.Equal(2, array[2]);
            Assert.Equal(3, array[3]);
            Assert.Equal(4, array[4]);
        }

        [Fact]
        public void SelectFirstItemInPrimitiveArray()
        {
            var expression = new SpelExpressionParser().ParseRaw("Ints.^[#this<5]");
            var context = new StandardEvaluationContext(new ArrayTestBean());
            var value = expression.GetValue(context);
            var condition = value is int;
            Assert.True(condition);
            Assert.Equal(0, value);
        }

        [Fact]
        public void SelectLastItemInPrimitiveArray()
        {
            var expression = new SpelExpressionParser().ParseRaw("Ints.$[#this<5]");
            var context = new StandardEvaluationContext(new ArrayTestBean());
            var value = expression.GetValue(context);
            var condition = value is int;
            Assert.True(condition);
            Assert.Equal(4, value);
        }

        [Fact]
        public void SelectionWithMap()
        {
            var context = new StandardEvaluationContext(new MapTestBean());
            var parser = new SpelExpressionParser();
            var exp = parser.ParseExpression("Colors.?[Key.StartsWith('b')]");

            var colorsMap = (Dictionary<object, object>)exp.GetValue(context);
            Assert.Equal(3, colorsMap.Count);
            Assert.True(colorsMap.ContainsKey("beige"));
            Assert.True(colorsMap.ContainsKey("blue"));
            Assert.True(colorsMap.ContainsKey("brown"));
        }

        [Fact]
        public void SelectFirstItemInMap()
        {
            var context = new StandardEvaluationContext(new MapTestBean());
            var parser = new SpelExpressionParser();

            var exp = parser.ParseExpression("Colors.^[Key.StartsWith('b')]");
            var colorsMap = (Dictionary<object, object>)exp.GetValue(context);
            Assert.Single(colorsMap);
            Assert.True(colorsMap.ContainsKey("brown"));
        }

        [Fact]

        public void SelectLastItemInMap()
        {
            var context = new StandardEvaluationContext(new MapTestBean());
            var parser = new SpelExpressionParser();

            var exp = parser.ParseExpression("Colors.$[Key.StartsWith('b')]");
            var colorsMap = (Dictionary<object, object>)exp.GetValue(context);
            Assert.Single(colorsMap);
            Assert.True(colorsMap.ContainsKey("beige"));
        }

        [Fact]
        public void ProjectionWithList()
        {
            var expression = new SpelExpressionParser().ParseRaw("#testList.![Wrapper.Value]");
            var context = new StandardEvaluationContext();
            context.SetVariable("testList", IntegerTestBean.CreateList());
            var value = expression.GetValue(context);
            var condition = value is List<object>;
            Assert.True(condition);
            var list = (List<object>)value;
            Assert.Equal(3, list.Count);
            Assert.Equal(5, list[0]);
            Assert.Equal(6, list[1]);
            Assert.Equal(7, list[2]);
        }

        [Fact]
        public void ProjectionWithSet()
        {
            var expression = new SpelExpressionParser().ParseRaw("#testList.![Wrapper.Value]");
            var context = new StandardEvaluationContext();
            context.SetVariable("testList", IntegerTestBean.CreateSet());
            var value = expression.GetValue(context);
            var condition = value is List<object>;
            Assert.True(condition);
            var list = (List<object>)value;
            Assert.Equal(3, list.Count);
            Assert.Equal(5, list[0]);
            Assert.Equal(6, list[1]);
            Assert.Equal(7, list[2]);
        }

        [Fact]
        public void ProjectionWithIEnumerable()
        {
            var expression = new SpelExpressionParser().ParseRaw("#testList.![Wrapper.Value]");
            var context = new StandardEvaluationContext();
            context.SetVariable("testList", IntegerTestBean.CreateIterable());
            var value = expression.GetValue(context);
            var condition = value is List<object>;
            Assert.True(condition);
            var list = (List<object>)value;
            Assert.Equal(3, list.Count);
            Assert.Equal(5, list[0]);
            Assert.Equal(6, list[1]);
            Assert.Equal(7, list[2]);
        }

        [Fact]
        public void ProjectionWithArray()
        {
            var expression = new SpelExpressionParser().ParseRaw("#testArray.![Wrapper.Value]");
            var context = new StandardEvaluationContext();
            context.SetVariable("testArray", IntegerTestBean.CreateArray());
            var value = expression.GetValue(context);
            Assert.True(value.GetType().IsArray);
            var typedValue = new TypedValue(value);
            Assert.Equal(typeof(ValueType), typedValue.TypeDescriptor.GetElementType());
            var array = (ValueType[])value;
            Assert.Equal(3, array.Length);
            Assert.Equal(5, array[0]);
            Assert.Equal(5.9f, array[1]);
            Assert.Equal(7, array[2]);
        }

        public class ListTestBean
        {
            private readonly List<int> _integers = new ();

            public ListTestBean()
            {
                for (var i = 0; i < 10; i++)
                {
                    _integers.Add(i);
                }
            }

            public List<int> Integers => _integers;
        }

        public class SetTestBean
        {
            private readonly ISet<int> _integers = new HashSet<int>();

            public SetTestBean()
            {
                for (var i = 0; i < 10; i++)
                {
                    _integers.Add(i);
                }
            }

            public ISet<int> Integers => _integers;
        }

        public class IEnumerableTestBean
        {
            private readonly ISet<int> _integers = new HashSet<int>();

            public IEnumerableTestBean()
            {
                for (var i = 0; i < 10; i++)
                {
                    _integers.Add(i);
                }
            }

            public IEnumerable<int> Integers => _integers;
        }

        public class ArrayTestBean
        {
            private readonly int[] _ints = new int[10];

            public ArrayTestBean()
            {
                for (var i = 0; i < 10; i++)
                {
                    _ints[i] = i;
                }
            }

            public int[] Ints => _ints;
        }

        public class MapTestBean
        {
            private readonly Dictionary<string, string> _colors = new ();

            public MapTestBean()
            {
                // colors.put("black", "schwarz");
                _colors.Add("red", "rot");
                _colors.Add("brown", "braun");
                _colors.Add("blue", "blau");
                _colors.Add("yellow", "gelb");
                _colors.Add("beige", "beige");
            }

            public Dictionary<string, string> Colors => _colors;
        }

        public class IntegerTestBean
        {
            private readonly IntegerWrapper _wrapper;

            public IntegerTestBean(float value)
            {
                _wrapper = new IntegerWrapper(value);
            }

            public IntegerTestBean(int value)
            {
                _wrapper = new IntegerWrapper(value);
            }

            public IntegerWrapper Wrapper => _wrapper;

            public static List<IntegerTestBean> CreateList()
            {
                var list = new List<IntegerTestBean>();
                for (var i = 0; i < 3; i++)
                {
                    list.Add(new IntegerTestBean(i + 5));
                }

                return list;
            }

            public static ISet<IntegerTestBean> CreateSet()
            {
                var set = new HashSet<IntegerTestBean>();
                for (var i = 0; i < 3; i++)
                {
                    set.Add(new IntegerTestBean(i + 5));
                }

                return set;
            }

            public static IEnumerable<IntegerTestBean> CreateIterable()
            {
                var set = CreateSet();
                return set;
            }

            public static IntegerTestBean[] CreateArray()
            {
                var array = new IntegerTestBean[3];
                for (var i = 0; i < 3; i++)
                {
                    if (i == 1)
                    {
                        array[i] = new IntegerTestBean(5.9f);
                    }
                    else
                    {
                        array[i] = new IntegerTestBean(i + 5);
                    }
                }

                return array;
            }
        }

        public class IntegerWrapper
        {
            private readonly int? _int;
            private readonly float? _float;

            public IntegerWrapper(float value)
            {
                _float = value;
            }

            public IntegerWrapper(int value)
            {
                _int = value;
            }

            public object Value
            {
                get
                {
                    if (_int.HasValue)
                    {
                        return _int.Value;
                    }

                    return _float.Value;
                }
            }
        }
    }
}
