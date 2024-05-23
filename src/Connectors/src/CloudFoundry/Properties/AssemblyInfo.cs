// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Steeltoe.Connectors;
using Steeltoe.Connectors.CloudFoundry;

[assembly: InternalsVisibleTo("Steeltoe.Connectors.CloudFoundry.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Security.Authentication.CloudFoundry")]

[assembly: ServiceInfoFactoryAssembly]
[assembly: ServiceInfoCreatorAssembly(typeof(CloudFoundryServiceInfoCreator))]
