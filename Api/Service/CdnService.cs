using System;
using Api.Data.Models;

namespace Api.Service
{
    public class CdnService
    {
        public const string UrlCdn = "https://cdn.outfit.photos"; // bucket : outfit-image 

        public Uri? getCdnPath(Image? image)
        {
            if (image == null)
                return null;
            return new Uri($"{UrlCdn}/{image.Key}");
        }
    }
}