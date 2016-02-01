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

namespace Spring.Extensions.Configuration.Common
{
 
    public class ConfigServerClientSettingsBase
    {

        /// <summary>
        /// The Config Server address
        /// </summary>
        public virtual string Uri { get; set; }

        /// <summary>
        /// Enables/Disables the Config Server provider
        /// </summary>
        public virtual bool Enabled { get; set; }

        /// <summary>
        /// The environment used when accessing configuration data 
        /// </summary>
        public virtual string Environment { get; set; } 

        /// <summary>
        /// The application name used when accessing configuration data 
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// The label used when accessing configuration data 
        /// </summary>
        public virtual string Label { get; set; }

        /// <summary>
        /// The username used when accessing the Config Server 
        /// </summary>
        public virtual string Username { get; set; }

        /// <summary>
        /// The password used when accessing the Config Server
        /// </summary>
        public virtual string Password { get; set; }

        /// <summary>
        /// Enables/Disables failfast behavior
        /// </summary>
        public virtual bool FailFast { get; set; }

        /// <summary>
        /// Enables/Disables whether provider validates server certificates
        /// </summary>
        public virtual bool ValidateCertificates { get; set; }

        /// <summary>
        /// Initialize Config Server client settings 
        /// </summary>
        internal protected ConfigServerClientSettingsBase() 
        {

        }

    }
}

