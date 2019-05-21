using System;

namespace Steeltoe.Common.Extensions
{
    public static class UriExtensions
    {
        public static string ToMaskedString(this Uri source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(source.UserInfo))
            {
                return source.ToString();
            }

            var builder = new UriBuilder(source)
            {
                UserName = "****",
                Password = "****"
            };

            return builder.Uri.ToString();
        }
    }
}
