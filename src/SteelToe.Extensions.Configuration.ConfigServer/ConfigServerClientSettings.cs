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

namespace SteelToe.Extensions.Configuration.ConfigServer
{
    /// <summary>
    /// Holds the settings used to configure the Spring Cloud Config Server provider 
    /// <see cref="ConfigServerConfigurationProvider"/>.
    /// </summary>
    public class ConfigServerClientSettings 
    {
        /// <summary>
        /// Default Config Server address used by provider
        /// </summary>
        public const string DEFAULT_URI = "http://localhost:8888";

        /// <summary>
        /// Default enironment used when accessing configuration data
        /// </summary>
        public const string DEFAULT_ENVIRONMENT = "Production";

        /// <summary>
        /// Default fail fast setting
        /// </summary>
        public const bool DEFAULT_FAILFAST = false;

        /// <summary>
        /// Default Config Server provider enabled setting
        /// </summary>
        public const bool DEFAULT_PROVIDER_ENABLED = true;

        /// <summary>
        /// Default certifcate validation enabled setting
        /// </summary>
        public const bool DEFAULT_CERTIFICATE_VALIDATION = true;

        /// <summary>
        /// Initialize Config Server client settings with defaults
        /// </summary>
        public ConfigServerClientSettings() : base()
        {
            ValidateCertificates = DEFAULT_CERTIFICATE_VALIDATION;
            FailFast = DEFAULT_FAILFAST;
            Environment = DEFAULT_ENVIRONMENT;
            Enabled = DEFAULT_PROVIDER_ENABLED;
            Uri = DEFAULT_URI;
        }

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
            get { return GetUserName(); }
            set { this.username = value; }
        }

        /// <summary>
        /// The password used when accessing the Config Server
        /// </summary>
        public virtual string Password
        {
            get { return GetPassword(); }
            set { this.password = value; }
        }

        /// <summary>
        /// Enables/Disables failfast behavior
        /// </summary>
        public virtual bool FailFast { get; set; }

        /// <summary>
        /// Enables/Disables whether provider validates server certificates
        /// </summary>
        public virtual bool ValidateCertificates { get; set; }

        /// <summary>
        /// Returns the HttpRequestUrl, unescaped
        /// </summary>
        public virtual string RawUri
        {
            get
            {
                return GetRawUri();
            }
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
            catch (UriFormatException)
            {


            }
            return Uri;
        }
        internal string GetPassword()
        {
            if (!string.IsNullOrEmpty(password))
                return password;
            return GetUserPassElement(1);
        }
        internal string GetUserName()
        {
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
            catch (UriFormatException)
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



