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

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.ConfigServer.ITest
{
    public class HomeController : Controller
    {
        private ConfigServerDataAsOptions _options;
        private IHealthContributor _health;

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
                health.Details.TryGetValue("propertySources", out object sourcelist);

                var nameList = ToCSV(sourcelist as IList<string>);
                return health.Status.ToString() + "," + nameList;
            }
            else
            {
                return string.Empty;
            }
        }

        private object ToCSV(IList<string> list)
        {
            string result = string.Empty;
            foreach (var name in list)
            {
                result += name + ",";
            }

            return result;
        }
    }
}
