// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Core
{
    /// <summary>
    /// A registry which can be used to store and lookup objects (destinations) by name
    /// </summary>
    public interface IDestinationRegistry : IDisposable
    {
        /// <summary>
        /// Lookup an object by name
        /// </summary>
        /// <param name="name">the name to use during lookup</param>
        /// <returns>the object if found</returns>
        object Lookup(string name);

        /// <summary>
        /// Register an object (destination) in the registry
        /// </summary>
        /// <param name="name">the name of the object</param>
        /// <param name="destination">the object to be associated with the name</param>
        void Register(string name, object destination);

        /// <summary>
        /// Remove a registered object from the registry
        /// </summary>
        /// <param name="name">the name to remove</param>
        /// <returns>the object removed or null if not found</returns>
        object Deregister(string name);

        /// <summary>
        /// Determine if an object exists in the registry
        /// </summary>
        /// <param name="name">the name to use during the lookup</param>
        /// <returns>true if name exists, false otherwise</returns>
        bool Contains(string name);

        /// <summary>
        /// Gets the underlying service provider the registry may be using during name lookup
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
