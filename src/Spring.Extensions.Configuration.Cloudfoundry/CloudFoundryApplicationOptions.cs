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

namespace Spring.Extensions.Configuration.CloudFoundry
{

    public class CloudFoundryApplicationOptions 
    {
        public Vcap Vcap { get; set; }

        public string ApplicationId
        {
            get
            {
                return Vcap?.Application?.Application_Id;
            }
        }

        public string ApplicationName
        {
            get
            {
                return Vcap?.Application?.Application_Name;
            }
        }
        public string[] ApplicationUris
        {
            get
            {
                return Vcap?.Application?.Application_Uris;
            }
        }
        public string ApplicationVersion
        {
            get
            {
                return Vcap?.Application?.Application_Version;
            }
        }
        public string InstanceId
        {
            get
            {
                return Vcap?.Application?.Instance_Id;
            }
        }
        public int InstanceIndex
        {
            get
            {
                return GetInt(Vcap?.Application?.Instance_Index, -1);
            }
        }

        public string Name
        {
            get
            {
                return Vcap?.Application?.Name;
            }
        }
        public int Port
        {
            get
            {
                return GetInt(Vcap?.Application?.Port, -1);
            }
        }
        public string SpaceId
        {
            get
            {
                return Vcap?.Application?.Space_Id;
            }
        }
        public string SpaceName
        {
            get
            {
                return Vcap?.Application?.Space_Name;
            }
        }
        public string Start
        {
            get
            {
                return Vcap?.Application?.Start;
            }
        }
        public string[] Uris
        {
            get
            {
                return Vcap?.Application?.Uris;
            }
        }
        public string Version
        {
            get
            {
                return Vcap?.Application?.Version;
            }
        }
        public int DiskLimit
        {
            get
            {
                var limits = Vcap?.Application?.Limits;
                if ( limits == null)
                    return -1;
                return GetInt(limits.Disk, -1);
            }
        }
        public int MemoryLimit
        {
            get
            {
                var limits = Vcap?.Application?.Limits;
                if (limits == null)
                    return -1;
                return GetInt(limits.Mem, -1);
            }
        }
        public int FileDescriptorLimit
        {
            get
            {
                var limits = Vcap?.Application?.Limits;
                if (limits == null)
                    return -1;
                return GetInt(limits.Fds, -1);
            }
        }

        private int GetInt(string strValue, int def)
        {

            int result = def;
            if (!string.IsNullOrEmpty(strValue))
            {
                int.TryParse(strValue, out result);
            }
            return result;
        }
    }


    public class Vcap
    {
        public Application Application { get; set; }

        public Dictionary<string, Service[]> Services { get; set; }
    }
    public class Application
    {
        public string Application_Id { get; set; }
        public string Application_Name { get; set; }
        public string[] Application_Uris { get; set; }
        public string Application_Version { get; set; }
        public string Instance_Id { get; set; }
        public string Instance_Index { get; set; }
        public Limits Limits { get; set; }
        public string Name { get; set; }
        public string Port { get; set; }
        public string Space_Id { get; set; }
        public string Space_Name { get; set; }
        public string Start { get; set; }
        public string[] Uris { get; set; }
        public string Version { get; set; }
    }
    public class Limits
    {
        public string Disk { get; set; }
        public string Fds { get; set; }
        public string Mem { get; set; }
    }


}
