// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;

namespace Steeltoe.Extensions.Logging.Test
{
    internal class ConsoleOutputBorrower : IDisposable
    {
        private StringWriter _borrowedOutput;
        private TextWriter _originalOutput;

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