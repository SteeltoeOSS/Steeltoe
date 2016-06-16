using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;


namespace SteelToe.Extensions.Configuration.ConfigServer
{
    public static class ConfigurationSettingsHelper
    {
        private const string SPRING_APPLICATION_PREFIX = "spring:application";

        public static void Initialize(string configPrefix, ConfigServerClientSettings settings, IHostingEnvironment environment, IConfigurationRoot root)
        {
            if (configPrefix == null)
            {
                throw new ArgumentNullException(nameof(configPrefix));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }


            var clientConfigsection = root.GetSection(configPrefix);

            settings.Name = ResovlePlaceholders(GetApplicationName(clientConfigsection, root), root);
            settings.Environment = ResovlePlaceholders(GetEnvironment(clientConfigsection, environment), root);
            settings.Label = ResovlePlaceholders(GetLabel(clientConfigsection), root);
            settings.Username = ResovlePlaceholders(GetUsername(clientConfigsection), root);
            settings.Password = ResovlePlaceholders(GetPassword(clientConfigsection), root);
            settings.Uri = ResovlePlaceholders(GetUri(clientConfigsection, root, settings.Uri), root);
            settings.Enabled = GetEnabled(clientConfigsection, root, settings.Enabled);
            settings.FailFast = GetFailFast(clientConfigsection, root, settings.FailFast);
            settings.ValidateCertificates = GetCertificateValidation(clientConfigsection, root, settings.ValidateCertificates);
            settings.RetryEnabled = GetRetryEnabled(clientConfigsection, root, settings.RetryEnabled);
            settings.RetryInitialInterval = GetRetryInitialInterval(clientConfigsection, root, settings.RetryInitialInterval);
            settings.RetryMaxInterval = GetRetryMaxInterval(clientConfigsection, root, settings.RetryMaxInterval);
            settings.RetryMultiplier = GetRetryMultiplier(clientConfigsection, root, settings.RetryMultiplier);
            settings.RetryAttempts = GetRetryMaxAttempts(clientConfigsection, root, settings.RetryAttempts);
        }
   
        private static int GetRetryMaxAttempts(IConfigurationSection clientConfigsection, IConfigurationRoot root, int def)
        {
            return GetInt("retry:maxAttempts", clientConfigsection, root, def);
        }

        private static double GetRetryMultiplier(IConfigurationSection clientConfigsection, IConfigurationRoot root, double def)
        {
            return GetDouble("retry:multiplier", clientConfigsection, root, def);
        }

        private static int GetRetryMaxInterval(IConfigurationSection clientConfigsection, IConfigurationRoot root, int def)
        {
            return GetInt("retry:maxInterval", clientConfigsection, root, def);
        }

        private static int GetRetryInitialInterval(IConfigurationSection clientConfigsection, IConfigurationRoot root, int def)
        {
            return GetInt("retry:initialInterval", clientConfigsection, root, def);
        }

        private static bool GetRetryEnabled(IConfigurationSection clientConfigsection, IConfigurationRoot root, bool def)
        {
            return GetBoolean("retry:enabled", clientConfigsection, root, def);
        }

        private static bool GetFailFast(IConfigurationSection clientConfigsection, IConfigurationRoot root, bool def)
        {
            return GetBoolean("failFast", clientConfigsection, root, def);
        }

        private static bool GetEnabled(IConfigurationSection clientConfigsection, IConfigurationRoot root, bool def)
        {
            return GetBoolean("enabled", clientConfigsection, root, def);
        }

        private static string GetUri(IConfigurationSection clientConfigsection, IConfigurationRoot root, string def)
        {

            // First check for spring:cloud:config:uri
            var uri = clientConfigsection["uri"];
            if (!string.IsNullOrEmpty(uri))
            {
                return uri;
            }

            // Take default if none of above
            return def;
        }

        private static string GetPassword(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection["password"];
        }

        private static string GetUsername(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection["username"];
        }

        private static string GetLabel(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection["label"];
        }

        private static string GetApplicationName(IConfigurationSection clientConfigsection, IConfigurationRoot root)
        {
            // TODO: Figure out a sensible "default" app name (e.g apps assembly name?)
            var appSection = root.GetSection(SPRING_APPLICATION_PREFIX);
            return GetSetting("name", clientConfigsection, appSection, null);
        }

        private static string GetEnvironment(IConfigurationSection section, IHostingEnvironment environment)
        {
            // if spring:cloud:config:env present, use it
            var env = section["env"];
            if (!string.IsNullOrEmpty(env))
            {
                return env;
            }

            // Otherwise use ASP.NET 5 defined value (i.e. ASPNET_ENV or Hosting:Environment) (its default is 'Production')
            return environment.EnvironmentName;
        }

        private static bool GetCertificateValidation(IConfigurationSection clientConfigsection, IConfigurationRoot root, bool def)
        {
            return GetBoolean("validate_certificates", clientConfigsection, root, def);
        }

        private static string ResovlePlaceholders(string property, IConfiguration config)
        {
            return PropertyPlaceholderHelper.ResovlePlaceholders(property, config);
        }

        private static string GetSetting(string key, IConfigurationSection primary, IConfigurationSection secondary, string def)
        {
            // First check for key in primary
            var setting = primary[key];
            if (!string.IsNullOrEmpty(setting))
            {
                return setting;
            }

            // Next check for key in secondary
            setting = secondary[key];
            if (!string.IsNullOrEmpty(setting))
            {
                return setting;
            }

            return def;
        }

        private static int GetInt(string key, IConfigurationSection clientConfigsection, IConfigurationRoot root, int def)
        {
            var val = clientConfigsection[key];
            if (!string.IsNullOrEmpty(val))
            {
                int result;
                string resolved = ResovlePlaceholders(val, root);
                if (int.TryParse(resolved, out result))
                    return result;
            }
            return def;
        }
        private static double GetDouble(string key, IConfigurationSection clientConfigsection, IConfigurationRoot root, double def)
        {
            var val = clientConfigsection[key];
            if (!string.IsNullOrEmpty(val))
            {
                double result;
                string resolved = ResovlePlaceholders(val, root);
                if (double.TryParse(resolved, out result))
                    return result;
            }
            return def;
        }

        private static bool GetBoolean(string key, IConfigurationSection clientConfigsection, IConfigurationRoot root, bool def)
        {
            var val = clientConfigsection[key];
            if (!string.IsNullOrEmpty(val))
            {
                bool result;
                string resolved = ResovlePlaceholders(val, root);
                if (Boolean.TryParse(resolved, out result))
                    return result;
            }
            return def;

        }

    }
}
