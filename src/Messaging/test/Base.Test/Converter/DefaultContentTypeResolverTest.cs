// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Messaging.Converter.Test
{
    public class DefaultContentTypeResolverTest
    {
        [Fact]
        public void Resolve()
        {
            var map = new Dictionary<string, object>();
            map.Add(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_JSON);
            var headers = new MessageHeaders(map);
            var resolver = new DefaultContentTypeResolver();
            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, resolver.Resolve(headers));
        }

        [Fact]
        public void ResolvestringContentType()
        {
            var map = new Dictionary<string, object>();
            map.Add(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.APPLICATION_JSON_VALUE);
            var headers = new MessageHeaders(map);
            var resolver = new DefaultContentTypeResolver();
            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, resolver.Resolve(headers));
        }

        [Fact]
        public void ResolveInvalidstringContentType()
        {
            var map = new Dictionary<string, object>();
            map.Add(MessageHeaders.CONTENT_TYPE, "invalidContentType");
            var headers = new MessageHeaders(map);
            var resolver = new DefaultContentTypeResolver();
            Assert.Throws<ArgumentException>(() => resolver.Resolve(headers));
        }

        [Fact]
        public void ResolveUnknownHeaderType()
        {
            var map = new Dictionary<string, object>();
            map.Add(MessageHeaders.CONTENT_TYPE, 1);
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
            var resolver = new DefaultContentTypeResolver();
            resolver.DefaultMimeType = MimeTypeUtils.APPLICATION_JSON;
            var headers = new MessageHeaders(new Dictionary<string, object>());

            Assert.Equal(MimeTypeUtils.APPLICATION_JSON, resolver.Resolve(headers));
        }
    }
}
