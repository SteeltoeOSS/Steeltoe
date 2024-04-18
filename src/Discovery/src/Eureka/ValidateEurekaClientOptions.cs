// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

internal sealed class ValidateEurekaClientOptions : IValidateOptions<EurekaClientOptions>
{
    public ValidateOptionsResult Validate(string? name, EurekaClientOptions options)
    {
        if (options is not { Enabled: true } or { ShouldRegisterWithEureka: false, ShouldFetchRegistry: false })
        {
            return ValidateOptionsResult.Success;
        }

        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(options.EurekaServerServiceUrls))
        {
            errors.Add("Eureka Service URL must be provided.");
        }
        else
        {
            string[] urls = options.EurekaServerServiceUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string url in urls)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                {
                    errors.Add($"Eureka URL '{url}' is invalid.");
                }
                else
                {
                    if (uri.Host == "localhost" && (Platform.IsContainerized || Platform.IsCloudHosted))
                    {
                        errors.Add($"Eureka URL '{url}' is not valid in containerized or cloud environments. " +
                            "Please configure Eureka:Client:ServiceUrl with a non-localhost address or add a service binding.");
                    }
                }
            }
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}
