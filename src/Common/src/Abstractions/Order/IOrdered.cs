// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Order
{
    /// <summary>
    /// An interface that can be implemented by objects that should be orderable.
    /// </summary>
    public interface IOrdered
    {
        /// <summary>
        /// Gets the order of this object
        /// </summary>
        int Order { get; }
    }
}
