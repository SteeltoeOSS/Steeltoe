// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public interface IDeclarable
{
    /// <summary>
    /// Gets or sets a value indicating whether this object should be declared.
    /// </summary>
    bool ShouldDeclare { get; set; }

    /// <summary>
    /// Gets or sets a collection of Admins that should declare this object.
    /// </summary>
    List<object> DeclaringAdmins { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether should ignore exceptions.
    /// </summary>
    public bool IgnoreDeclarationExceptions { get; set; }

    /// <summary>
    /// Gets or sets the arguments for this declarable.
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; }

    /// <summary>
    /// Adds an argument to the declarable.
    /// </summary>
    /// <param name="name">the argument name.</param>
    /// <param name="value">the argument value.</param>
    void AddArgument(string name, object value);

    /// <summary>
    /// Remove an argument from the declarable.
    /// </summary>
    /// <param name="name">the argument name.</param>
    /// <returns>the value if present.</returns>
    object RemoveArgument(string name);
}
