// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Security;

internal sealed class FileProvider(FileSource source) : FileConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        using var reader = new StreamReader(stream);
        string value = reader.ReadToEnd();
        Data[source.Key] = value;
    }
}
