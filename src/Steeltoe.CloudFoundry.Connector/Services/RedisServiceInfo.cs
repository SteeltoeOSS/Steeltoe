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


namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class RedisServiceInfo : UriServiceInfo
    {
        public const string REDIS_SCHEME = "redis";

        public RedisServiceInfo(String id, String host, int port, String password) :
                 base(id, REDIS_SCHEME, host, port, null, password, null)
        {
        }

        public RedisServiceInfo(String id, String uri) :
                    base(id, uri)
        {
        }
    }
}

