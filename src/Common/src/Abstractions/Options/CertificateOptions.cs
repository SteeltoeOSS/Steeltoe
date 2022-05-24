// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Options
{
    /// <summary>
    /// Options for use with platform-provided certificates
    /// </summary>
    public class CertificateOptions
    {
        public string Name { get; set; }

        public X509Certificate2 Certificate { get; set; }

        public List<X509Certificate2> IssuerChain { get; set; } = new ();
    }
}
