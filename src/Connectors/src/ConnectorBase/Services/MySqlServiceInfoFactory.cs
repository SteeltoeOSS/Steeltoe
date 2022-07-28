// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services;

public class MySqlServiceInfoFactory : RelationalServiceInfoFactory
{
    public MySqlServiceInfoFactory()
        : base(new Tags("mysql"), MySqlServiceInfo.MysqlScheme)
    {
    }

    public override IServiceInfo Create(string id, string url)
    {
        return new MySqlServiceInfo(id, url);
    }
}
