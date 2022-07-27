// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;

namespace Steeltoe.Integration;

/// <summary>
/// A group of commonly used services used by the integration components
/// </summary>
public interface IIntegrationServices
{
    /// <summary>
    /// Gets or sets the current message builder factory
    /// </summary>
    IMessageBuilderFactory MessageBuilderFactory { get; set; }

    /// <summary>
    /// Gets or sets the current channel resolver
    /// </summary>
    IDestinationResolver<IMessageChannel> ChannelResolver { get; set; }

    /// <summary>
    /// Gets or sets the current conversion service
    /// </summary>
    IConversionService ConversionService { get; set; }

    /// <summary>
    /// Gets or sets the current id generator
    /// </summary>
    IIDGenerator IdGenerator { get; set; }

    /// <summary>
    /// Gets or sets the current id expression parser
    /// </summary>
    IExpressionParser ExpressionParser { get; set; }
}