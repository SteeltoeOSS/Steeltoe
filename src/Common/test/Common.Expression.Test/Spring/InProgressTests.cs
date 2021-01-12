// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class InProgressTests : AbstractExpressionTests
    {
        // [Fact]
        // public void TestRelOperatorsBetween01()
        // {
        //    Evaluate("1 between listOneFive", "true", typeof(bool));

        // // no inline list building at the moment
        //    // Evaluate("1 between {1, 5}", "true", typeof(bool));
        // }

        // [Fact]
        // public void TestRelOperatorsBetweenErrors01()
        // {
        //    EvaluateAndCheckError("1 between T(String)", SpelMessage.BETWEEN_RIGHT_OPERAND_MUST_BE_TWO_ELEMENT_LIST, 10);
        // }

        // [Fact]
        // public void TestRelOperatorsBetweenErrors03()
        // {
        //    EvaluateAndCheckError("1 between listOfNumbersUpToTen", SpelMessage.BETWEEN_RIGHT_OPERAND_MUST_BE_TWO_ELEMENT_LIST, 10);
        // }

        //// PROJECTION
        // [Fact]
        // public void TestProjection01()
        // {
        //    Evaluate("listOfNumbersUpToTen.![#this<5?'y':'n']", "[y,y,y,y,n,n,n,n,n,n]", typeof(List<object>));

        // // inline list creation not supported at the moment
        //    // Evaluate("{1,2,3,4,5,6,7,8,9,10}.!{#isEven(#this)}", "[n, y, n, y, n, y, n, y, n, y]", typeof(IList));
        // }

        // [Fact]
        // public void TestProjection02()
        // {
        //    // inline map creation not supported at the moment
        //    // Evaluate("#{'a':'y','b':'n','c':'y'}.![value=='y'?key:null].nonnull().sort()", "[a, c]", typeof(IList));
        //    Evaluate("mapOfNumbersUpToTen.![key>5?value:null]", "[null, null, null, null, null, six, seven, eight, nine, ten]", typeof(IList));
        // }

        // [Fact]
        // public void TestProjection05()
        // {
        //    EvaluateAndCheckError("'abc'.![true]", SpelMessage.PROJECTION_NOT_SUPPORTED_ON_TYPE);
        //    EvaluateAndCheckError("null.![true]", SpelMessage.PROJECTION_NOT_SUPPORTED_ON_TYPE);
        //    Evaluate("null?.![true]", null, null);
        // }

        // [Fact]
        // public void TestProjection06()
        // {
        //    SpelExpression expr = (SpelExpression)parser.ParseExpression("'abc'.![true]");
        //    Assert.Equal("'abc'.![true]", expr.ToStringAST());
        // }

        //// SELECTION
        // [Fact]
        // public void TestSelection02()
        // {
        //    Evaluate("TestMap.keySet().?[#this matches '.*o.*']", "[monday]", typeof(IList));
        //    Evaluate("TestMap.keySet().?[#this matches '.*r.*'].contains('saturday')", "true", typeof(bool));
        //    Evaluate("TestMap.keySet().?[#this matches '.*r.*'].size()", "3", typeof(int));
        // }

        // [Fact]
        // public void TestSelectionError_NonBooleanSelectionCriteria()
        // {
        //    EvaluateAndCheckError("listOfNumbersUpToTen.?['nonboolean']", SpelMessage.RESULT_OF_SELECTION_CRITERIA_IS_NOT_BOOLEAN);
        // }

        // [Fact]
        // public void TestSelection03()
        // {
        //    Evaluate("mapOfNumbersUpToTen.?[key>5].size()", "5", typeof(int));
        // }

        // [Fact]
        // public void TestSelection04()
        // {
        //    EvaluateAndCheckError("mapOfNumbersUpToTen.?['hello'].size()", SpelMessage.RESULT_OF_SELECTION_CRITERIA_IS_NOT_BOOLEAN);
        // }

        // [Fact]
        // public void TestSelection05()
        // {
        //    Evaluate("mapOfNumbersUpToTen.?[key>11].size()", "0", typeof(int));
        //    Evaluate("mapOfNumbersUpToTen.^[key>11]", null, null);
        //    Evaluate("mapOfNumbersUpToTen.$[key>11]", null, null);
        //    Evaluate("null?.$[key>11]", null, null);
        //    EvaluateAndCheckError("null.?[key>11]", SpelMessage.INVALID_TYPE_FOR_SELECTION);
        //    EvaluateAndCheckError("'abc'.?[key>11]", SpelMessage.INVALID_TYPE_FOR_SELECTION);
        // }

        // [Fact]
        // public void TestSelectionFirst01()
        // {
        //    Evaluate("listOfNumbersUpToTen.^[#isEven(#this) == 'y']", "2", typeof(int));
        // }

        // [Fact]
        // public void TestSelectionFirst02()
        // {
        //    Evaluate("mapOfNumbersUpToTen.^[key>5].size()", "1", typeof(int));
        // }

        // [Fact]
        // public void TestSelectionLast01()
        // {
        //    Evaluate("listOfNumbersUpToTen.$[#isEven(#this) == 'y']", "10", typeof(int));
        // }

        // [Fact]
        // public void TestSelectionLast02()
        // {
        //    Evaluate("mapOfNumbersUpToTen.$[key>5]", "{10=ten}", typeof(IDictionary));
        //    Evaluate("mapOfNumbersUpToTen.$[key>5].size()", "1", typeof(int));
        // }

        // [Fact]
        // public void TestSelectionAST()
        // {
        //    SpelExpression expr = (SpelExpression)parser.ParseExpression("'abc'.^[true]");
        //    Assert.Equal("'abc'.^[true]", expr.ToStringAST());
        //    expr = (SpelExpression)parser.ParseExpression("'abc'.?[true]");
        //    Assert.Equal("'abc'.?[true]", expr.ToStringAST());
        //    expr = (SpelExpression)parser.ParseExpression("'abc'.$[true]");
        //    Assert.Equal("'abc'.$[true]", expr.ToStringAST());
        // }

        //// Constructor invocation
        // [Fact]
        // public void TestSetConstruction01()
        // {
        //    Evaluate("new java.util.HashSet().addAll({'a','b','c'})", "true", typeof(bool));
        // }
    }
}
