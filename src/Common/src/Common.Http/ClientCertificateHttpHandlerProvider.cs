﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Http
{
    [Obsolete("Use CertificateProvidingHandler instead")]
    public class ClientCertificateHttpHandlerProvider : IHttpClientHandlerProvider
    {
        private readonly ClientCertificateHttpHandler _handler;

        public ClientCertificateHttpHandlerProvider(IOptionsMonitor<CertificateOptions> certOptions)
        {
            _handler = new ClientCertificateHttpHandler(certOptions);
        }

        public HttpClientHandler GetHttpClientHandler() => _handler;
    }
}
