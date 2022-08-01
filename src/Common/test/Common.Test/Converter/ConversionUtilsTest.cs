// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using Xunit;

namespace Steeltoe.Common.Converter.Test;

public class ConversionUtilsTest
{
    [Fact]
    public void TestCanCreateCompatListFrom()
    {
        Assert.False(ConversionUtils.CanCreateCompatListFor(null));
        Assert.False(ConversionUtils.CanCreateCompatListFor(typeof(IList<>)));
        Assert.True(ConversionUtils.CanCreateCompatListFor(typeof(IList<string>)));
        Assert.True(ConversionUtils.CanCreateCompatListFor(typeof(List<string>)));
        Assert.False(ConversionUtils.CanCreateCompatListFor(typeof(List<>)));
        Assert.False(ConversionUtils.CanCreateCompatListFor(typeof(LinkedList<int>)));
        Assert.True(ConversionUtils.CanCreateCompatListFor(typeof(Collection<int>)));
        Assert.True(ConversionUtils.CanCreateCompatListFor(typeof(ArrayList)));
    }

    [Fact]
    public void TestCreateList()
    {
        Assert.Null(ConversionUtils.CreateCompatListFor(null));
        Assert.Null(ConversionUtils.CreateCompatListFor(typeof(IList<>)));
        Assert.IsType<List<string>>(ConversionUtils.CreateCompatListFor(typeof(IList<string>)));
        Assert.IsType<List<string>>(ConversionUtils.CreateCompatListFor(typeof(List<string>)));
        Assert.Null(ConversionUtils.CreateCompatListFor(typeof(List<>)));
        Assert.Null(ConversionUtils.CreateCompatListFor(typeof(LinkedList<int>)));
        Assert.IsType<Collection<int>>(ConversionUtils.CreateCompatListFor(typeof(Collection<int>)));
        Assert.IsType<ArrayList>(ConversionUtils.CreateCompatListFor(typeof(ArrayList)));
        Assert.IsType<ArrayList>(ConversionUtils.CreateCompatListFor(typeof(IList)));
    }

    [Fact]
    public void TestMakeGenericListType()
    {
        Assert.Equal(typeof(List<string>), ConversionUtils.MakeGenericListType(typeof(IList<string>)));
    }
}
