// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public sealed class HomeController : Controller
{
    private readonly ConfigServerDataAsOptions _options;
    private readonly IHealthContributor _health;

    public HomeController(IOptions<ConfigServerDataAsOptions> options, IHealthContributor health)
    {
        _options = options.Value;
        _health = health;
    }

    [HttpGet]
    public string VerifyAsInjectedOptions()
    {
        if (_options != null)
        {
            return _options.Bar + _options.Foo + _options.Info?.Description + _options.Info?.Url;
        }

        return string.Empty;
    }

    [HttpGet]
    public string Health()
    {
        if (_health != null)
        {
            HealthCheckResult health = _health.Health();
            health.Details.TryGetValue("propertySources", out object sourceList);

            object nameList = ToCsv(sourceList as IList<string>);
            return $"{health.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps)},{nameList}";
        }

        return string.Empty;
    }

    private object ToCsv(IList<string> list)
    {
        string result = string.Empty;

        foreach (string name in list)
        {
            result += $"{name},";
        }

        return result;
    }
}
