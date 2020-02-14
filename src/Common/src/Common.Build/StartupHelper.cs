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

using System;
using System.IO;

namespace Steeltoe.Common.Build
{
    public static class StartupHelper
    {
        public static void UseOrGeneratePlatformCertificates(string orgId = null, string spaceId = null)
        {
            if (!Platform.IsCloudFoundry)
            {
                Console.WriteLine("Not running on the platform... using local certs");

                var task = new CertificateWriter();
                task.Write(orgId, spaceId);

                Environment.SetEnvironmentVariable("CF_INSTANCE_CERT", Path.Combine(CertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceCert.pem"));
                Environment.SetEnvironmentVariable("CF_INSTANCE_KEY", Path.Combine(CertificateWriter.AppBasePath, "GeneratedCertificates", "SteeltoeInstanceKey.pem"));
            }
            else
            {
                Console.WriteLine("CF_INSTANCE_CERT: {0}", Environment.GetEnvironmentVariable("CF_INSTANCE_CERT"));
                Console.WriteLine("CF_INSTANCE_KEY: {0}", Environment.GetEnvironmentVariable("CF_INSTANCE_KEY"));
            }
        }
    }
}
