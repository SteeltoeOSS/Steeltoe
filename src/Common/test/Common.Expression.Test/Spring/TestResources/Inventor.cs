// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Spring.TestResources
{
    public class Inventor
    {
        public List<int> ListOfInteger = new List<int>();
        public List<bool> BoolList = new List<bool>();
        public Dictionary<string, bool> MapOfstringToBoolean = new Dictionary<string, bool>();
        public Dictionary<int, string> MapOfNumbersUpToTen = new Dictionary<int, string>();
        public List<int> ListOfNumbersUpToTen = new List<int>();
        public List<int> ListOneFive = new List<int>();
        public string[] StringArrayOfThreeItems = new string[] { "1", "2", "3" };
        public int Counter;
        public string _name;
        public string _name_;
        public string RandomField;
        public Dictionary<string, string> TestDictionary;
        public string PublicName;
        public ArrayContainer ArrayContainer;
        public bool PublicBoolean;

        private string name;
        private PlaceOfBirth placeOfBirth;
        private DateTime birthdate;
        private string nationality;
        private string[] inventions;
        private bool wonNobelPrize;
        private PlaceOfBirth[] placesLived;
        private List<PlaceOfBirth> placesLivedList = new List<PlaceOfBirth>();
        private bool accessedThroughGetSet;
        private string foo;

        public Inventor(string name, DateTime birthdate, string nationality)
        {
            this.name = name;
            _name = name;
            _name_ = name;
            this.birthdate = birthdate;
            this.nationality = nationality;
            ArrayContainer = new ArrayContainer();
            TestDictionary = new Dictionary<string, string>();
            TestDictionary.Add("monday", "montag");
            TestDictionary.Add("tuesday", "dienstag");
            TestDictionary.Add("wednesday", "mittwoch");
            TestDictionary.Add("thursday", "donnerstag");
            TestDictionary.Add("friday", "freitag");
            TestDictionary.Add("saturday", "samstag");
            TestDictionary.Add("sunday", "sonntag");
            ListOneFive.Add(1);
            ListOneFive.Add(5);
            BoolList.Add(false);
            BoolList.Add(false);
            ListOfNumbersUpToTen.Add(1);
            ListOfNumbersUpToTen.Add(2);
            ListOfNumbersUpToTen.Add(3);
            ListOfNumbersUpToTen.Add(4);
            ListOfNumbersUpToTen.Add(5);
            ListOfNumbersUpToTen.Add(6);
            ListOfNumbersUpToTen.Add(7);
            ListOfNumbersUpToTen.Add(8);
            ListOfNumbersUpToTen.Add(9);
            ListOfNumbersUpToTen.Add(10);
            MapOfNumbersUpToTen.Add(1, "one");
            MapOfNumbersUpToTen.Add(2, "two");
            MapOfNumbersUpToTen.Add(3, "three");
            MapOfNumbersUpToTen.Add(4, "four");
            MapOfNumbersUpToTen.Add(5, "five");
            MapOfNumbersUpToTen.Add(6, "six");
            MapOfNumbersUpToTen.Add(7, "seven");
            MapOfNumbersUpToTen.Add(8, "eight");
            MapOfNumbersUpToTen.Add(9, "nine");
            MapOfNumbersUpToTen.Add(10, "ten");
        }

        public string[] Inventions
        {
            get => inventions;
            set => inventions = value;
        }

        public PlaceOfBirth PlaceOfBirth
        {
            get => placeOfBirth;
            set
            {
                placeOfBirth = value;
                placesLived = new PlaceOfBirth[] { value };
                placesLivedList.Add(value);
            }
        }

        public int ThrowException(int valueIn)
        {
            Counter++;
            if (valueIn == 1)
            {
                throw new ArgumentException("IllegalArgumentException for 1");
            }

            if (valueIn == 2)
            {
                throw new SystemException("RuntimeException for 2");
            }

            if (valueIn == 4)
            {
                throw new TestException();
            }

            return valueIn;
        }

        public string ThrowException(PlaceOfBirth pob)
        {
            return pob.City;
        }

        public string Name => name;

        public bool WonNobelPrize
        {
            get => wonNobelPrize;
            set => wonNobelPrize = value;
        }

        public PlaceOfBirth[] PlacesLived
        {
            get => placesLived;
            set => placesLived = value;
        }

        public List<PlaceOfBirth> PlacesLivedList
        {
            get => placesLivedList;
            set => placesLivedList = value;
        }

        public string Echo(object o)
        {
            return o.ToString();
        }

        public string SayHelloTo(string person)
        {
            return "hello " + person;
        }

        public string PrintDouble(double d)
        {
            return d.ToString("F2");
        }

        public string PrintDoubles(double[] d)
        {
            return "{" + string.Join(", ", d) + "}";
        }

        public List<string> GetDoublesAsStringList()
        {
            var result = new List<string>();
            result.Add("14.35");
            result.Add("15.45");
            return result;
        }

        public string JoinThreeStrings(string a, string b, string c)
        {
            return a + b + c;
        }

        public int AVarargsMethod(params string[] strings)
        {
            if (strings == null)
            {
                return 0;
            }

            return strings.Length;
        }

        public int AVarargsMethod2(int i, params string[] strings)
        {
            if (strings == null)
            {
                return i;
            }

            return strings.Length + i;
        }

        public Inventor(params string[] strings)
        {
        }

        public bool SomeProperty
        {
            get => accessedThroughGetSet;
            set => accessedThroughGetSet = value;
        }

        public DateTime BirthDate => birthdate;

        public string Foo
        {
            get => foo;
            set => foo = value;
        }

        public string Nationality => nationality;

        public class TestException : Exception
        {
        }
    }
}
