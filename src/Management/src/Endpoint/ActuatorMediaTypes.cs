// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint;

internal static class ActuatorMediaTypes
{
    public const string V1Json = "application/vnd.spring-boot.actuator.v1+json";
    public const string V2Json = "application/vnd.spring-boot.actuator.v2+json";
    public const string V3Json = "application/vnd.spring-boot.actuator.v3+json";
    public const string AppJson = "application/json";
    public const string Any = "*/*";

    internal static string GetContentHeaders(ICollection<string> acceptHeaders, MediaTypeVersion version)
    {
        ArgumentNullException.ThrowIfNull(acceptHeaders);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(acceptHeaders);

        string contentHeader = AppJson;
        string versionContentHeader = GetContentTypeHeaderForVersion(version);

        if (acceptHeaders.Any(header => header == Any || header == versionContentHeader))
        {
            contentHeader = versionContentHeader;
        }

        return $"{contentHeader};charset=UTF-8";
    }

    private static string GetContentTypeHeaderForVersion(MediaTypeVersion version = MediaTypeVersion.V2)
    {
        return version switch
        {
            MediaTypeVersion.V1 => V1Json,
            MediaTypeVersion.V2 => V2Json,
            MediaTypeVersion.V3 => V3Json,
            _ => V2Json
        };
    }
}
