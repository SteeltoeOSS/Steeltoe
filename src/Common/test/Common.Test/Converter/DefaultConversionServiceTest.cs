// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Converter.Test
{
    public class DefaultConversionServiceTest
    {
        private enum Foo
        {
            BAR,
            BAZ
        }

        // Static so only one is created for this battery of tests.. ensures internal cache is filled by all of the tests
        private static readonly DefaultConversionService ConversionService = new ();

        [Fact]
        public void TestStringToCharacter()
        {
            Assert.Equal('1', ConversionService.Convert<char>("1"));
            Assert.Equal('1', ConversionService.Convert<char?>("1"));
        }

        [Fact]
        public void TestStringToCharacterEmptyString()
        {
            Assert.Null(ConversionService.Convert<char?>(string.Empty));
        }

        [Fact]
        public void TestStringToCharacterInvalidString()
        {
            Assert.Throws<ConversionFailedException>(() => ConversionService.Convert<char>("invalid"));
            Assert.Throws<ConversionFailedException>(() => ConversionService.Convert<char?>("invalid"));
        }

        [Fact]
        public void TestCharacterToString()
        {
            Assert.Equal("3", ConversionService.Convert<string>('3'));
        }

        [Fact]
        public void TestStringToBooleanTrue()
        {
            Assert.True(ConversionService.Convert<bool>("true"));
            Assert.True(ConversionService.Convert<bool>("on"));
            Assert.True(ConversionService.Convert<bool>("yes"));
            Assert.True(ConversionService.Convert<bool>("1"));
            Assert.True(ConversionService.Convert<bool>("TRUE"));
            Assert.True(ConversionService.Convert<bool>("ON"));
            Assert.True(ConversionService.Convert<bool>("YES"));

            Assert.True(ConversionService.Convert<bool?>("true"));
            Assert.True(ConversionService.Convert<bool?>("on"));
            Assert.True(ConversionService.Convert<bool?>("yes"));
            Assert.True(ConversionService.Convert<bool?>("1"));
            Assert.True(ConversionService.Convert<bool?>("TRUE"));
            Assert.True(ConversionService.Convert<bool?>("ON"));
            Assert.True(ConversionService.Convert<bool?>("YES"));
        }

        [Fact]
        public void TestStringToBooleanFalse()
        {
            Assert.False(ConversionService.Convert<bool>("false"));
            Assert.False(ConversionService.Convert<bool>("off"));
            Assert.False(ConversionService.Convert<bool>("no"));
            Assert.False(ConversionService.Convert<bool>("0"));
            Assert.False(ConversionService.Convert<bool>("FALSE"));
            Assert.False(ConversionService.Convert<bool>("OFF"));
            Assert.False(ConversionService.Convert<bool>("NO"));

            Assert.False(ConversionService.Convert<bool?>("false"));
            Assert.False(ConversionService.Convert<bool?>("off"));
            Assert.False(ConversionService.Convert<bool?>("no"));
            Assert.False(ConversionService.Convert<bool?>("0"));
            Assert.False(ConversionService.Convert<bool?>("FALSE"));
            Assert.False(ConversionService.Convert<bool?>("OFF"));
            Assert.False(ConversionService.Convert<bool?>("NO"));
        }

        [Fact]
        public void TestStringToBooleanEmptyString()
        {
            Assert.Null(ConversionService.Convert<bool?>(string.Empty));
        }

        [Fact]
        public void TestStringToBooleanInvalidString()
        {
            Assert.Throws<ConversionFailedException>(() => ConversionService.Convert<bool>("invalid"));
        }

        [Fact]
        public void TestBooleanToString()
        {
            Assert.Equal("True", ConversionService.Convert<string>(true));
        }

        [Fact]
        public void TestStringToByte()
        {
            Assert.Equal((byte)1, ConversionService.Convert<byte>("1"));
            Assert.Equal((sbyte)-1, ConversionService.Convert<sbyte>("-1"));
            Assert.Equal((byte)1, ConversionService.Convert<byte?>("1"));
            Assert.Equal((sbyte)-1, ConversionService.Convert<sbyte?>("-1"));
        }

        [Fact]
        public void TestByteToString()
        {
            Assert.Equal("65", ConversionService.Convert<string>(Encoding.UTF8.GetBytes("A")[0]));
            Assert.Equal("-1", ConversionService.Convert<string>((sbyte)-1));
        }

        [Fact]
        public void TestStringToShort()
        {
            Assert.Equal((short)1, ConversionService.Convert<short>("1"));
            Assert.Equal((ushort)1, ConversionService.Convert<ushort>("1"));
            Assert.Equal((short)1, ConversionService.Convert<short?>("1"));
            Assert.Equal((ushort)1, ConversionService.Convert<ushort?>("1"));
        }

        [Fact]
        public void TestShortToString()
        {
            short three = 3;
            ushort uthree = 3;
            Assert.Equal("3", ConversionService.Convert<string>(three));
            Assert.Equal("3", ConversionService.Convert<string>(uthree));
        }

        [Fact]
        public void TestStringToInteger()
        {
            Assert.Equal(1, ConversionService.Convert<int?>("1"));
            Assert.Equal(1, ConversionService.Convert<int>("1"));
            Assert.Equal(1U, ConversionService.Convert<uint>("1"));
            Assert.Equal(1U, ConversionService.Convert<uint?>("1"));
        }

        [Fact]
        public void TestIntegerToString()
        {
            Assert.Equal("3", ConversionService.Convert<string>(3));
            Assert.Equal("3", ConversionService.Convert<string>(3U));
        }

        [Fact]
        public void TestStringToLong()
        {
            Assert.Equal(1L, ConversionService.Convert<long>("1"));
            Assert.Equal(1UL, ConversionService.Convert<ulong>("1"));
            Assert.Equal(1L, ConversionService.Convert<long?>("1"));
            Assert.Equal(1UL, ConversionService.Convert<ulong?>("1"));
        }

        [Fact]
        public void TestLongToString()
        {
            Assert.Equal("3", ConversionService.Convert<string>(3L));
            Assert.Equal("3", ConversionService.Convert<string>(3UL));
        }

        [Fact]
        public void TestStringToFloat()
        {
            Assert.Equal(1.0f, ConversionService.Convert<float>("1.0"));
            Assert.Equal(1.0f, ConversionService.Convert<float?>("1.0"));
        }

        [Fact]
        public void TestFloatToString()
        {
            Assert.Equal("3.1", ConversionService.Convert<string>(3.1f));
        }

        [Fact]
        public void TestStringToDouble()
        {
            Assert.Equal(1.0d, ConversionService.Convert<double>("1.0"));
            Assert.Equal(1.0d, ConversionService.Convert<double?>("1.0"));
        }

        [Fact]
        public void TestDoubleToString()
        {
            Assert.Equal("3.1", ConversionService.Convert<string>(3.1d));
        }

        [Fact]
        public void TestStringToDecimal()
        {
            var result = 1.0m;
            Assert.Equal(result, ConversionService.Convert<decimal>("1.0"));
            Assert.Equal(result, ConversionService.Convert<decimal?>("1.0"));
        }

        [Fact]
        public void TestDecimalToString()
        {
            var source = 300.00m;
            Assert.Equal("300.00", ConversionService.Convert<string>(source));
        }

        [Fact]
        public void TestStringToNumberEmptyString()
        {
            Assert.Null(ConversionService.Convert<int?>(string.Empty));
            Assert.Null(ConversionService.Convert<uint?>(string.Empty));
        }

        [Fact]
        public void TestStringToEnum()
        {
            Assert.Equal(Foo.BAR, ConversionService.Convert<Foo>("BAR"));
            Assert.Equal(Foo.BAR, ConversionService.Convert<Foo?>("BAR"));
            Assert.Equal(Foo.BAR, ConversionService.Convert<Foo>("bar"));
            Assert.Equal(Foo.BAR, ConversionService.Convert<Foo?>("bar"));
        }

        [Fact]
        public void TestStringToEnumEmptyString()
        {
            Assert.Null(ConversionService.Convert<Foo?>(string.Empty));
        }

        [Fact]
        public void TestEnumToString()
        {
            Assert.Equal("BAR", ConversionService.Convert<string>(Foo.BAR));
        }

        [Fact]
        public void TestStringToString()
        {
            var str = "test";
            Assert.Same(str, ConversionService.Convert<string>(str));
        }

        [Fact]
        public void TestGuidToStringAndStringToGuid()
        {
            var uuid = Guid.NewGuid();
            var convertToString = ConversionService.Convert<string>(uuid);
            var convertToUUID = ConversionService.Convert<Guid>(convertToString);
            Assert.Equal(uuid, convertToUUID);
        }

        [Fact]
        public void TestNumberToNumber()
        {
            Assert.Equal(1L, ConversionService.Convert<long>(1L));
        }

        [Fact]
        public void TestNumberToNumberNotSupportedNumber()
        {
            Assert.Throws<ConversionFailedException>(() => ConversionService.Convert<double>('a'));
        }

        [Fact]
        public void TestNumberToCharacter()
        {
            Assert.Equal('A', ConversionService.Convert<char>(65));
        }

        [Fact]
        public void TestCharacterToNumber()
        {
            Assert.Equal(65, ConversionService.Convert<int>('A'));
        }

        [Fact]
        public void ConvertArrayToCollectionInterface()
        {
            var result = ConversionService.Convert<IList<string>>(new[] { "1", "2", "3" });
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void ConvertArrayToEnumerableInterface()
        {
            var array = new[] { "1", "2", "3" };
            var result = ConversionService.Convert<IEnumerable>(array);
            Assert.IsType<string[]>(result);
            Assert.Same(result, array);
        }

        [Fact]
        public void ConvertArrayToEnumerableStringInterface()
        {
            var array = new[] { "1", "2", "3" };
            var result = ConversionService.Convert<IEnumerable<string>>(array);
            Assert.IsType<string[]>(result);
            Assert.Same(result, array);
        }

        [Fact]
        public void ConvertArrayToEnumerableGenericTypeConversion()
        {
            var array = new[] { "1", "2", "3" };
            var result = ConversionService.Convert<IEnumerable<int>>(array);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
        }

        [Fact]
        public void ConvertArrayToCollectionGenericTypeConversion()
        {
            var result = ConversionService.Convert<IList<int>>(new[] { "1", "2", "3" });
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertArrayToCollectionImpl()
        {
            var result = ConversionService.Convert<List<string>>(new[] { "1", "2", "3" });
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void ConvertArrayToAbstractCollection()
        {
            // No public constructor found
            Assert.Throws<ConverterNotFoundException>(() => ConversionService.Convert<CollectionBase>(new[] { "1", "2", "3" }));
        }

        [Fact]
        public void ConvertArrayToString()
        {
            var result = ConversionService.Convert<string>(new[] { "1", "2", "3" });
            Assert.Equal("1,2,3", result);
        }

        [Fact]
        public void ConvertArrayToStringWithElementConversion()
        {
            var result = ConversionService.Convert<string>(new[] { 1, 2, 3 });
            Assert.Equal("1,2,3", result);
        }

        [Fact]
        public void ConvertEmptyArrayToString()
        {
            var result = ConversionService.Convert<string>(Array.Empty<string>());
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ConvertStringToArray()
        {
            var result = ConversionService.Convert<string[]>("1,2,3");
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void ConvertStringToArrayWithElementConversion()
        {
            var result = ConversionService.Convert<int[]>("1,2,3");
            Assert.Equal(3, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertEmptyStringToArray()
        {
            var result = ConversionService.Convert<string[]>(string.Empty);
            Assert.Empty(result);
        }

        [Fact]
        public void ConvertArrayToObject()
        {
            var array = new object[] { 3L };
            var result = ConversionService.Convert<long>(array);
            Assert.Equal(3L, result);
        }

        [Fact]
        public void ConvertArrayToObjectWithElementConversion()
        {
            object[] array = new[] { "3" };
            var result = ConversionService.Convert<int>(array);
            Assert.Equal(3, result);
        }

        [Fact]
        public void ConvertArrayToObjectAssignableTargetType()
        {
            var array = new[] { 3L };
            var result = ConversionService.Convert<long[]>(array);
            Assert.Equal(array, result);
        }

        [Fact]
        public void ConvertObjectToArray()
        {
            var result = ConversionService.Convert<object[]>(3L);
            Assert.Single(result);
            Assert.Equal(3L, result[0]);
        }

        [Fact]
        public void ConvertObjectToArrayWithElementConversion()
        {
            var result = ConversionService.Convert<int[]>(3L);
            Assert.Single(result);
            Assert.Equal(3, result[0]);
        }

        [Fact]
        public void ConvertCollectionToArray()
        {
            IList<string> list = new List<string>();
            list.Add("1");
            list.Add("2");
            list.Add("3");
            var result = ConversionService.Convert<string[]>(list);
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void ConvertSetToArray()
        {
            ISet<string> set = new HashSet<string>();
            set.Add("1");
            set.Add("2");
            set.Add("3");
            var result = ConversionService.Convert<string[]>(set);
            Assert.Equal(3, result.Length);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void ConvertSetToArrayWithElementConversion()
        {
            ISet<string> set = new HashSet<string>();
            set.Add("1");
            set.Add("2");
            set.Add("3");
            var result = ConversionService.Convert<int[]>(set);
            Assert.Equal(3, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertCollectionToArrayWithElementConversion()
        {
            IList<long> list = new List<long>();
            list.Add(1L);
            list.Add(2L);
            list.Add(3L);
            var result = ConversionService.Convert<int[]>(list);
            Assert.Equal(3, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertCollectionToString()
        {
            var list = new List<string>() { "foo", "bar" };
            var result = ConversionService.Convert<string>(list);
            Assert.Equal("foo,bar", result);
        }

        [Fact]
        public void ConvertCollectionToStringWithElementConversion()
        {
            var list = new List<int>() { 3, 5 };
            var result = ConversionService.Convert<string>(list);
            Assert.Equal("3,5", result);
        }

        [Fact]
        public void ConvertStringToCollection()
        {
            var result = ConversionService.Convert<IList<string>>("1,2,3");
            Assert.Equal(3, result.Count);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void ConvertStringToEnumerable()
        {
            var result = ConversionService.Convert<IEnumerable<string>>("1,2,3");
            Assert.Equal(3, result.Count());
            Assert.Contains("1", result);
            Assert.Contains("2", result);
            Assert.Contains("3", result);
        }

        [Fact]
        public void ConvertStringToEnumerableWithElementConversion()
        {
            var result = ConversionService.Convert<IEnumerable<int>>("1,2,3");
            Assert.Equal(3, result.Count());
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
        }

        [Fact]
        public void ConvertStringToCollectionWithElementConversion()
        {
            var result = ConversionService.Convert<IList<int>>("1,2,3");
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertEmptyStringToCollection()
        {
            var result = ConversionService.Convert<IList<int>>(string.Empty);
            Assert.Empty(result);
        }

        [Fact]
        public void ConvertCollectionToObject()
        {
            var list = new List<long>() { 3L };
            var result = ConversionService.Convert<long>(list);
            Assert.Equal(3L, result);
        }

        [Fact]
        public void ConvertCollectionToObjectWithElementConversion()
        {
            var list = new List<string>() { "3" };
            var result = ConversionService.Convert<int>(list);
            Assert.Equal(3, result);
        }

        [Fact]
        public void ConvertCollectionToObjectAssignableTarget()
        {
            IList<string> source = new List<string>() { "foo" };
            var result = ConversionService.Convert<object>(source);
            Assert.Same(source, result);
        }

        [Fact]
        public void ConvertListToDictionaryAssignableTarget()
        {
            IList<object> source = new List<object>() { new Dictionary<string, object>() { ["test"] = 3 } };
            var result = ConversionService.Convert<IDictionary<string, object>>(source);
            Assert.Equal(3, result["test"]);
        }

        [Fact]
        public void ConvertObjectToCollection()
        {
            var result = ConversionService.Convert<List<long>>(3L);
            Assert.Single(result);
            Assert.Equal(3L, result[0]);
        }

        [Fact]
        public void ConvertObjectToEnumerableInterface()
        {
            var result = ConversionService.Convert<IEnumerable>(3L);
            Assert.Single(result);
            foreach (var elem in result)
            {
                Assert.Equal(3L, elem);
            }
        }

        [Fact]
        public void ConvertObjectToCollectionWithElementConversion()
        {
            var result = ConversionService.Convert<List<int>>(3L);
            Assert.Single(result);
            Assert.Equal(3, result[0]);
        }

        [Fact]
        public void ConvertObjectToEnumerableWithElementConversion()
        {
            var result = ConversionService.Convert<IEnumerable<int>>(3L);
            Assert.Single(result);
            Assert.Contains(3, result);
        }

        [Fact]
        public void ConvertStringArrayToIntegerArray()
        {
            var result = ConversionService.Convert<int[]>(new[] { "1", "2", "3" });
            Assert.Equal(3, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertIntegerArrayToIntegerArray()
        {
            var result = ConversionService.Convert<int[]>(new[] { 1, 2, 3 });
            Assert.Equal(3, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertObjectArrayToIntegerArray()
        {
            var result = ConversionService.Convert<int[]>(new object[] { 1, 2, 3 });
            Assert.Equal(3, result.Length);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertArrayToArrayAssignable()
        {
            var orig = new[] { 1, 2, 3 };
            var result = ConversionService.Convert<int[]>(orig);
            Assert.Same(orig, result);
        }

        [Fact]
        public void ConvertListOfStringToString()
        {
            var list = new List<string>() { "Foo", "Bar" };
            var result = ConversionService.Convert<string>(list);
            Assert.Equal("Foo,Bar", result);
        }

        [Fact]
        public void ConvertListOfListToString()
        {
            var list1 = new List<string>() { "Foo", "Bar" };
            var list2 = new List<string>() { "Baz", "Boop" };
            var list = new List<List<string>>() { list1, list2 };
            var result = ConversionService.Convert<string>(list);
            Assert.Equal("Foo,Bar,Baz,Boop", result);
        }

        [Fact]
        public void ConvertCollectionToCollectionWithElementConversion()
        {
            var foo = new Collection<string> { "1", "2", "3" };

            var result = ConversionService.Convert<List<int>>(foo);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertSetToCollectionWithElementConversion()
        {
            ISet<string> foo = new HashSet<string>();
            foo.Add("1");
            foo.Add("2");
            foo.Add("3");

            var result = ConversionService.Convert<List<int>>(foo);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void ConvertCollectionToCollectionNull()
        {
            var bar = (List<int>)ConversionService.Convert(null, typeof(ICollection), typeof(List<int>));
            Assert.Null(bar);
        }

        [Fact]
        public void ConvertCollectionToCollectionNotGeneric()
        {
            var foo = new Collection<string> { "1", "2", "3" };
            var result = ConversionService.Convert<IList>(foo);
            Assert.Equal(3, result.Count);
            Assert.Equal("1", result[0]);
            Assert.Equal("2", result[1]);
            Assert.Equal("3", result[2]);
        }

        [Fact]
        public void CollectionToCollectionEnumerableWithElementConversion()
        {
            var strings = new ArrayList { "3", "9" };

            var result = ConversionService.Convert<IEnumerable<int>>(strings);
            Assert.Contains(3, result);
            Assert.Contains(9, result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void CollectionToCollectionEnumerableString()
        {
            var strings = new ArrayList { "3", "9" };

            var result = ConversionService.Convert<IEnumerable<string>>(strings);
            Assert.Contains("3", result);
            Assert.Contains("9", result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void CollectionToCollectionStringCollection()
        {
            var strings = new ArrayList { "3", "9" };

            var result = ConversionService.Convert<StringCollection>(strings);
            Assert.Contains("3", result[0]);
            Assert.Contains("9", result[1]);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void ConvertDictToDictWithElementConversion()
        {
            var foo = new Dictionary<string, string> { { "1", "BAR" }, { "2", "BAZ" } };

            var map = ConversionService.Convert<Dictionary<int, Foo>>(foo);
            Assert.Equal(Foo.BAR, map[1]);
            Assert.Equal(Foo.BAZ, map[2]);
        }

        [Fact]
        public void ConvertDictionaryValuesToList()
        {
            IDictionary<string, int> hashMap = new Dictionary<string, int>();
            hashMap.Add("1", 1);
            hashMap.Add("2", 2);
            var converted = ConversionService.Convert<List<int>>(hashMap.Values);
            Assert.Contains(1, converted);
            Assert.Contains(2, converted);
        }

        [Fact]
        public void ConvertDictionaryBothElementConvertion()
        {
            var strings = new Dictionary<string, string> { { "3", "9" }, { "6", "31" } };
            var integers = ConversionService.Convert<IDictionary<int, int>>(strings);
            Assert.Equal(9, integers[3]);
            Assert.Equal(31, integers[6]);
        }

        [Fact]
        public void ConvertHashtableToDictionary()
        {
            var strings = new Hashtable { { "3", "9" }, { "6", "31" } };
            var integers = ConversionService.Convert<IDictionary<int, int>>(strings);
            Assert.Equal(9, integers[3]);
            Assert.Equal(31, integers[6]);
        }

        [Fact]
        public void ConvertHashtableToSortedList()
        {
            var strings = new Hashtable { { "3", "9" }, { "6", "31" } };
            var integers = ConversionService.Convert<SortedList>(strings);
            Assert.Equal("9", integers["3"]);
            Assert.Equal("31", integers["6"]);
        }

        [Fact]
        public void ConvertDictionaryConcurrentDictionary()
        {
            var strings = new Dictionary<string, string> { { "3", "9" }, { "6", "31" } };
            var integers = ConversionService.Convert<ConcurrentDictionary<int, int>>(strings);
            Assert.Equal(9, integers[3]);
            Assert.Equal(31, integers[6]);
        }

        [Fact]
        public void ConvertObjectToStringWithValueOfMethodPresentUsingToString()
        {
            ISBN.Reset();
            Assert.Equal("123456789", ConversionService.Convert<string>(new ISBN("123456789")));

            Assert.Equal(1, ISBN.ConstructorCount);
            Assert.Equal(0, ISBN.ValueOfCount);
            Assert.Equal(1, ISBN.ToStringCount);
        }

        [Fact]
        public void ConvertObjectToObjectUsingValueOfMethod()
        {
            ISBN.Reset();
            Assert.Equal(new ISBN("123456789"), ConversionService.Convert<ISBN>("123456789"));

            Assert.Equal(2, ISBN.ConstructorCount);
            Assert.Equal(1, ISBN.ValueOfCount);
            Assert.Equal(0, ISBN.ToStringCount);
        }

        [Fact]
        public void ConvertObjectToStringUsingToString()
        {
            SSN.Reset();
            Assert.Equal("123456789", ConversionService.Convert<string>(new SSN("123456789")));

            Assert.Equal(1, SSN.ConstructorCount);
            Assert.Equal(1, SSN.ToStringCount);
        }

        [Fact]
        public void ConvertObjectToObjectUsingObjectConstructor()
        {
            SSN.Reset();
            Assert.Equal(new SSN("123456789"), ConversionService.Convert<SSN>("123456789"));

            Assert.Equal(2, SSN.ConstructorCount);
            Assert.Equal(0, SSN.ToStringCount);
        }

        [Fact]
        public void ConvertObjectToObjectNoValueOfMethodOrConstructor()
        {
            Assert.Throws<ConverterNotFoundException>(() => ConversionService.Convert<SSN>(3L));
        }

        [Fact]
        public void ConvertCharArrayToString()
        {
            var converted = ConversionService.Convert<string>(new[] { 'a', 'b', 'c' });
            Assert.Equal("a,b,c", converted);
        }

        [Fact]
        public void ConvertStringToCharArray()
        {
            var converted = ConversionService.Convert<char[]>("a,b,c");
            Assert.Equal(new[] { 'a', 'b', 'c' }, converted);
        }

        [Fact]
        public void MultidimensionalArrayToListConversionShouldConvertEntriesCorrectly()
        {
            var grid = new[]
            {
                new[] { "1", "2", "3", "4" }, new[] { "5", "6", "7", "8" },
                new[] { "9", "10", "11", "12" }
            };
            var converted = ConversionService.Convert<List<string[]>>(grid);
            var convertedBack = ConversionService.Convert<string[][]>(converted);
            Assert.Equal(grid, convertedBack);
        }

        [Fact]
        public void TestStringToEncoding()
        {
            Assert.Equal(Encoding.UTF8, ConversionService.Convert<Encoding>("UTF-8"));
        }

        [Fact]
        public void TestEncodingToString()
        {
            Assert.Equal("utf-8", ConversionService.Convert<string>(Encoding.UTF8));
        }

        private class ISBN
        {
            public static int ConstructorCount;
            public static int ToStringCount;
            public static int ValueOfCount;

            public static void Reset()
            {
                ConstructorCount = 0;
                ToStringCount = 0;
                ValueOfCount = 0;
            }

            public static ISBN ValueOf(string value)
            {
                ValueOfCount++;
                return new ISBN(value);
            }

            private readonly string value;

            public ISBN(string value)
            {
                ConstructorCount++;
                this.value = value;
            }

            public override bool Equals(object o)
            {
                if (o is not ISBN isbn)
                {
                    return false;
                }

                return value.Equals(isbn.value);
            }

            public override int GetHashCode() => value.GetHashCode();

            public override string ToString()
            {
                ToStringCount++;
                return value;
            }
        }

        private class SSN
        {
            public static int ConstructorCount;

            public static int ToStringCount;

            public static void Reset()
            {
                ConstructorCount = 0;
                ToStringCount = 0;
            }

            private readonly string value;

            public SSN(string value)
            {
                ConstructorCount++;
                this.value = value;
            }

            public override bool Equals(object o)
            {
                if (o is not SSN ssn)
                {
                    return false;
                }

                return value.Equals(ssn.value);
            }

            public override int GetHashCode() => value.GetHashCode();

            public override string ToString()
            {
                ToStringCount++;
                return value;
            }
        }
    }
}
