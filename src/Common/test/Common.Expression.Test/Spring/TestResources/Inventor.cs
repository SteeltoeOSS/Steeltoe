// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources;
#pragma warning disable IDE1006 // Naming Styles
public class Inventor
{
    public List<int> ListOfInteger = new ();
    public List<bool> BoolList = new ();
    public Dictionary<string, bool> MapOfstringToBoolean = new ();
    public Dictionary<int, string> MapOfNumbersUpToTen = new ();
    public List<int> ListOfNumbersUpToTen = new ();
    public List<int> ListOneFive = new ();
    public string[] StringArrayOfThreeItems = { "1", "2", "3" };
    public int Counter;
    public string _name;
    public string _name_;
    public string RandomField;
    public Dictionary<string, string> TestDictionary;
    public string PublicName;
    public ArrayContainer ArrayContainer;
    public bool PublicBoolean;

    private PlaceOfBirth _placeOfBirth;

    public Inventor(params string[] strings)
    {
    }

    public Inventor(string name, DateTime birthdate, string nationality)
    {
        Name = name;
        _name = name;
        _name_ = name;
        BirthDate = birthdate;
        Nationality = nationality;
        ArrayContainer = new ArrayContainer();
        TestDictionary = new Dictionary<string, string>
        {
            { "monday", "montag" },
            { "tuesday", "dienstag" },
            { "wednesday", "mittwoch" },
            { "thursday", "donnerstag" },
            { "friday", "freitag" },
            { "saturday", "samstag" },
            { "sunday", "sonntag" }
        };
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

    public string[] Inventions { get; set; }

    public PlaceOfBirth PlaceOfBirth
    {
        get => _placeOfBirth;
        set
        {
            _placeOfBirth = value;
            PlacesLived = new[] { value };
            PlacesLivedList.Add(value);
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

    public string Name { get; }

    public bool WonNobelPrize { get; set; }

    public PlaceOfBirth[] PlacesLived { get; set; }

    public List<PlaceOfBirth> PlacesLivedList { get; set; } = new ();

    public string Echo(object o)
    {
        return o.ToString();
    }

    public string SayHelloTo(string person)
    {
        return $"hello {person}";
    }

    public string PrintDouble(double d)
    {
        return d.ToString("F2");
    }

    public string PrintDoubles(double[] d)
    {
        return $"{{{string.Join(", ", d)}}}";
    }

    public List<string> GetDoublesAsStringList()
    {
        var result = new List<string>
        {
            "14.35",
            "15.45"
        };
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

    public bool SomeProperty { get; set; }

    public DateTime BirthDate { get; }

    public string Foo { get; set; }

    public string Nationality { get; }

    public class TestException : Exception
    {
    }
}
#pragma warning restore IDE1006 // Naming Styles
