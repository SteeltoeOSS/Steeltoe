// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;

namespace Steeltoe.Management.Endpoint.DbMigrations.Test;

public class SomeDbException : DbException
{
    public SomeDbException(string message)
        : base(message)
    {
    }
}