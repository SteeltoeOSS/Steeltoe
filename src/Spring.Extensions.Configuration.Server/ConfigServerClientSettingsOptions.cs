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
    public class ConfigServerClientSettingsOptions : Common.ConfigServerClientSettingsOptionsBase
    {
        public override bool ValidateCertificates
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.Validate_Certificates,
                    ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION);
            }
        }
        public override bool Enabled
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.Enabled,
                    ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED);
            }
        }
        public override bool FailFast
        {
            get
            {
                return GetBoolean(Spring?.Cloud?.Config?.FailFast,
                    ConfigServerClientSettings.DEFAULT_FAILFAST);
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

                return settings;
            }
        }
    }
}
