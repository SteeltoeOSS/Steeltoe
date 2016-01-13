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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Spring.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryServicesOptions
    {
        public Vcap Vcap { get; set; }

        public IList<Service> Services
        {
            get
            {
                List<Service> results = new List<Service>();
                if (Vcap.Services != null)
                {
                    foreach(KeyValuePair<string, Service[]> kvp in Vcap.Services)
                    {
                        results.AddRange(kvp.Value);
                    }
                }

                return results;
            }
        }
    }

    public class Service
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string[] Tags { get; set;  }
        public string Plan { get; set; }
        public Dictionary<string, Credential> Credentials { get; set; }
    }

    [TypeConverter(typeof(CredentialConverter))]
    public class Credential : Dictionary<string, Credential> {

        private string _value;
        public Credential()
        {

        }
        public Credential(string value)
        {
            _value = value;
        }

        public string Value
        {
            get
            {
                return _value;
            }
        }
    }

    public class CredentialConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
                return new Credential((string)value);
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

    }
}
