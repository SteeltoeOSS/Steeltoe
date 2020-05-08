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

using System;
using System.Security.Cryptography.X509Certificates;

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

        /// <summary>
        /// Default Vault Token Time to Live setting
        /// </summary>
        public const int DEFAULT_VAULT_TOKEN_TTL = 300000;

        /// <summary>
        /// Default Vault Token renewal rate
        /// </summary>
        public const int DEFAULT_VAULT_TOKEN_RENEW_RATE = 60000;

        /// <summary>
        /// Default Disable Vault Token renewal
        /// </summary>
        public const bool DEFAULT_DISABLE_TOKEN_RENEWAL = false;

        /// <summary>
        /// Default address used by provider to obtain a OAuth Access Token
        /// </summary>
        public const string DEFAULT_ACCESS_TOKEN_URI = null;

        /// <summary>
        /// Default client id used by provider to obtain a OAuth Access Token
        /// </summary>
        public const string DEFAULT_CLIENT_ID = null;

        /// <summary>
        /// Default client secret used by provider to obtain a OAuth Access Token
        /// </summary>
        public const string DEFAULT_CLIENT_SECRET = null;

        /// <summary>
        /// Default discovery first enabled setting
        /// </summary>
        public const bool DEFAULT_DISCOVERY_ENABLED = false;

        /// <summary>
        /// Default discovery first service id setting
        /// </summary>
        public const string DEFAULT_CONFIGSERVER_SERVICEID = "configserver";

        /// <summary>
        /// Default health check enabled setting
        /// </summary>
        public const bool DEFAULT_HEALTH_ENABLED = true;

        /// <summary>
        /// Default health check time to live in milliseconds setting
        /// </summary>
        public const long DEFAULT_HEALTH_TIMETOLIVE = 60 * 5 * 1000;

        private static readonly char[] COLON_DELIMIT = new char[] { ':' };
        private static readonly char[] COMMA_DELIMIT = new char[] { ',' };

        private string username;
        private string password;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigServerClientSettings"/> class.
        /// </summary>
        /// <remarks>Initialize Config Server client settings with defaults</remarks>
        public ConfigServerClientSettings()
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
            DiscoveryEnabled = DEFAULT_DISCOVERY_ENABLED;
            DiscoveryServiceId = DEFAULT_CONFIGSERVER_SERVICEID;
            HealthEnabled = DEFAULT_HEALTH_ENABLED;
            HealthTimeToLive = DEFAULT_HEALTH_TIMETOLIVE;
        }

        /// <summary>
        /// Gets or sets the Config Server address
        /// </summary>
        public virtual string Uri { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether enables/Disables the Config Server provider
        /// </summary>
        public virtual bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the environment used when accessing configuration data
        /// </summary>
        public virtual string Environment { get; set; }

        /// <summary>
        /// Gets or sets the application name used when accessing configuration data
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets or sets the label used when accessing configuration data
        /// </summary>
        public virtual string Label { get; set; }

#pragma warning disable S4275 // Getters and setters should access the expected fields
        /// <summary>
        /// Gets or sets the username used when accessing the Config Server
        /// </summary>
        public virtual string Username
        {
            get { return GetUserName(); }
            set { username = value; }
        }

        /// <summary>
        /// Gets or sets the password used when accessing the Config Server
        /// </summary>
        public virtual string Password
        {
            get { return GetPassword(); }
            set { password = value; }
        }
#pragma warning restore S4275 // Getters and setters should access the expected fields

        /// <summary>
        /// Gets or sets a value indicating whether enables/Disables failfast behavior
        /// </summary>
        public virtual bool FailFast { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether enables/Disables whether provider validates server certificates
        /// </summary>
        public virtual bool ValidateCertificates { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether enables/Disables config server client retry on failures
        /// </summary>
        public virtual bool RetryEnabled { get; set; }

        /// <summary>
        /// Gets or sets initial retry interval in milliseconds
        /// </summary>
        public virtual int RetryInitialInterval { get; set; }

        /// <summary>
        /// Gets or sets max retry interval in milliseconds
        /// </summary>
        public virtual int RetryMaxInterval { get; set; }

        /// <summary>
        ///  Gets or sets multiplier for next retry interval
        /// </summary>
        public virtual double RetryMultiplier { get; set; }

        /// <summary>
        /// Gets or sets the max number of retries the client will attempt
        /// </summary>
        public virtual int RetryAttempts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether discovery first behavior is enabled
        /// </summary>
        public virtual bool DiscoveryEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value of the service id used during discovery first behavior
        /// </summary>
        public virtual string DiscoveryServiceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether health check is enabled
        /// </summary>
        public virtual bool HealthEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value for the health check cache time to live
        /// </summary>
        public virtual long HealthTimeToLive { get; set; }

        /// <summary>
        /// Gets returns the HttpRequestUrl, unescaped
        /// </summary>
        [Obsolete("Will be removed, use RawUris instead")]
        public virtual string RawUri
        {
            get { return GetRawUri(); }
        }

        /// <summary>
        /// Gets returns HttpRequestUrls, unescaped
        /// </summary>
        public virtual string[] RawUris
        {
            get { return GetRawUris(); }
        }

        /// <summary>
        /// Gets or sets returns the token use for Vault
        /// </summary>
        public virtual string Token { get; set; }

        /// <summary>
        /// Gets or sets returns the request timeout in milliseconds
        /// </summary>
        public virtual int Timeout { get; set; }

        /// <summary>
        /// Gets or sets address used by provider to obtain a OAuth Access Token
        /// </summary>
        public virtual string AccessTokenUri { get; set; } = DEFAULT_ACCESS_TOKEN_URI;

        /// <summary>
        /// Gets or sets client id used by provider to obtain a OAuth Access Token
        /// </summary>
        public virtual string ClientId { get; set; } = DEFAULT_CLIENT_ID;

        /// <summary>
        /// Gets or sets client secret used by provider to obtain a OAuth Access Token
        /// </summary>
        public virtual string ClientSecret { get; set; } = DEFAULT_CLIENT_SECRET;

        public virtual X509Certificate2 ClientCertificate { get; set; }

        /// <summary>
        /// Gets or sets vault token Time to Live setting in Millisecoonds
        /// </summary>
        public virtual int TokenTtl { get; set; } = DEFAULT_VAULT_TOKEN_TTL;

        /// <summary>
        /// Gets or sets vault token renew rate in Milliseconds
        /// </summary>
        public virtual int TokenRenewRate { get; set; } = DEFAULT_VAULT_TOKEN_RENEW_RATE;

        public virtual bool DisableTokenRenewal { get; set; } = DEFAULT_DISABLE_TOKEN_RENEWAL;

        internal static bool IsMultiServerConfig(string uris)
        {
            return uris.Contains(",");
        }

        [Obsolete("Will be removed, use GetRawUris() instead")]
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

        internal string GetRawUri(string uri)
        {
            try
            {
                System.Uri ri = new System.Uri(uri);
                return ri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            catch (UriFormatException)
            {
            }

            return null;
        }

        internal string[] GetRawUris()
        {
            if (!string.IsNullOrEmpty(Uri))
            {
                string[] uris = Uri.Split(COMMA_DELIMIT, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < uris.Length; i++)
                {
                    string uri = GetRawUri(uris[i]);
                    if (string.IsNullOrEmpty(uri))
                    {
                        return Array.Empty<string>();
                    }

                    uris[i] = uri;
                }

                return uris;
            }

            return Array.Empty<string>();
        }

        internal string[] GetUris()
        {
            if (!string.IsNullOrEmpty(Uri))
            {
                return Uri.Split(COMMA_DELIMIT, StringSplitOptions.RemoveEmptyEntries);
            }

            return Array.Empty<string>();
        }

        internal string GetPassword()
        {
            return GetPassword(Uri);
        }

        internal string GetPassword(string uri)
        {
            if (!string.IsNullOrEmpty(password))
            {
                return password;
            }

            return GetUserPassElement(uri, 1);
        }

        internal string GetUserName()
        {
            return GetUserName(Uri);
        }

        internal string GetUserName(string uri)
        {
            if (!string.IsNullOrEmpty(username))
            {
                return username;
            }

            return GetUserPassElement(uri, 0);
        }

        private static string GetUserInfo(string uri)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                Uri u = new Uri(uri);
                return u.UserInfo;
            }

            return null;
        }

        private static string GetUserPassElement(string uri, int index)
        {
            if (IsMultiServerConfig(uri))
            {
                return null;
            }

            string result = null;
            string userInfo = GetUserInfo(uri);
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
