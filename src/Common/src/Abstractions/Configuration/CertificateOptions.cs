// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Configuration;

/// <summary>
/// Options for use with platform-provided certificates.
/// </summary>
public sealed class CertificateOptions
{
    internal const string ConfigurationKeyPrefix = "Certificates";

    public X509Certificate2? Certificate { get; set; }

    public IList<X509Certificate2> IssuerChain { get; } = [];
}
