// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Steeltoe.Connector.Test
{
    public class TestHelpers
    {
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
