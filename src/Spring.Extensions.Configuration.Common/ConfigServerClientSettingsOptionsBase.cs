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
    public class ConfigServerClientSettingsOptionsBase
    {
        public virtual bool ValidateCertificates
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.Validate_Certificates, false);
            }
        }
        public virtual bool Enabled
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.Enabled, false);
            }
        }
        public virtual bool FailFast
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.FailFast, false);
            }
        }
        public virtual string Environment
        {
            get
            {
                return Spring?.Cloud?.Config?.Env;
            }
        }
        public virtual string Label
        {
            get
            {
                return Spring?.Cloud?.Config?.Label;
            }

        }
        public virtual string Name
        {
            get
            {
                return Spring?.Cloud?.Config?.Name;
            }

        }
        public virtual string Password
        {
            get
            {
                return Spring?.Cloud?.Config?.Password;
            }

        }
        public virtual string Uri
        {
            get
            {
                return Spring?.Cloud?.Config?.Uri;
            }

        }
        public virtual string Username
        {
            get
            {
                return Spring?.Cloud?.Config?.Username;
            }

        }
        internal ConfigServerClientSettingsBase Settings
        {
            get
            {
                ConfigServerClientSettingsBase settings = new ConfigServerClientSettingsBase();
                settings.Enabled = GetBoolean(Spring?.Cloud?.Config?.Enabled, false);
                settings.FailFast = GetBoolean(Spring?.Cloud?.Config?.FailFast, false);
                settings.ValidateCertificates = GetBoolean(Spring?.Cloud?.Config?.Validate_Certificates, false);

                settings.Environment = Spring?.Cloud?.Config?.Env;
                settings.Label = Spring?.Cloud?.Config?.Label;
                settings.Name = Spring?.Cloud?.Config?.Name;
                settings.Password = Spring?.Cloud?.Config?.Password;
                settings.Uri = Spring?.Cloud?.Config?.Uri;
                settings.Username = Spring?.Cloud?.Config?.Username;

                return settings;
            }
        }

        public Spring Spring { get; set; }

        protected bool GetBoolean(string strValue, bool def)
        {

            bool result = def;
            if (!string.IsNullOrEmpty(strValue))
            {
                bool.TryParse(strValue, out result);
            }
            return result;
        }
    }

    public class Spring
    {
        public Cloud Cloud { get; set; }
    }
    public class Cloud
    {
        public Config Config { get; set; }
    }
    public class Config
    {
        public string Enabled { get; set; }
        public string FailFast { get; set; }
        public string Env { get; set; }
        public string Label { get; set; }
        public string Name { get; set;  }
        public string Password { get; set; }
        public string Uri { get; set; }
        public string Username { get; set; }
        public string Validate_Certificates { get; set; }

    }
}
