// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

internal sealed class FakeHeapDumper : IHeapDumper, IDisposable
{
    public static readonly byte[] FakeFileContent = "FAKEDUMP_"u8.ToArray().Concat(Enumerable.Repeat((byte)0xFF, 1024)).ToArray();

    private readonly List<TempFile> _filesCreated = [];

    public string DumpHeapToFile(CancellationToken cancellationToken)
    {
        var file = new TempFile();
        File.WriteAllBytes(file.FullPath, FakeFileContent);

        _filesCreated.Add(file);
        return file.FullPath;
    }

    public void Dispose()
    {
        foreach (TempFile file in _filesCreated)
        {
            file.Dispose();
        }
    }
}
