// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Support.Test;

public class NativeMessageHeaderAccessorTest
{
    [Fact]
    public void CreateFromNativeHeaderMap()
    {
        var inputNativeHeaders = new Dictionary<string, List<string>>
        {
            { "foo", new List<string> { "bar" } },
            { "bar", new List<string> { "baz" } }
        };

        var headerAccessor = new NativeMessageHeaderAccessor(inputNativeHeaders);
        var actual = headerAccessor.ToDictionary();

        Assert.Single(actual);
        Assert.NotNull(actual[NativeMessageHeaderAccessor.NativeHeaders]);
        Assert.Equal(inputNativeHeaders, actual[NativeMessageHeaderAccessor.NativeHeaders]);
        Assert.NotSame(inputNativeHeaders, actual[NativeMessageHeaderAccessor.NativeHeaders]);
    }

    [Fact]
    public void CreateFromMessage()
    {
        var inputNativeHeaders = new Dictionary<string, List<string>>
        {
            { "foo", new List<string> { "bar" } },
            { "bar", new List<string> { "baz" } }
        };

        var inputHeaders = new Dictionary<string, object>
        {
            { "a", "b" },
            { NativeMessageHeaderAccessor.NativeHeaders, inputNativeHeaders }
        };

        var message = Message.Create("p", inputHeaders);
        var headerAccessor = new NativeMessageHeaderAccessor(message);
        var actual = headerAccessor.ToDictionary();

        Assert.Equal(2, actual.Count);
        Assert.Equal("b", actual["a"]);
        Assert.NotNull(actual[NativeMessageHeaderAccessor.NativeHeaders]);
        Assert.Equal(inputNativeHeaders, actual[NativeMessageHeaderAccessor.NativeHeaders]);
        Assert.NotSame(inputNativeHeaders, actual[NativeMessageHeaderAccessor.NativeHeaders]);
    }

    [Fact]
    public void CreateFromMessageNull()
    {
        var headerAccessor = new NativeMessageHeaderAccessor((IMessage)null);

        var actual = headerAccessor.ToDictionary();
        Assert.Empty(actual);

        var actualNativeHeaders = headerAccessor.ToNativeHeaderDictionary();
        Assert.Empty(actualNativeHeaders);
    }

    [Fact]
    public void CreateFromMessageAndModify()
    {
        var inputNativeHeaders = new Dictionary<string, List<string>>
        {
            { "foo", new List<string> { "bar" } },
            { "bar", new List<string> { "baz" } }
        };

        var nativeHeaders = new Dictionary<string, object>
        {
            { "a", "b" },
            { NativeMessageHeaderAccessor.NativeHeaders, inputNativeHeaders }
        };

        var message = Message.Create("p", nativeHeaders);

        var headerAccessor = new NativeMessageHeaderAccessor(message);
        headerAccessor.SetHeader("a", "B");
        headerAccessor.SetNativeHeader("foo", "BAR");

        var actual = headerAccessor.ToDictionary();

        Assert.Equal(2, actual.Count);
        Assert.Equal("B", actual["a"]);

        var actualNativeHeaders =
            (IDictionary<string, List<string>>)actual[NativeMessageHeaderAccessor.NativeHeaders];

        Assert.NotNull(actualNativeHeaders);
        Assert.Equal(new List<string> { "BAR" }, actualNativeHeaders["foo"]);
        Assert.Equal(new List<string> { "baz" }, actualNativeHeaders["bar"]);
    }

    [Fact]
    public void SetNativeHeader()
    {
        var nativeHeaders = new Dictionary<string, List<string>>
        {
            { "foo", new List<string> { "bar" } }
        };

        var headers = new NativeMessageHeaderAccessor(nativeHeaders);
        headers.SetNativeHeader("foo", "baz");

        Assert.Equal(new List<string> { "baz" }, headers.GetNativeHeader("foo"));
    }

    [Fact]
    public void SetNativeHeaderNullValue()
    {
        var nativeHeaders = new Dictionary<string, List<string>>
        {
            { "foo", new List<string> { "bar" } }
        };

        var headers = new NativeMessageHeaderAccessor(nativeHeaders);
        headers.SetNativeHeader("foo", null);

        Assert.Null(headers.GetNativeHeader("foo"));
    }

    [Fact]
    public void SetNativeHeaderLazyInit()
    {
        var headerAccessor = new NativeMessageHeaderAccessor();
        headerAccessor.SetNativeHeader("foo", "baz");

        Assert.Equal(new List<string> { "baz" }, headerAccessor.GetNativeHeader("foo"));
    }

    [Fact]
    public void SetNativeHeaderLazyInitNullValue()
    {
        var headerAccessor = new NativeMessageHeaderAccessor();
        headerAccessor.SetNativeHeader("foo", null);

        Assert.Null(headerAccessor.GetNativeHeader("foo"));
        Assert.Null(headerAccessor.MessageHeaders[NativeMessageHeaderAccessor.NativeHeaders]);
    }

    [Fact]
    public void SetNativeHeaderImmutable()
    {
        var headerAccessor = new NativeMessageHeaderAccessor();
        headerAccessor.SetNativeHeader("foo", "bar");
        headerAccessor.SetImmutable();
        var ex = Assert.Throws<InvalidOperationException>(() => headerAccessor.SetNativeHeader("foo", "baz"));
        Assert.Contains("Already immutable", ex.Message);
    }

    [Fact]
    public void AddNativeHeader()
    {
        var nativeHeaders = new Dictionary<string, List<string>>
        {
            { "foo", new List<string> { "bar" } }
        };

        var headers = new NativeMessageHeaderAccessor(nativeHeaders);
        headers.AddNativeHeader("foo", "baz");

        Assert.Equal(new List<string> { "bar", "baz" }, headers.GetNativeHeader("foo"));
    }

    [Fact]
    public void AddNativeHeaderNullValue()
    {
        var nativeHeaders = new Dictionary<string, List<string>>
        {
            { "foo", new List<string> { "bar" } }
        };

        var headers = new NativeMessageHeaderAccessor(nativeHeaders);
        headers.AddNativeHeader("foo", null);

        Assert.Equal(new List<string> { "bar" }, headers.GetNativeHeader("foo"));
    }

    [Fact]
    public void AddNativeHeaderLazyInit()
    {
        var headerAccessor = new NativeMessageHeaderAccessor();
        headerAccessor.AddNativeHeader("foo", "bar");

        Assert.Equal(new List<string> { "bar" }, headerAccessor.GetNativeHeader("foo"));
    }

    [Fact]
    public void AddNativeHeaderLazyInitNullValue()
    {
        var headerAccessor = new NativeMessageHeaderAccessor();
        headerAccessor.AddNativeHeader("foo", null);

        Assert.Null(headerAccessor.GetNativeHeader("foo"));
        Assert.Null(headerAccessor.MessageHeaders[NativeMessageHeaderAccessor.NativeHeaders]);
    }

    [Fact]
    public void AddNativeHeaderImmutable()
    {
        var headerAccessor = new NativeMessageHeaderAccessor();
        headerAccessor.AddNativeHeader("foo", "bar");
        headerAccessor.SetImmutable();
        var ex = Assert.Throws<InvalidOperationException>(() => headerAccessor.AddNativeHeader("foo", "baz"));
        Assert.Contains("Already immutable", ex.Message);
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void SetImmutableIdempotent()
#pragma warning restore S2699 // Tests should include assertions
    {
        var headerAccessor = new NativeMessageHeaderAccessor();
        headerAccessor.AddNativeHeader("foo", "bar");
        headerAccessor.SetImmutable();
        headerAccessor.SetImmutable();
    }
}
