// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorMediaTypes
{
    public static readonly string V1_JSON = "application/vnd.spring-boot.actuator.v1+json";

    public static readonly string V2_JSON = "application/vnd.spring-boot.actuator.v2+json";

    public static readonly string APP_JSON = "application/json";

    public static readonly string ANY = "*/*";

    public static string GetContentHeaders(List<string> acceptHeaders, MediaTypeVersion version = MediaTypeVersion.V2)
    {
        var contentHeader = APP_JSON;
        var versionContentHeader = GetContentTypeHeaderForVersion(version);

        if (acceptHeaders != null && acceptHeaders.Any(x => x == ANY || x == versionContentHeader))
        {
            contentHeader = versionContentHeader;
        }

        return contentHeader += ";charset=UTF-8";
    }

    private static string GetContentTypeHeaderForVersion(MediaTypeVersion version = MediaTypeVersion.V2)
    {
        return version == MediaTypeVersion.V2 ? V2_JSON : V1_JSON;
    }
}
