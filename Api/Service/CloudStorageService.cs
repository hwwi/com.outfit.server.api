using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Api.Data.Models;
using Api.Extension;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Service
{
    public class CloudStorageService
    {
        private readonly ILogger<CloudStorageService> _logger;
        private readonly IAmazonS3 _amazonS3;

        public CloudStorageService(
            ILogger<CloudStorageService> logger,
            IAmazonS3 amazonS3
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _amazonS3 = amazonS3 ?? throw new ArgumentNullException(nameof(amazonS3));
        }

        public async Task DeleteAsync(params Image[] images)
        {
            foreach (var request in images.GroupBy(x => x.Bucket)
                .Select(x =>
                    new DeleteObjectsRequest {
                        BucketName = x.Key, Objects = x.Select(y => new KeyVersion {Key = y.Key}).ToList()
                    }))
                try
                {
                    await _amazonS3.DeleteObjectsAsync(request);
                }
                catch (DeleteObjectsException e)
                {
                    if (e.Response.DeleteErrors.Count != 0)
                    {
                        var errors = e.Response.DeleteErrors.Select(error =>
                            $"Code: {error.Code}, Key: {error.Key}, Message: {error.Message}");
                        throw new InvalidOperationException($"S3 deleteObjects error -> \n{string.Join("\n", errors)}");
                    }
                }
        }

        public Image CreateFile(IFormFile formFile, string keyWithoutExtension)
        {
            using System.Drawing.Image image = System.Drawing.Image.FromStream(formFile.OpenReadStream());
            // using ImageFormatConverter for extract file format string after ... https://github.com/dotnet/runtime/issues/1011
            return new Image {
                Bucket = "outfit-image",
                Key = $"{keyWithoutExtension}.{image.RawFormat.ToString().ToLowerInvariant()}",
                FileName = formFile.FileName,
                ContentType = formFile.ContentType,
                RawFormat = image.RawFormat.ToString(),
                Orientation = image.GetExifOrientation(),
                Width = image.Width,
                Height = image.Height,
                ItemTags = new List<ItemTag>()
            };
        }

        public async Task UploadAsync(Image file, Stream stream)
        {
            await _amazonS3.UploadObjectFromStreamAsync(file.Bucket, file.Key, stream, null);
        }
    }
}