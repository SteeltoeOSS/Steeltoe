// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.Security;

internal sealed class FileProvider : FileConfigurationProvider
{
    public FileProvider(FileConfigurationSource source)
        : base(source)
    {
    }

    public override void Load(Stream stream)
    {
        var source = Source as FileSource;
        string key = source.Key;
        using var reader = new StreamReader(stream);
        string value = reader.ReadToEnd();
        Data[key] = value;
    }
}
