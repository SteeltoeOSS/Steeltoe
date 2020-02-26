// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Security;
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