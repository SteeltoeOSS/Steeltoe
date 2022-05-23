// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public class SpelParserOptions
    {
        public SpelParserOptions()
        : this(null, false, false, int.MaxValue)
        {
        }

        public SpelParserOptions(SpelCompilerMode compilerMode)
        : this(compilerMode, false, false, int.MaxValue)
        {
        }

        public SpelParserOptions(bool autoGrowNullReferences, bool autoGrowCollections)
        : this(null, autoGrowNullReferences, autoGrowCollections, int.MaxValue)
        {
        }

        public SpelParserOptions(bool autoGrowNullReferences, bool autoGrowCollections, int maximumAutoGrowSize)
        : this(null, autoGrowNullReferences, autoGrowCollections, maximumAutoGrowSize)
        {
        }

        public SpelParserOptions(SpelCompilerMode? compilerMode, bool autoGrowNullReferences, bool autoGrowCollections, int maximumAutoGrowSize)
        {
            CompilerMode = compilerMode ?? SpelCompilerMode.OFF;
            AutoGrowNullReferences = autoGrowNullReferences;
            AutoGrowCollections = autoGrowCollections;
            MaximumAutoGrowSize = maximumAutoGrowSize;
        }

        public int MaximumAutoGrowSize { get; set; }

        public bool AutoGrowCollections { get; set; }

        public bool AutoGrowNullReferences { get; set; }

        public SpelCompilerMode CompilerMode { get; set; }
    }
}
