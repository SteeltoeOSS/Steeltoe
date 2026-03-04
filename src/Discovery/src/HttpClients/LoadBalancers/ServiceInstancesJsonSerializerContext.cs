// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.HttpClients.LoadBalancers;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(ServiceInstancesResolver.JsonSerializableServiceInstance[]))]
internal sealed partial class ServiceInstancesJsonSerializerContext : JsonSerializerContext;
