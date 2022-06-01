// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.ConfigServer.ITest;

public class HomeController : Controller
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
        else
        {
            return string.Empty;
        }
    }

    [HttpGet]
    public string Health()
    {
        if (_health != null)
        {
            var health = _health.Health();
            health.Details.TryGetValue("propertySources", out var sourcelist);

            var nameList = ToCSV(sourcelist as IList<string>);
            return $"{health.Status},{nameList}";
        }
        else
        {
            return string.Empty;
        }
    }

    private object ToCSV(IList<string> list)
    {
        var result = string.Empty;
        foreach (var name in list)
        {
            result += $"{name},";
        }

        return result;
    }
}
