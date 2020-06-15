// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using Steeltoe.Security.Authentication.Mtls;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class MutualTlsAuthenticationOptionsPostConfigurer : IPostConfigureOptions<MutualTlsAuthenticationOptions>
    {
        private readonly IOptionsMonitor<CertificateOptions> _containerIdentityOptions;

        public MutualTlsAuthenticationOptionsPostConfigurer(IOptionsMonitor<CertificateOptions> containerIdentityOptions)
        {
            _containerIdentityOptions = containerIdentityOptions;
        }

        public void PostConfigure(string name, MutualTlsAuthenticationOptions options)
        {
            options.IssuerChain = _containerIdentityOptions.CurrentValue.IssuerChain;
        }
    }
}