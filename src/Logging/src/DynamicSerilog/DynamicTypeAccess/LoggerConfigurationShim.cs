// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Logging.DynamicSerilog.DynamicTypeAccess;

internal sealed class LoggerConfigurationShim : Shim
{
    public LogEventLevel MinimumLevel => InstanceAccessor.GetPrivateFieldValue<LogEventLevel>("_minimumLevel");

    public Dictionary<string, LoggingLevelSwitch> Overrides => InstanceAccessor.GetPrivateFieldValue<Dictionary<string, LoggingLevelSwitch>>("_overrides");

    public LoggerConfigurationShim(object instance)
        : base(new InstanceAccessor(new TypeAccessor(typeof(LoggerConfiguration)), instance))
    {
    }
}
