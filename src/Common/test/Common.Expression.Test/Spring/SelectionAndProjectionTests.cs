// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class SelectionAndProjectionTests
{
    [Fact]
    public void SelectionWithList()
    {
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.?[#this<5]");
        var context = new StandardEvaluationContext(new ListTestBean());
        object value = expression.GetValue(context);
        bool condition = value is List<object>;
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
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.^[#this<5]");
        var context = new StandardEvaluationContext(new ListTestBean());
        object value = expression.GetValue(context);
        bool condition = value is int;
        Assert.True(condition);
        Assert.Equal(0, value);
    }

    [Fact]
    public void SelectLastItemInList()
    {
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.$[#this<5]");
        var context = new StandardEvaluationContext(new ListTestBean());
        object value = expression.GetValue(context);
        bool condition = value is int;
        Assert.True(condition);
        Assert.Equal(4, value);
    }

    [Fact]
    public void SelectionWithSet()
    {
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.?[#this<5]");
        var context = new StandardEvaluationContext(new SetTestBean());
        object value = expression.GetValue(context);
        bool condition = value is List<object>;
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
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.^[#this<5]");
        var context = new StandardEvaluationContext(new SetTestBean());
        object value = expression.GetValue(context);
        bool condition = value is int;
        Assert.True(condition);
        Assert.Equal(0, value);
    }

    [Fact]
    public void SelectLastItemInSet()
    {
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.$[#this<5]");
        var context = new StandardEvaluationContext(new SetTestBean());
        object value = expression.GetValue(context);
        bool condition = value is int;
        Assert.True(condition);
        Assert.Equal(4, value);
    }

    [Fact]
    public void SelectionWithIEnumerable()
    {
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.?[#this<5]");
        var context = new StandardEvaluationContext(new EnumerableTestBean());
        object value = expression.GetValue(context);
        bool condition = value is List<object>;
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
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.?[#this<5]");
        var context = new StandardEvaluationContext(new ArrayTestBean());
        object value = expression.GetValue(context);
        Assert.True(value.GetType().IsArray);
        var typedValue = new TypedValue(value);
        Assert.Equal(typeof(int), typedValue.TypeDescriptor.GetElementType());
        int[] array = (int[])value;
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
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.^[#this<5]");
        var context = new StandardEvaluationContext(new ArrayTestBean());
        object value = expression.GetValue(context);
        bool condition = value is int;
        Assert.True(condition);
        Assert.Equal(0, value);
    }

    [Fact]
    public void SelectLastItemInArray()
    {
        IExpression expression = new SpelExpressionParser().ParseRaw("Integers.$[#this<5]");
        var context = new StandardEvaluationContext(new ArrayTestBean());
        object value = expression.GetValue(context);
        bool condition = value is int;
        Assert.True(condition);
        Assert.Equal(4, value);
    }

    [Fact]
    public void SelectionWithMap()
    {
        var context = new StandardEvaluationContext(new MapTestBean());
        var parser = new SpelExpressionParser();
        IExpression exp = parser.ParseExpression("Colors.?[Key.StartsWith('b')]");

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

        IExpression exp = parser.ParseExpression("Colors.^[Key.StartsWith('b')]");
        var colorsMap = (Dictionary<object, object>)exp.GetValue(context);
        Assert.Single(colorsMap);
        Assert.True(colorsMap.ContainsKey("brown"));
    }

    [Fact]
    public void SelectLastItemInMap()
    {
        var context = new StandardEvaluationContext(new MapTestBean());
        var parser = new SpelExpressionParser();

        IExpression exp = parser.ParseExpression("Colors.$[Key.StartsWith('b')]");
        var colorsMap = (Dictionary<object, object>)exp.GetValue(context);
        Assert.Single(colorsMap);
        Assert.True(colorsMap.ContainsKey("beige"));
    }

    [Fact]
    public void ProjectionWithList()
    {
        IExpression expression = new SpelExpressionParser().ParseRaw("#testList.![Wrapper.Value]");
        var context = new StandardEvaluationContext();
        context.SetVariable("testList", IntegerTestBean.CreateList());
        object value = expression.GetValue(context);
        bool condition = value is List<object>;
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
        IExpression expression = new SpelExpressionParser().ParseRaw("#testList.![Wrapper.Value]");
        var context = new StandardEvaluationContext();
        context.SetVariable("testList", IntegerTestBean.CreateSet());
        object value = expression.GetValue(context);
        bool condition = value is List<object>;
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
        IExpression expression = new SpelExpressionParser().ParseRaw("#testList.![Wrapper.Value]");
        var context = new StandardEvaluationContext();
        context.SetVariable("testList", IntegerTestBean.CreateIterable());
        object value = expression.GetValue(context);
        bool condition = value is List<object>;
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
        IExpression expression = new SpelExpressionParser().ParseRaw("#testArray.![Wrapper.Value]");
        var context = new StandardEvaluationContext();
        context.SetVariable("testArray", IntegerTestBean.CreateArray());
        object value = expression.GetValue(context);
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
        public List<int> Integers { get; } = new();

        public ListTestBean()
        {
            for (int i = 0; i < 10; i++)
            {
                Integers.Add(i);
            }
        }
    }

    public class SetTestBean
    {
        public ISet<int> Integers { get; } = new HashSet<int>();

        public SetTestBean()
        {
            for (int i = 0; i < 10; i++)
            {
                Integers.Add(i);
            }
        }
    }

    public class EnumerableTestBean
    {
        private readonly ISet<int> _integers = new HashSet<int>();

        public IEnumerable<int> Integers => _integers;

        public EnumerableTestBean()
        {
            for (int i = 0; i < 10; i++)
            {
                _integers.Add(i);
            }
        }
    }

    public class ArrayTestBean
    {
        public int[] Integers { get; } = new int[10];

        public ArrayTestBean()
        {
            for (int i = 0; i < 10; i++)
            {
                Integers[i] = i;
            }
        }
    }

    public class MapTestBean
    {
        public Dictionary<string, string> Colors { get; } = new();

        public MapTestBean()
        {
            // colors.put("black", "schwarz");
            Colors.Add("red", "rot");
            Colors.Add("brown", "braun");
            Colors.Add("blue", "blau");
            Colors.Add("yellow", "gelb");
            Colors.Add("beige", "beige");
        }
    }

    public class IntegerTestBean
    {
        public IntegerWrapper Wrapper { get; }

        public IntegerTestBean(float value)
        {
            Wrapper = new IntegerWrapper(value);
        }

        public IntegerTestBean(int value)
        {
            Wrapper = new IntegerWrapper(value);
        }

        public static List<IntegerTestBean> CreateList()
        {
            var list = new List<IntegerTestBean>();

            for (int i = 0; i < 3; i++)
            {
                list.Add(new IntegerTestBean(i + 5));
            }

            return list;
        }

        public static ISet<IntegerTestBean> CreateSet()
        {
            var set = new HashSet<IntegerTestBean>();

            for (int i = 0; i < 3; i++)
            {
                set.Add(new IntegerTestBean(i + 5));
            }

            return set;
        }

        public static IEnumerable<IntegerTestBean> CreateIterable()
        {
            ISet<IntegerTestBean> set = CreateSet();
            return set;
        }

        public static IntegerTestBean[] CreateArray()
        {
            var array = new IntegerTestBean[3];

            for (int i = 0; i < 3; i++)
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

        public IntegerWrapper(float value)
        {
            _float = value;
        }

        public IntegerWrapper(int value)
        {
            _int = value;
        }
    }
}
