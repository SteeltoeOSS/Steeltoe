﻿// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Security.Authentication.MtlsCore
{
    public class CertificateForwarderOptions
    {
        /// <summary>
        /// The header name containing the Base64 encoded client certificate.
        /// </summary>
        /// <remarks>
        /// This defaults to X-ARR-ClientCert, which is the header Azure Web Apps uses.
        /// </remarks>
        public string CertificateHeader { get; set; } = "X-ARR-ClientCert";

        /// <summary>
        /// The function used to convert the header to an instance of <see cref="X509Certificate2"/>.
        /// </summary>
        /// <remarks>
        /// This defaults to a conversion from a base64 converted string.
        /// </remarks>
        public Func<string, X509Certificate2> HeaderConverter = (headerValue) => new X509Certificate2(Convert.FromBase64String(headerValue));
    }
}
