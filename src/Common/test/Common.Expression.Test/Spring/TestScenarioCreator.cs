// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Expression.Internal.Spring.TestResources;
using System;
using System.Globalization;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring;

public static class TestScenarioCreator
{
    public static StandardEvaluationContext GetTestEvaluationContext()
    {
        var testContext = new StandardEvaluationContext();
        SetupRootContextObject(testContext);
        PopulateVariables(testContext);
        PopulateFunctions(testContext);
        return testContext;
    }

    public static string IsEven(int i)
    {
        if (i % 2 == 0)
        {
            return "y";
        }

        return "n";
    }

    public static int[] ReverseInt(int i, int j, int k)
    {
        return new[] { k, j, i };
    }

    public static string ReverseString(string input)
    {
        var backwards = new StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            backwards.Append(input[input.Length - 1 - i]);
        }

        return backwards.ToString();
    }

    public static string VarargsFunctionReverseStringsAndMerge(params string[] strings)
    {
        var sb = new StringBuilder();
        if (strings != null)
        {
            for (var i = strings.Length - 1; i >= 0; i--)
            {
                sb.Append(strings[i]);
            }
        }

        return sb.ToString();
    }

    public static string VarargsFunctionReverseStringsAndMerge2(int j, params string[] strings)
    {
        var sb = new StringBuilder();
        sb.Append(j);
        if (strings != null)
        {
            for (var i = strings.Length - 1; i >= 0; i--)
            {
                sb.Append(strings[i]);
            }
        }

        return sb.ToString();
    }

    private static void PopulateFunctions(StandardEvaluationContext testContext)
    {
        try
        {
            testContext.RegisterFunction("IsEven", typeof(TestScenarioCreator).GetMethod("IsEven", new[] { typeof(int) }));
            testContext.RegisterFunction("ReverseInt", typeof(TestScenarioCreator).GetMethod("ReverseInt", new[] { typeof(int), typeof(int), typeof(int) }));
            testContext.RegisterFunction("ReverseString", typeof(TestScenarioCreator).GetMethod("ReverseString", new[] { typeof(string) }));
            testContext.RegisterFunction("VarargsFunctionReverseStringsAndMerge", typeof(TestScenarioCreator).GetMethod("VarargsFunctionReverseStringsAndMerge", new[] { typeof(string[]) }));
            testContext.RegisterFunction("VarargsFunctionReverseStringsAndMerge2", typeof(TestScenarioCreator).GetMethod("VarargsFunctionReverseStringsAndMerge2", new[] { typeof(int), typeof(string[]) }));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Populate failed", ex);
        }
    }

    private static void PopulateVariables(StandardEvaluationContext testContext)
    {
        testContext.SetVariable("answer", 42);
    }

    private static void SetupRootContextObject(StandardEvaluationContext testContext)
    {
        var c = new GregorianCalendar();
        var tesla = new Inventor("Nikola Tesla", c.ToDateTime(1856, 7, 9, 0, 0, 0, Calendar.CurrentEra), "Serbian")
        {
            PlaceOfBirth = new PlaceOfBirth("SmilJan"),
            Inventions = new[]
            {
                "Telephone repeater", "Rotating magnetic field principle",
                "Polyphase alternating-current system", "Induction motor", "Alternating-current power transmission",
                "Tesla coil transformer", "Wireless communication", "Radio", "Fluorescent lights"
            }
        };
        testContext.SetRootObject(tesla);
    }
}
