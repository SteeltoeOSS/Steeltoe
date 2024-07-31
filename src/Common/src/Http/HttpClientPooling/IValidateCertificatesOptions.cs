// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Http.HttpClientPooling;

/// <summary>
/// Indicates that an options type provides a boolean to turn off certificate validation.
/// </summary>
public interface IValidateCertificatesOptions
{
    bool ValidateCertificates { get; set; }
}
