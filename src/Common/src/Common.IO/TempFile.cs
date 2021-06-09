using System.IO;

namespace Steeltoe.Common.IO
{
    public sealed class TempFile : TempPath
    {
        /// <summary>
        /// Creates a new instance of a TempFile.
        /// </summary>
        public TempFile()
        {
        }

        /// <summary>
        /// Creates a new instance of a TempFile with the specified name.
        /// </summary>
        public TempFile(string name) : base(name)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            File.Create(FullPath).Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!(File.Exists(FullPath)))
            {
                return;
            }

            try
            {
                File.Delete(FullPath);
            }
            catch
            {
                // ignore
            }
        }
    }
}