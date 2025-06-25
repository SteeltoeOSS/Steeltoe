// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

public static class TextConverter
{
    public static Stream ToStream(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var stream = new MemoryStream();

        using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            writer.Write(text);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
