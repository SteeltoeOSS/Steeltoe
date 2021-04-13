// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Configuration.SpringBoot
{
    /// <summary>
    /// Configuration source used in creating a <see cref="SpringBootEnvProvider"/> that generates random numbers
    /// </summary>
    public class SpringBootEnvSource : IConfigurationSource
    {
        internal ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpringBootEnvSource"/> class.
        /// </summary>
        /// <param name="logFactory">the logger factory to use</param>
        public SpringBootEnvSource(ILoggerFactory logFactory = null)
        {
            _loggerFactory = logFactory;
        }

        /// <summary>
        /// Builds a <see cref="SpringBootEnvProvider"/> from the sources.
        /// </summary>
        /// <param name="builder">the provided builder</param>
        /// <returns>the SpringBootEnv provider</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SpringBootEnvProvider(_loggerFactory);
        }
    }
}
