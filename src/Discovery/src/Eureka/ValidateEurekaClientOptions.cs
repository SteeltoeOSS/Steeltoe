// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

internal sealed partial class ValidateEurekaClientOptions : IValidateOptions<EurekaClientOptions>
{
    private readonly ILogger<ValidateEurekaClientOptions> _logger;

    public ValidateEurekaClientOptions(ILogger<ValidateEurekaClientOptions> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public ValidateOptionsResult Validate(string? name, EurekaClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || !uri.IsWellFormedOriginalString())
                {
                    errors.Add($"Eureka URL '{url}' is invalid.");
                }
                else
                {
                    if (uri.Host == "localhost" && (Platform.IsContainerized || Platform.IsCloudHosted))
                    {
                        LogLocalhostEurekaUrl(url);
                    }
                }
            }
        }

        return errors.Count > 0 ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning,
        Message = "Eureka URL '{Url}' is unlikely to be valid in containerized or cloud environments. " +
            "Please configure Eureka:Client:ServiceUrl with a non-localhost address or add a service binding.")]
    private partial void LogLocalhostEurekaUrl(string url);
}
