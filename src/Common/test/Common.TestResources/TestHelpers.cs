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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Steeltoe.Common
{
    public class TestHelpers
    {
        public static string CreateTempFile(string contents)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }

        public static Stream StringToStream(string str)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(str);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }

        public static string StreamToString(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        public static ILoggerFactory GetLoggerFactory()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace));
            serviceCollection.AddLogging(builder => builder.AddConsole((opts) =>
            {
                opts.DisableColors = true;
            }));
            serviceCollection.AddLogging(builder => builder.AddDebug());
            return serviceCollection.BuildServiceProvider().GetService<ILoggerFactory>();
        }

        public static string VCAP_APPLICATION = @"
            {
                ""limits"": {
                ""fds"": 16384,
                ""mem"": 1024,
                ""disk"": 1024
                },
                ""application_name"": ""spring-cloud-broker"",
                ""application_uris"": [
                ""spring-cloud-broker.apps.testcloud.com""
                ],
                ""name"": ""spring-cloud-broker"",
                ""space_name"": ""p-spring-cloud-services"",
                ""space_id"": ""65b73473-94cc-4640-b462-7ad52838b4ae"",
                ""uris"": [
                ""spring-cloud-broker.apps.testcloud.com""
                ],
                ""users"": null,
                ""version"": ""07e112f7-2f71-4f5a-8a34-db51dbed30a3"",
                ""application_version"": ""07e112f7-2f71-4f5a-8a34-db51dbed30a3"",
                ""application_id"": ""798c2495-fe75-49b1-88da-b81197f2bf06""
            }";
    }
}
