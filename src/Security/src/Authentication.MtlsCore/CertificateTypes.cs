// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Steeltoe.Security.Authentication.MtlsCore
{
    [Flags]
    public enum CertificateTypes
    {
        All = 0,
        Chained = 1,
        SelfSigned = 2
    }
}
