// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(Application))]
[JsonSerializable(typeof(SpringBootAdminApiClient.RegistrationResult))]
internal sealed partial class SpringBootAdminJsonSerializerContext : JsonSerializerContext
{
    private static readonly Lazy<JsonSerializerOptions> LazyOptionsWithReflectionFallback = new(CreateOptionsWithReflectionFallback);

    // Application.Metadata is IDictionary<string, object>, whose values can be arbitrary types (e.g. DateTime).
    // The source generator can't know these types at compile time, so we fall back to reflection for them.
    internal static JsonSerializerOptions OptionsWithReflectionFallback => LazyOptionsWithReflectionFallback.Value;

    private static JsonSerializerOptions CreateOptionsWithReflectionFallback()
    {
        var options = new JsonSerializerOptions();
        options.TypeInfoResolverChain.Add(Default);
        options.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
        return options;
    }
}
