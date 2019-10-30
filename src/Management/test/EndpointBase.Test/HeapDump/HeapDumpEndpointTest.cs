// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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
