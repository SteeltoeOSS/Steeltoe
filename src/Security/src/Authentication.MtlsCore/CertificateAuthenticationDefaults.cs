﻿// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Steeltoe.Security.Authentication.MtlsCore
{
    /// <summary>
    /// Default values related to certificate authentication middleware
    /// </summary>
    public static class CertificateAuthenticationDefaults
    {
        /// <summary>
        /// The default value used for CertificateAuthenticationOptions.AuthenticationScheme
        /// </summary>
        public const string AuthenticationScheme = "Certificate";

        /// <summary>
        /// The name used for the items dictionary on AuthenticateResult
        /// </summary>
        public const string CertificateItemsKey = AuthenticationScheme;
    }
}
