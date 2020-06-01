// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.DiaSymReader;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class MetaDataImportProvider : IMetadataImportProvider
    {
        private readonly object _import;

        public MetaDataImportProvider(object import)
        {
            _import = import;
        }

        public object GetMetadataImport()
        {
            return _import;
        }
    }
}
