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

            return source.ToMaskedUri().ToString();
        }

        public static Uri ToMaskedUri(this Uri source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(source.UserInfo))
            {
                return source;
            }

            var builder = new UriBuilder(source)
            {
                UserName = "****",
                Password = "****"
            };

            return builder.Uri;
        }
    }
}
