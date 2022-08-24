// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Steeltoe.Connector;

[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connector.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connector.CloudFoundry.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connector.EntityFrameworkCore.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connector.EntityFramework6.Test")]
[assembly: InternalsVisibleTo("External.Connector.Test")]

[assembly: ServiceInfoFactoryAssembly]
[assembly: ConnectionInfoAssembly]
