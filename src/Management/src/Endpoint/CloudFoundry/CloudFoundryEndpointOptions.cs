// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Http.HttpClientPooling;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public sealed class CloudFoundryEndpointOptions : EndpointOptions, IValidateCertificatesOptions
{
    public bool ValidateCertificates { get; set; } = true;
    public string? ApplicationId { get; set; }
    public string? CloudFoundryApi { get; set; }
}
