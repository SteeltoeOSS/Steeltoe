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

using System;

namespace Steeltoe.Extensions.Configuration.ConfigServer
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
        /// Default number of retries to be attempted
        /// </summary>
        public const int DEFAULT_MAX_RETRY_ATTEMPTS = 6;

        /// <summary>
        /// Default initial retry interval in milliseconds
        /// </summary>
        public const int DEFAULT_INITIAL_RETRY_INTERVAL = 1000;

        /// <summary>
        /// Default multiplier for next retry interval
        /// </summary>
        public const double DEFAULT_RETRY_MULTIPLIER = 1.1;

        /// <summary>
        /// Default initial retry interval in milliseconds
        /// </summary>
        public const int DEFAULT_MAX_RETRY_INTERVAL = 2000;

        /// <summary>
        /// Default retry enabled setting
        /// </summary>
        public const bool DEFAULT_RETRY_ENABLED = false;

        /// <summary>
        /// Default timeout in milliseconds
        /// </summary>
        public const int DEFAULT_TIMEOUT_MILLISECONDS = 6 * 1000;

        private static readonly char[] COLON_DELIMIT = new char[] { ':' };
        private string username;
        private string password;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerClientSettings"/> class.
        /// </summary>
        /// <remarks>Initialize Config Server client settings with defaults</remarks>
        public ConfigServerClientSettings()
            : base()
        {
            ValidateCertificates = DEFAULT_CERTIFICATE_VALIDATION;
            FailFast = DEFAULT_FAILFAST;
            Environment = DEFAULT_ENVIRONMENT;
            Enabled = DEFAULT_PROVIDER_ENABLED;
            Uri = DEFAULT_URI;
            RetryEnabled = DEFAULT_RETRY_ENABLED;
            RetryInitialInterval = DEFAULT_INITIAL_RETRY_INTERVAL;
            RetryMaxInterval = DEFAULT_MAX_RETRY_INTERVAL;
            RetryAttempts = DEFAULT_MAX_RETRY_ATTEMPTS;
            RetryMultiplier = DEFAULT_RETRY_MULTIPLIER;
            Timeout = DEFAULT_TIMEOUT_MILLISECONDS;
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
        /// Enables/Disables config server client retry on failures
        /// </summary>
        public virtual bool RetryEnabled { get; set; }

        /// <summary>
        /// Initial retry interval in milliseconds
        /// </summary>
        public virtual int RetryInitialInterval { get; set; }

        /// <summary>
        /// Max retry interval in milliseconds
        /// </summary>
        public virtual int RetryMaxInterval { get; set; }

        /// <summary>
        ///  Multiplier for next retry interval
        /// </summary>
        public virtual double RetryMultiplier { get; set; }

        /// <summary>
        /// The max number of retries the client will attempt
        /// </summary>
        public virtual int RetryAttempts { get; set; }

        /// <summary>
        /// Returns the HttpRequestUrl, unescaped
        /// </summary>
        public virtual string RawUri
        {
            get { return GetRawUri(); }
        }

        /// <summary>
        /// Returns the token use for Vault
        /// </summary>
        public virtual string Token { get; set; }

        /// <summary>
        /// Returns the request timeout in milliseconds
        /// </summary>
        public virtual int Timeout { get; set; }

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
            {
                return password;
            }

            return GetUserPassElement(1);
        }

        internal string GetUserName()
        {
            if (!string.IsNullOrEmpty(username))
            {
                return username;
            }

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
                {
                    result = info[index];
                }
            }

            return result;
        }
    }
}
