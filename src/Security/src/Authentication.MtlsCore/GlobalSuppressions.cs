// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via CertificateForwarderExtensions.", Scope = "type", Target = "~T:idunno.Authentication.Certificate.CertificateAuthenticationHandler")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Enables configuration of the function used to convert a header into a certificate.", Scope = "member", Target = "~F:idunno.Authentication.Certificate.CertificateForwarderOptions.HeaderConverter")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Standard signature for a certificate validator, cannot be changed.", Scope = "member", Target = "~M:idunno.Authentication.Certificate.CertificateValidator.DisableChannelValidation(System.Security.Cryptography.X509Certificates.X509Certificate2,System.Security.Cryptography.X509Certificates.X509Chain,System.Net.Security.SslPolicyErrors)~System.Boolean")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Do not directly await a Task", Justification = "Not applicable to .NET Core", Scope = "type", Target = "~T:idunno.Authentication.Certificate.CertificateAuthenticationHandler")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Do not directly await a Task", Justification = "Not applicable to .NET Core", Scope = "type", Target = "~T:idunno.Authentication.Certificate.CertificateForwarderMiddleware")]
