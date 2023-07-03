// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Steeltoe.Connectors;

[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration")]
[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connectors.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connectors.CloudFoundry.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connectors.EntityFrameworkCore")]
[assembly: InternalsVisibleTo("Steeltoe.Connectors.EntityFrameworkCore.Test")]

[assembly: ServiceInfoFactoryAssembly]
