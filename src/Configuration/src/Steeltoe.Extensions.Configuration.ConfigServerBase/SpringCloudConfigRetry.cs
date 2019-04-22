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

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    public class SpringCloudConfigRetry
    {
        public bool Enabled { get; set; } = ConfigServerClientSettings.DEFAULT_RETRY_ENABLED;

        public int InitialInterval { get; set; } = ConfigServerClientSettings.DEFAULT_INITIAL_RETRY_INTERVAL;

        public int MaxInterval { get; set; } = ConfigServerClientSettings.DEFAULT_MAX_RETRY_INTERVAL;

        public double Multiplier { get; set; } = ConfigServerClientSettings.DEFAULT_RETRY_MULTIPLIER;

        public int MaxAttempts { get; set; } = ConfigServerClientSettings.DEFAULT_MAX_RETRY_ATTEMPTS;
    }
}
