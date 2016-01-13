//
// Copyright 2015 the original author or authors.
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.OptionsModel;

namespace Spring.Extensions.Configuration.Server.IntegrationTest
{
 
    public class HomeController : Controller
    {
        private ConfigServerDataAsOptions _options;
        public HomeController(IOptions<ConfigServerDataAsOptions> options)
        {
            _options = options.Value;
        }
        [HttpGet]
        public string VerifyAsInjectedOptions()
        {
            if (_options != null)
                return _options.Bar + _options.Foo + _options.Info.Description + _options.Info.Url;
            else
                return "";
        }

    }
}
