using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using System;


namespace SteelToe.Extensions.Configuration.ConfigServer
{
    public static class ConfigurationSettingsHelper
    {
        private const string SPRING_APPLICATION_PREFIX = "spring:application";

        public static void Initialize(string configPrefix, ConfigServerClientSettings settings, IHostingEnvironment environment, ConfigurationRoot root)
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

        }

        private static bool GetFailFast(IConfigurationSection configServerSection, ConfigurationRoot root, bool def)
        {
            var failFast = configServerSection["failFast"];
            if (!string.IsNullOrEmpty(failFast))
            {
                bool result;
                string resolved = ResovlePlaceholders(failFast, root);
                if (Boolean.TryParse(resolved, out result))
                    return result;
            }
            return def;
        }

        private static bool GetEnabled(IConfigurationSection configServerSection, ConfigurationRoot root, bool def)
        {
            var enabled = configServerSection["enabled"];
            if (!string.IsNullOrEmpty(enabled))
            {
                bool result;
                string resolved = ResovlePlaceholders(enabled, root);
                if (Boolean.TryParse(resolved, out result))
                    return result;
            }
            return def;

        }

        private static string GetUri(IConfigurationSection configServerSection, ConfigurationRoot root, string def)
        {

            // First check for spring:cloud:config:uri
            var uri = configServerSection["uri"];
            if (!string.IsNullOrEmpty(uri))
            {
                return uri;
            }

            // Take default if none of above
            return def;
        }

        private static string GetPassword(IConfigurationSection configServerSection)
        {
            return configServerSection["password"];
        }

        private static string GetUsername(IConfigurationSection configServerSection)
        {
            return configServerSection["username"];
        }

        private static string GetLabel(IConfigurationSection configServerSection)
        {
            // TODO: multi label  support
            return configServerSection["label"];
        }

        private static string GetApplicationName(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
            // TODO: Figure out a sensible "default" app name (e.g apps assembly name?)
            var appSection = root.GetSection(SPRING_APPLICATION_PREFIX);
            return GetSetting("name", configServerSection, appSection, null);
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

        private static bool GetCertificateValidation(IConfigurationSection configServerSection, ConfigurationRoot root, bool def)
        {
            var accept = configServerSection["validate_certificates"];
            if (!string.IsNullOrEmpty(accept))
            {
                bool result;
                string resolved = ResovlePlaceholders(accept, root);
                if (Boolean.TryParse(resolved, out result))
                    return result;
            }
            return def;

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
    }
}
