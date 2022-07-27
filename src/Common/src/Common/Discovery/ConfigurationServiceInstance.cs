// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Discovery;

public class ConfigurationServiceInstance : IServiceInstance
{
    public string ServiceId { get; set; }

    public string Host { get; set; }

    public int Port { get; set; }

    public bool IsSecure { get; set; }

    public Uri Uri => new ((IsSecure ? Uri.UriSchemeHttps : Uri.UriSchemeHttp) + Uri.SchemeDelimiter + Host + ':' + Port);

    public IDictionary<string, string> Metadata { get; set; }
}