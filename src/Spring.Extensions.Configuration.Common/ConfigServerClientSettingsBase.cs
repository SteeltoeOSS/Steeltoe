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
        public virtual string Username
        {
            get { return GetUserName(); } set { this.username = value; }
        }

        /// <summary>
        /// The password used when accessing the Config Server
        /// </summary>
        public virtual string Password
        {
            get { return GetPassword(); } set { this.password = value; }
        }

        /// <summary>
        /// Enables/Disables failfast behavior
        /// </summary>
        public virtual bool FailFast { get; set; }

        /// <summary>
        /// Enables/Disables whether provider validates server certificates
        /// </summary>
        public virtual bool ValidateCertificates { get; set; }

        public virtual string RawUri
        {
            get
            {
                return GetRawUri();
            }
        }

        /// <summary>
        /// Initialize Config Server client settings 
        /// </summary>
        internal protected ConfigServerClientSettingsBase() 
        {
        }

        internal string GetRawUri()
        {
            try
            {
                if (!string.IsNullOrEmpty(Uri))
                {
                    System.Uri uri = new System.Uri(Uri);
                    return uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
                }
            }
            catch (UriFormatException e)
            {
                // Log
  
            }
            return Uri;
        }
        internal string GetPassword()
        {
            if (!string.IsNullOrEmpty(password))
                return password;
            return GetUserPassElement(1);
        }
        internal string GetUserName() {
            if (!string.IsNullOrEmpty(username))
                return username;
            return GetUserPassElement(0);
        }

        private string GetUserInfo()
        {

            try
            {
                if (!string.IsNullOrEmpty(Uri))
                {
                    System.Uri uri = new System.Uri(Uri);
                    return uri.UserInfo;
                }
            }
            catch (UriFormatException e)
            {
                // Log
                throw;
            }
            return null;

        }
        private string GetUserPassElement(int index)
        {
            string result = null;
            string userInfo = GetUserInfo();
            if (!string.IsNullOrEmpty(userInfo))
            {
                string[] info = userInfo.Split(COLON_DELIMIT);
                if (info.Length > index)
                    result = info[index];
            }

            return result;

        }

        private string username;
        private string password;

        private static readonly char[] COLON_DELIMIT = new char[] { ':' };
    }
}

