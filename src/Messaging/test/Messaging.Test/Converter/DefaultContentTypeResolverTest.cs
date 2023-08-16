// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Xunit;

namespace Steeltoe.Messaging.Test.Converter;

public sealed class DefaultContentTypeResolverTest
{
    [Fact]
    public void Resolve()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.ContentType, MimeTypeUtils.ApplicationJson }
        };

        var headers = new MessageHeaders(map);
        var resolver = new DefaultContentTypeResolver();
        Assert.Equal(MimeTypeUtils.ApplicationJson, resolver.Resolve(headers));
    }

    [Fact]
    public void ResolveStringContentType()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.ContentType, MimeTypeUtils.ApplicationJsonValue }
        };

        var headers = new MessageHeaders(map);
        var resolver = new DefaultContentTypeResolver();
        Assert.Equal(MimeTypeUtils.ApplicationJson, resolver.Resolve(headers));
    }

    [Fact]
    public void ResolveInvalidStringContentType()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.ContentType, "invalidContentType" }
        };

        var headers = new MessageHeaders(map);
        var resolver = new DefaultContentTypeResolver();
        Assert.Throws<ArgumentException>(() => resolver.Resolve(headers));
    }

    [Fact]
    public void ResolveUnknownHeaderType()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.ContentType, 1 }
        };

        var headers = new MessageHeaders(map);
        var resolver = new DefaultContentTypeResolver();
        Assert.Throws<ArgumentException>(() => resolver.Resolve(headers));
    }

    [Fact]
    public void ResolveNoContentTypeHeader()
    {
        var headers = new MessageHeaders(new Dictionary<string, object>());
        var resolver = new DefaultContentTypeResolver();
        Assert.Null(resolver.Resolve(headers));
    }

    [Fact]
    public void ResolveDefaultMimeType()
    {
        var resolver = new DefaultContentTypeResolver
        {
            DefaultMimeType = MimeTypeUtils.ApplicationJson
        };

        var headers = new MessageHeaders(new Dictionary<string, object>());

        Assert.Equal(MimeTypeUtils.ApplicationJson, resolver.Resolve(headers));
    }
}
