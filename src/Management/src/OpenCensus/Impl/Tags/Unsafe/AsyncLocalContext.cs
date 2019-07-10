// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Management.Census.Tags.Unsafe
{
    [Obsolete("Use OpenCensus project packages")]
    internal static class AsyncLocalContext
    {
        private static readonly ITagContext EMPTY_TAG_CONTEXT = new EmptyTagContext();

        private static readonly AsyncLocal<ITagContext> _context = new AsyncLocal<ITagContext>();

        public static ITagContext CurrentTagContext
        {
            get
            {
                if (_context.Value == null)
                {
                    return EMPTY_TAG_CONTEXT;
                }

                return _context.Value;
            }

            set
            {
                if (value == EMPTY_TAG_CONTEXT)
                {
                    _context.Value = null;
                }
                else
                {
                    _context.Value = value;
                }
            }
        }

        internal sealed class EmptyTagContext : TagContextBase
        {
            public override IEnumerator<ITag> GetEnumerator()
            {
                return new List<ITag>().GetEnumerator();
            }
        }
    }
}
