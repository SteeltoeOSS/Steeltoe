// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Environment;

public sealed class EnvironmentEndpointOptions : EndpointOptions
{
    private Sanitizer? _sanitizer;

    /// <summary>
    /// Gets the list of keys to sanitize. A key can be a simple string that the property must end with, or a regular expression. A case-insensitive match is
    /// always performed. Use a single-element empty string to disable sanitization. Default value:
    /// <code><![CDATA[
    /// [ "password", "secret", "key", "token", ".*credentials.*", "vcap_services" ]
    /// ]]></code>
    /// </summary>
    public IList<string> KeysToSanitize { get; } = new List<string>();

    internal Sanitizer GetSanitizer()
    {
        _sanitizer ??= new Sanitizer(KeysToSanitize);
        return _sanitizer;
    }
}
