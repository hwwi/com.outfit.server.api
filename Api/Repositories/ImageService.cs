using System;
using System.Collections.Generic;
using System.Linq;
using Api.Data;
using Api.Data.Models;

namespace Api.Repositories
{
    public class ImageService
    {
        private const float Correction = 0.005f;

        private readonly List<(SupportedAspectRatio, float)> _supportedAspectRatioRates =
            new List<(SupportedAspectRatio, float)> {
                (SupportedAspectRatio.Proportion4Over5, 0.8f),
                (SupportedAspectRatio.Proportion1Over1, 1f),
                (SupportedAspectRatio.Proportion5Over4, 1.25f),
                (SupportedAspectRatio.Proportion1Dot91Over1, 1.91f)
            };

        public SupportedAspectRatio? getSupportedAspectRatio(Image image)
        {
            float aspectRatio = image.AspectRatio;
            try
            {
                var (supportedAspectRatio, _) = _supportedAspectRatioRates
                    .First(y =>
                        y.Item2 - Correction <= aspectRatio && aspectRatio <= y.Item2 + Correction
                    );
                return supportedAspectRatio;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public List<SupportedAspectRatio> getSupportedAspectRatio(
            IEnumerable<Image> images,
            IEnumerable<SupportedAspectRatio> targetRatios
        ) => images.Select(getSupportedAspectRatio)
            .Where(x => x != null && targetRatios.Contains(x.Value))
            .Cast<SupportedAspectRatio>()
            .Distinct()
            .ToList();
    }
}