// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Steeltoe.Extensions.Logging.Test
{
    internal class ConsoleOutputBorrower : IDisposable
    {
        private readonly StringWriter _borrowedOutput;
        private readonly TextWriter _originalOutput;

        public ConsoleOutputBorrower()
        {
            _borrowedOutput = new StringWriter();
            _originalOutput = Console.Out;
            Console.SetOut(_borrowedOutput);
        }

        public override string ToString()
        {
            return _borrowedOutput.ToString();
        }

        public void Dispose()
        {
            Console.SetOut(_originalOutput);
            _borrowedOutput.Dispose();
        }
    }
}