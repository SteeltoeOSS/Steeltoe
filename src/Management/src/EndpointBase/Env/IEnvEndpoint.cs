// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Env;

public interface IEnvEndpoint
{
    EnvironmentDescriptor Invoke();

    string GetPropertySourceName(IConfigurationProvider provider);

    IList<PropertySourceDescriptor> GetPropertySources(IConfiguration configuration);

    PropertySourceDescriptor GetPropertySourceDescriptor(IConfigurationProvider provider);
}
