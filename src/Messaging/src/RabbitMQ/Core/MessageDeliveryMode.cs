// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.RabbitMQ.Core;

[TypeConverter(typeof(SnakeCaseAllCapsEnumMemberTypeConverter<MessageDeliveryMode>))]
public enum MessageDeliveryMode
{
    NonPersistent = 1,
    Persistent = 2
}
