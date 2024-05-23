// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;

namespace Steeltoe.Configuration.ConfigServer.Integration.Test;

public sealed class HomeController : Controller
{
    private readonly ConfigServerDataAsOptions _options;
    private readonly IHealthContributor _healthContributor;

    public HomeController(IOptions<ConfigServerDataAsOptions> options, IHealthContributor healthContributor)
    {
        ArgumentGuard.NotNull(healthContributor);
        ArgumentGuard.NotNull(options);

        _options = options.Value;
        _healthContributor = healthContributor;
    }

    [HttpGet]
    public string VerifyAsInjectedOptions()
    {
        return _options.Bar + _options.Foo + _options.Info?.Description + _options.Info?.Url;
    }

    [HttpGet]
    public async Task<string> HealthAsync()
    {
        HealthCheckResult? health = await _healthContributor.CheckHealthAsync(HttpContext.RequestAborted);

        if (health != null && health.Details.TryGetValue("propertySources", out object? sourceObject) && sourceObject is IList<string> sourceList)
        {
            string names = ToCsv(sourceList);
            return $"{health.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps)},{names}";
        }

        return string.Empty;
    }

    private static string ToCsv(IEnumerable<string> list)
    {
        var builder = new StringBuilder();

        foreach (string name in list)
        {
            builder.Append($"{name},");
        }

        return builder.ToString();
    }
}
