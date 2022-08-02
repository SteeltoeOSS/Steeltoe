// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Attributes;

/// <summary>
/// Annotation that marks a method to be a listener to inputs declared via EnableBinding.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class StreamListenerAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the binding target (e.g. channel).
    /// </summary>
    public virtual string Target { get; set; }

    /// <summary>
    /// Gets or sets the expression language condition that must be met by all items dispatched to this method.
    /// </summary>
    public virtual string Condition { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to copy incoming headers to outgoing messages.
    /// </summary>
    public virtual bool CopyHeaders { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamListenerAttribute" /> class.
    /// </summary>
    /// <param name="target">
    /// the name of the binding target (e.g. channel).
    /// </param>
    /// <param name="copyHeaders">
    /// when true, copy incoming headers to any outgoing messages.
    /// </param>
    public StreamListenerAttribute(string target, bool copyHeaders = true)
        : this(target, null, copyHeaders)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamListenerAttribute" /> class.
    /// </summary>
    /// <param name="target">
    /// the name of the binding target (e.g. channel).
    /// </param>
    /// <param name="condition">
    /// expression language condition that must be met by all items dispatched to this method.
    /// </param>
    /// <param name="copyHeaders">
    /// when true, copy incoming headers to any outgoing messages.
    /// </param>
    public StreamListenerAttribute(string target, string condition, bool copyHeaders = true)
    {
        Target = target;
        Condition = condition;
        CopyHeaders = copyHeaders;
    }
}
