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

using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Http.Test
{
    internal class TestServiceInstance : IServiceInstance
    {
        private Uri _uri;

        public TestServiceInstance(Uri uri)
        {
            _uri = uri;
        }

        public string Host
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsSecure
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IDictionary<string, string> Metadata
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Port
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ServiceId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }
    }
}
