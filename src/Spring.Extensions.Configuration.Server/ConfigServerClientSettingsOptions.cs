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

namespace Spring.Extensions.Configuration.Server
{
    public class ConfigServerClientSettingsOptions
    {
        public bool ValidateCertificates
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.Validate_Certificates,
                    ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION);
            }
        }
        public bool Enabled
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.Enabled,
                    ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED);
            }
        }
        public bool FailFast
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.FailFast,
                    ConfigServerClientSettings.DEFAULT_FAILFAST);
            }
        }
        public string Environment
        {
            get
            {
                return Spring?.Cloud?.Config?.Env;
            }
        }
        public string Label
        {
            get
            {
                return Spring?.Cloud?.Config?.Label;
            }

        }
        public string Name
        {
            get
            {
                return Spring?.Cloud?.Config?.Name;
            }

        }
        public string Password
        {
            get
            {
                return Spring?.Cloud?.Config?.Password;
            }

        }
        public string Uri
        {
            get
            {
                return Spring?.Cloud?.Config?.Uri;
            }

        }
        public string Username
        {
            get
            {
                return Spring?.Cloud?.Config?.Username;
            }

        }
        public string AccessTokenUri
        {
            get
            {
                return Spring?.Cloud?.Config?.Access_Token_Uri;
            }

        }
        public string ClientSecret
        {
            get
            {
                return Spring?.Cloud?.Config?.Client_Secret;
            }

        }
        public string ClientId
        {
            get
            {
                return Spring?.Cloud?.Config?.Client_Id;
            }

        }

        public ConfigServerClientSettings Settings
        {
            get
            {
                ConfigServerClientSettings settings = new ConfigServerClientSettings();
                settings.Enabled = GetBoolean(Spring?.Cloud?.Config?.Enabled, 
                    ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED);
                settings.FailFast = GetBoolean(Spring?.Cloud?.Config?.FailFast,
                    ConfigServerClientSettings.DEFAULT_FAILFAST);
                settings.ValidateCertificates = GetBoolean(Spring?.Cloud?.Config?.Validate_Certificates,
                 ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION);

                settings.Environment = Spring?.Cloud?.Config?.Env;
                settings.Label = Spring?.Cloud?.Config?.Label;
                settings.Name = Spring?.Cloud?.Config?.Name;
                settings.Password = Spring?.Cloud?.Config?.Password;
                settings.Uri = Spring?.Cloud?.Config?.Uri;
                settings.Username = Spring?.Cloud?.Config?.Username;
                settings.AccessTokenUri = Spring?.Cloud?.Config?.Access_Token_Uri;
                settings.ClientSecret = Spring?.Cloud?.Config?.Client_Secret;
                settings.ClientId = Spring?.Cloud?.Config?.Client_Id;

                return settings;
            }
        }
        public Spring Spring { get; set; }

        private bool GetBoolean(string strValue, bool def)
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
        public string Access_Token_Uri { get; set; }
        public string Client_Secret { get; set; }
        public string Client_Id { get; set; }
        public string Validate_Certificates { get; set; }

    }
}
