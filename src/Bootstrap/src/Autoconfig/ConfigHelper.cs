using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Bootstrap.Autoconfig
{
    internal static class ConfigHelper
    {
        public static bool HasWavefront()
        {
            var wavefront_url_prefix = "management:metrics:export:wavefront:uri";
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var uri = configuration.GetValue<string>(wavefront_url_prefix);
            return !string.IsNullOrEmpty(uri);
        }
    }
}
