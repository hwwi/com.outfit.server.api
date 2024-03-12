using System.Drawing;
using System.Linq;
using Api.Data;

namespace Api.Extension
{
    public static class ImageExtensions
    {
        public static ExifOrientation GetExifOrientation(this Image image)
        {
            if (!image.PropertyIdList.Contains(0x0112))
                return ExifOrientation.Undefined;
            return (ExifOrientation)image.GetPropertyItem(0x0112).Value[0];
        }

        public static float GetAspectRatio(this Image image) =>
            image.GetExifOrientation().GetAspectRatio(image.Width, image.Height);

        public static float GetAspectRatio(this ExifOrientation orientation, int width, int height)
        {
            float aspectRatio;
            switch (orientation)
            {
                case ExifOrientation.Rotate90:
                case ExifOrientation.Transverse:
                case ExifOrientation.Rotate270:
                case ExifOrientation.Transpose:
                    aspectRatio = height / (float)width;
                    break;
                default:
                    aspectRatio = width / (float)height;
                    break;
            }

            return aspectRatio;
        }
    }
}