// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test;

public class DefaultContentTypeResolverTest
{
    [Fact]
    public void Resolve()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_JSON }
        };
        var headers = new MessageHeaders(map);
        var resolver = new DefaultContentTypeResolver();
        Assert.Equal(MimeTypeUtils.APPLICATION_JSON, resolver.Resolve(headers));
    }

    [Fact]
    public void ResolvestringContentType()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_JSON_VALUE }
        };
        var headers = new MessageHeaders(map);
        var resolver = new DefaultContentTypeResolver();
        Assert.Equal(MimeTypeUtils.APPLICATION_JSON, resolver.Resolve(headers));
    }

    [Fact]
    public void ResolveInvalidstringContentType()
    {
        var map = new Dictionary<string, object>
        {
            { MessageHeaders.CONTENT_TYPE, "invalidContentType" }
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
            { MessageHeaders.CONTENT_TYPE, 1 }
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
            DefaultMimeType = MimeTypeUtils.APPLICATION_JSON
        };
        var headers = new MessageHeaders(new Dictionary<string, object>());

        Assert.Equal(MimeTypeUtils.APPLICATION_JSON, resolver.Resolve(headers));
    }
}
