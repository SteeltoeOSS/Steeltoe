// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Converter;

public interface ITypeMapper
{
    TypePrecedence Precedence { get; set; }

    Type DefaultType { get; set; }

    void FromType(Type type, IMessageHeaders headers);

    Type ToType(IMessageHeaders headers);

    Type GetInferredType(IMessageHeaders headers);
}
