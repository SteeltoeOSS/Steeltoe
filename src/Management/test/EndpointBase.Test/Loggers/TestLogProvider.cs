﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    internal class TestLogProvider : IDynamicLoggerProvider
    {
        public string Category { get; set; }

        public LogLevel Level { get; set; }

        public bool GetLoggerConfigurationsCalled
        {
            get; set;
        }

        public ILogger CreateLogger(string categoryName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ICollection<ILoggerConfiguration> GetLoggerConfigurations()
        {
            GetLoggerConfigurationsCalled = true;
            return new List<ILoggerConfiguration>();
        }

        public void SetLogLevel(string category, LogLevel? level)
        {
            Category = category;
            Level = level ?? LogLevel.None;
        }
    }
}
