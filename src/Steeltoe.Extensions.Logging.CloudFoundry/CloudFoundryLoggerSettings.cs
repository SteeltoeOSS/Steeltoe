
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;

namespace Steeltoe.Extensions.Logging.CloudFoundry

{
    public class CloudFoundryLoggerSettings : ICloudFoundryLoggerSettings
    {
        private IConsoleLoggerSettings _settings;
        private ConfigurationReloadToken _changeToken;
        
        private IDictionary<string, LogLevel> DynamicSwitches { get; set; } 

        public CloudFoundryLoggerSettings()
        {
            DynamicSwitches = new Dictionary<string, LogLevel>();
        }

        public CloudFoundryLoggerSettings(IConfiguration configuration)
        {
            _changeToken = new ConfigurationReloadToken();
            _settings = new ConfigurationConsoleLoggerSettings(configuration);
            _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
            DynamicSwitches = new Dictionary<string, LogLevel>();
        }



        private void OnConfigurationReload(object obj)
        {
            // Configuration changed;
            _changeToken.OnReload();
        }


        public IChangeToken ChangeToken {
            get { return _changeToken;  }
               
        }

        public bool IncludeScopes
        {
            get
            {
                return _settings.IncludeScopes;
            }
        }

        public IConsoleLoggerSettings Reload()
        {
            _settings = _settings.Reload();
            _changeToken = new ConfigurationReloadToken();
            _settings.ChangeToken.RegisterChangeCallback(OnConfigurationReload, null);
            return this;
        }

        public bool TryGetSwitch(string name, out LogLevel level)
        {
            if (DynamicSwitches.TryGetValue(name, out level))
            {
                return true;
            }
            return _settings.TryGetSwitch(name, out level);
     
        }

        public void SetLogLevel(string category, LogLevel level)
        {
            DynamicSwitches[category] = level;
            _changeToken.OnReload();
        }
    }
}

