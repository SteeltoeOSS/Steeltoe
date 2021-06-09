using System.IO;

namespace Steeltoe.Common.IO
{
    public sealed class TempDirectory : TempPath
    {
        /// <inheritdoc/>
        public TempDirectory()
        {
        }

        /// <summary>
        public TempDirectory(string name) : base(name)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            Directory.CreateDirectory(FullPath);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!(Directory.Exists(FullPath)))
            {
                return;
            }

            try
            {
                Directory.Delete(FullPath, true);
            }
            catch
            {
                // ignore
            }
        }
    }
}