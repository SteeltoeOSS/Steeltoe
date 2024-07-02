// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Configuration.Encryption.Decryption;

[assembly: ConfigurationSchema("Encrypt", typeof(ConfigServerEncryptionSettings))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Configuration", "Steeltoe.Configuration.Encryption")]

[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Configuration.ConfigServer")]
[assembly: InternalsVisibleTo("Steeltoe.Configuration.ConfigServer.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Configuration.Encryption.Test")]
