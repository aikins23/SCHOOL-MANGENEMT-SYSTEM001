using System;
using System.IO;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Image utility class for handling photo uploads
    /// </summary>
    public static class ImageHelper
    {
        public static byte[] ConvertImageToBytes(string imagePath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");

            var fileInfo = new FileInfo(imagePath);
            if (fileInfo.Length > AppConfig.MaxPhotoSizeBytes)
                throw new InvalidOperationException($"Image size exceeds maximum allowed size of {AppConfig.MaxPhotoSizeMB}MB");

            var ext = Path.GetExtension(imagePath).ToLower();
            if (Array.IndexOf(AppConfig.AllowedImageExtensions, ext) < 0)
                throw new InvalidOperationException($"Image format not supported. Allowed formats: {string.Join(", ", AppConfig.AllowedImageExtensions)}");

            return File.ReadAllBytes(imagePath);
        }

        public static System.Drawing.Image BytesToImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            using (var ms = new MemoryStream(imageBytes))
            {
                return System.Drawing.Image.FromStream(ms);
            }
        }

        public static byte[] ImageToBytes(System.Drawing.Image image)
        {
            if (image == null)
                return null;

            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }
}
