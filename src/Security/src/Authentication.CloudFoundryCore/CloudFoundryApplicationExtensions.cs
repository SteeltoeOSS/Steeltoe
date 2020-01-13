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

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.MtlsCore;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryApplicationExtensions
    {
        public static IApplicationBuilder UseCloudFoundryContainerIdentity(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseCertificateRotation();
            return app.UseMiddleware<CertificateForwarderMiddleware>();
        }
    }
}