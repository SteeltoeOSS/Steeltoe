// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Steeltoe.Management.Endpoint.HeapDump.Test
{
    public class HeapDumpEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfNullRepo()
        {
            Assert.Throws<ArgumentNullException>(() => new HeapDumpEndpoint(new HeapDumpEndpointOptions(), null));
        }

        [Fact]
        public void Invoke_CreatesDump()
        {
            if (Platform.IsWindows)
            {
                var loggerFactory = TestHelpers.GetLoggerFactory();
                var logger1 = loggerFactory.CreateLogger<WindowsHeapDumper>();
                var logger2 = loggerFactory.CreateLogger<HeapDumpEndpoint>();

                var dumper = new WindowsHeapDumper(new HeapDumpEndpointOptions(), logger: logger1);
                var ep = new HeapDumpEndpoint(new HeapDumpEndpointOptions(), dumper, logger2);

                var result = ep.Invoke();
                Assert.NotNull(result);
                Assert.True(File.Exists(result));
                File.Delete(result);
            }
            else if (Platform.IsLinux)
            {
                if (typeof(object).Assembly.GetType("System.Index") != null)
                {
                    var loggerFactory = TestHelpers.GetLoggerFactory();
                    var logger1 = loggerFactory.CreateLogger<LinuxHeapDumper>();
                    var logger2 = loggerFactory.CreateLogger<HeapDumpEndpoint>();

                    var dumper = new LinuxHeapDumper(new HeapDumpEndpointOptions(), logger: logger1);
                    var ep = new HeapDumpEndpoint(new HeapDumpEndpointOptions(), dumper, logger2);

                    var result = ep.Invoke();
                    Assert.NotNull(result);
                    Assert.True(File.Exists(result));
                    File.Delete(result);
                }
            }
        }
    }
}
