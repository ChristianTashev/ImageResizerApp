using System.Drawing.Imaging;
using System.Drawing;
using System;

namespace ImageResizerApp
{
    public class ImageResizer
    {
        public static Bitmap ResizeImage(Bitmap sourceImage, double resizeFactor)
        {
            // Изчисляване на новите размери на изображението
            int sourceWidth = sourceImage.Width;
            int sourceHeight = sourceImage.Height;
            int resizedWidth = (int)(sourceWidth * resizeFactor);
            int resizedHeight = (int)(sourceHeight * resizeFactor);

            Bitmap resizedBitmap = new Bitmap(resizedWidth, resizedHeight, PixelFormat.Format32bppArgb);

            BitmapData sourceData = sourceImage.LockBits(
                new Rectangle(0, 0, sourceWidth, sourceHeight),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            BitmapData resizedData = resizedBitmap.LockBits(
                new Rectangle(0, 0, resizedWidth, resizedHeight),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int bytesPerPix = 4;

            unsafe
            {
                byte* ptrSourcePix = (byte*)sourceData.Scan0;
                byte* ptrResizedPix = (byte*)resizedData.Scan0;

                for (int y = 0; y < resizedHeight; y++)
                {
                    for (int x = 0; x < resizedWidth; x++)
                    {
                        int origX = (int)(x / resizeFactor);
                        int origY = (int)(y / resizeFactor);

                        int widthBoundary = sourceWidth - origX;
                        int heightBoundary = sourceHeight - origY;
                        int resizeBoundary = (int)Math.Ceiling(1 / resizeFactor);

                        int effectiveWidth = Math.Min(resizeBoundary, widthBoundary);
                        int effectiveHeight = Math.Min(resizeBoundary, heightBoundary);

                        long totalRed = 0, totalGreen = 0, totalBlue = 0, totalAlpha = 0;

                        for (int newY = 0; newY < effectiveHeight; newY++)
                        {
                            int offsetY = origY + newY;
                            byte* sourceRow = ptrSourcePix + (offsetY * sourceData.Stride);

                            for (int newX = 0; newX < effectiveWidth; newX++)
                            {
                                int offsetX = origX + newX;
                                byte* currentPixel = sourceRow + (offsetX * bytesPerPix);

                                totalBlue += currentPixel[0];
                                totalGreen += currentPixel[1];
                                totalRed += currentPixel[2];
                                totalAlpha += currentPixel[3];
                            }
                        }

                        int pixelArea = effectiveWidth * effectiveHeight;
                        byte averageRed = (byte)(totalRed / pixelArea);
                        byte averageGreen = (byte)(totalGreen / pixelArea);
                        byte averageBlue = (byte)(totalBlue / pixelArea);
                        byte averageAlpha = (byte)(totalAlpha / pixelArea);

                        byte* resizedRow = ptrResizedPix + (y * resizedData.Stride);
                        byte* resizedPixel = resizedRow + (x * bytesPerPix);

                        resizedPixel[0] = averageBlue;
                        resizedPixel[1] = averageGreen;
                        resizedPixel[2] = averageRed;
                        resizedPixel[3] = averageAlpha;
                    }
                }
            }

            sourceImage.UnlockBits(sourceData);
            resizedBitmap.UnlockBits(resizedData);

            return resizedBitmap;
        }
    }
}
