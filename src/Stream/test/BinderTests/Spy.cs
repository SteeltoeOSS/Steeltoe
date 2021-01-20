using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// Represents an out-of-band connection to the underlying middleware, so that tests can check that some messages actually do (or do not) transit through it.
    /// </summary>
    public class Spy
    {
        public Func<bool, object> Receive;
    }
}
