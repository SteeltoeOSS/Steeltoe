// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Http;

namespace Steeltoe.Common.Http.Test;

public sealed class HeaderRedactionTest
{
    [Fact]
    public void HttpClientFactory_uses_HTTP_header_redaction_by_default()
    {
        // https://learn.microsoft.com/en-us/dotnet/core/compatibility/networking/9.0/redact-headers
        Version? assemblyVersion = typeof(HttpClientFactoryOptions).Assembly.GetName().Version;

        assemblyVersion.Should().NotBeNull();
        assemblyVersion.Major.Should().BeGreaterThanOrEqualTo(9);
    }
}
