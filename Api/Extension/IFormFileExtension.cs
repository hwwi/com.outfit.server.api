using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Api.Extension
{
    public static class IFormFileExtension
    {
        private static readonly String[] ImageFormat = {".png", ".gif", ".jpg", ".jpeg",".exif",".tiff"};

        public static bool IsSupportedImageFormat(this IFormFile formFile)
        {
            return formFile != null
                   && formFile.ContentType.StartsWith("image", StringComparison.InvariantCulture)
                   && ImageFormat.Contains(Path.GetExtension(formFile.FileName), StringComparer.OrdinalIgnoreCase)
                ;
        }
    }
}