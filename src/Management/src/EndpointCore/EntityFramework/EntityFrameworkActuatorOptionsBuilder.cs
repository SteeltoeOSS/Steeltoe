// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.EndpointBase.DbMigrations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.EntityFramework
{
    public class EntityFrameworkActuatorOptionsBuilder
    {
        private readonly IConfiguration _config;
        private List<Type> _dbTypes = new List<Type>();
        private bool _autoDiscoverContexts;

        internal EntityFrameworkActuatorOptionsBuilder(IConfiguration config)
        {
            _config = config;
        }

        public EntityFrameworkActuatorOptionsBuilder AddDbContext<T>()
            where T : DbContext
        {
            _dbTypes.Add(typeof(T));
            return this;
        }

        public void AutoDiscoverContexts() => _autoDiscoverContexts = true;

        internal EntityFrameworkEndpointOptions Build()
        {
            var options = new EntityFrameworkEndpointOptions(_config);
            options.ContextTypes = _dbTypes.ToList();
            options.AutoDiscoverContexts = _autoDiscoverContexts;
            return options;
        }
    }
}