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

using Steeltoe.Common.Security;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class ClientCertificatesFixture : IDisposable
    {
        public readonly Guid ServerOrgId = new Guid("a8fef16f-94c0-49e3-aa0b-ced7c3da6229");
        public readonly Guid ServerSpaceId = new Guid("122b942a-d7b9-4839-b26e-836654b9785f");
        public readonly LocalCertificateWriter CertificateWriter = new LocalCertificateWriter();

        public ClientCertificatesFixture()
        {
            CertificateWriter.CertificateFilenamePrefix = "OrgAndSpaceMatch";
            CertificateWriter.Write(ServerOrgId, ServerSpaceId);

            CertificateWriter.CertificateFilenamePrefix = "SpaceMatch";
            CertificateWriter.Write(Guid.NewGuid(), ServerSpaceId);

            CertificateWriter.CertificateFilenamePrefix = "OrgMatch";
            CertificateWriter.Write(ServerOrgId, Guid.NewGuid());
        }

        #region IDisposable Support
        private bool disposedValue = false;

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // delete certificates ?
                }

                disposedValue = true;
            }
        }
        #endregion
    }
}
