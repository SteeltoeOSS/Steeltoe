// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Utils.Diagnostics
{
    /// <summary>
    /// The exception that is thrown when a system error occurs running a command.
    /// </summary>
    public class CommandException : Exception
    {
        /// <inheritdoc cref="Exception"/>
        public CommandException(string message)
            : base(message)
        {
        }

        /// <inheritdoc cref="Exception"/>
        public CommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
