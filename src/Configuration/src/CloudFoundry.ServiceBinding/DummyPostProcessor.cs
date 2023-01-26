// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding;

internal sealed class DummyPostProcessor : IConfigurationPostProcessor
{
    public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string> configurationData)
    {
        // This dummy only exists to make tests succeed.
        // To be removed after adding real processors to this project.
        throw new NotImplementedException();
    }
}
