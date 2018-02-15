// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public enum CredentialType
    {
        Value,
        Password,
        User,
        JSON,
        Certificate,
        RSA,
        SSH
    }

    public enum CertificateKeyLength
    {
        Length_2048 = 2048,
        Length_3072 = 3072,
        Length_4096 = 4096
    }

#pragma warning disable SA1300 // ElementMustBeginWithUpperCaseLetter
    /// <summary>
    /// Overwrite mode for existing credentials (https://credhub-api.cfapps.io/#overwriting-credential-values)
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OverwiteMode
    {
        [EnumMember(Value = "no-overwrite")]
        noOverwrite,
        overwrite,
        converge
    }

    /// <summary>
    /// Uses for certificates
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum KeyUsage
    {
        digital_signature,
        non_repudiation,
        key_encipherment,
        data_encipherment,
        key_agreement,
        key_cert_sign,
        crl_sign,
        encipher_only,
        decipher_only
    }

    /// <summary>
    /// Extended key usage for certificates
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ExtendedKeyUsage
    {
        [Description("Client Auth")]
        client_auth,
        server_auth,
        code_signing,
        email_protection,
        timestamping
    }

    /// <summary>
    /// Operations that can be allowed for an actor
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OperationPermissions
    {
        read,
        write,
        delete,
        read_acl,
        write_acl
    }
#pragma warning restore SA1300 // ElementMustBeginWithUpperCaseLetter
}