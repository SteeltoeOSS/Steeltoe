using System;
using System.IO;

namespace Steeltoe.Common.IO
{
    public abstract class TempPath : IDisposable
    {
        /// <summary>
        /// Gets the absolute path of the TempPath.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets the name of the TempPath.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new instance of a TempPath with a GUID as its name.
        /// </summary>
        public TempPath() : this(Guid.NewGuid().ToString())
        {
        }

        /// <summary>
        /// Creates a new instance of a TempPath with the specified name.
        /// The full path to the file will be rooted in at <code>Path.GetTempPath()</code>.
        /// </summary>
        /// <param name="name">Name of temporary path</param>
        public TempPath(string name)
        {
            Name = name;
            FullPath = $"{Path.GetTempPath()}{Path.DirectorySeparatorChar}{Name}";
            Initialize();
        }

        protected virtual void Initialize()
        {
            
        }
        
        ~TempPath()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}