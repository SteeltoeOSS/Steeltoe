// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.Services;

public class ServiceContextDescriptor:Dictionary<string, ApplicationContext>
{
}

public class ApplicationContext:Dictionary<string, ServiceDescriptor>
{
}
