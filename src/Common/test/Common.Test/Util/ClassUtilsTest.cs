// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Common.Util.Test;

public class ClassUtilsTest
{
    [Fact]
    public void TestIsAssignable_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ClassUtils.IsAssignable(null, null));
        Assert.Throws<ArgumentNullException>(() => ClassUtils.IsAssignable(typeof(string), null));
    }

    [Fact]
    public void TestIsAssignableValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ClassUtils.IsAssignableValue(null, null));
    }

    [Fact]
    public void TestIsAssignableValue()
    {
        Assert.True(ClassUtils.IsAssignableValue(typeof(string), null));
        Assert.True(ClassUtils.IsAssignableValue(typeof(string), string.Empty));
        Assert.True(ClassUtils.IsAssignableValue(typeof(object), string.Empty));
    }

    [Fact]
    public void TestIsAssignable()
    {
        Assert.True(ClassUtils.IsAssignable(typeof(object), typeof(object)));
        Assert.True(ClassUtils.IsAssignable(typeof(string), typeof(string)));
        Assert.True(ClassUtils.IsAssignable(typeof(object), typeof(string)));
        Assert.True(ClassUtils.IsAssignable(typeof(object), typeof(int)));
        Assert.True(ClassUtils.IsAssignable(typeof(int), typeof(int)));
        Assert.True(ClassUtils.IsAssignable(typeof(int), typeof(int)));
        Assert.False(ClassUtils.IsAssignable(typeof(string), typeof(object)));
        Assert.False(ClassUtils.IsAssignable(typeof(int), typeof(double)));
        Assert.False(ClassUtils.IsAssignable(typeof(double), typeof(int)));
    }
}
