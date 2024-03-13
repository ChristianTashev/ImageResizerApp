using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace ImageResizerApp
{
    public class ConcurrentImageResizer
    {
        public static Bitmap ResizeInParallel(Bitmap sourceBitmap, int targetWidth, int targetHeight)
        {
            Bitmap targetBitmap = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppRgb);
            int srcWidth = sourceBitmap.Width;
            int srcHeight = sourceBitmap.Height;

            BitmapData srcData = sourceBitmap.LockBits(
                new Rectangle(0, 0, srcWidth, srcHeight),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppRgb);

            BitmapData tgtData = targetBitmap.LockBits(
                new Rectangle(0, 0, targetWidth, targetHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppRgb);

            int pixelDepth = Image.GetPixelFormatSize(PixelFormat.Format32bppRgb) / 8;
            byte[] srcPixels = new byte[srcData.Stride * srcHeight];
            byte[] tgtPixels = new byte[tgtData.Stride * targetHeight];

            Marshal.Copy(srcData.Scan0, srcPixels, 0, srcPixels.Length);

            float resizeXRatio = (float)srcWidth / targetWidth;
            float resizeYRatio = (float)srcHeight / targetHeight;

            int cpuCount = Environment.ProcessorCount;
            Thread[] resizeThreads = new Thread[cpuCount];

            for (int threadId = 0; threadId < cpuCount; threadId++)
            {
                int segmentStart = threadId * targetHeight / cpuCount;
                int segmentEnd = (threadId + 1) * targetHeight / cpuCount;

                resizeThreads[threadId] = new Thread(() =>
                {
                    for (int y = segmentStart; y < segmentEnd; y++)
                    {
                        int tgtRowIndex = y * tgtData.Stride;

                        for (int x = 0; x < targetWidth; x++)
                        {
                            int srcX = (int)(x * resizeXRatio);
                            int srcY = (int)(y * resizeYRatio);
                            int srcIndex = (srcY * srcData.Stride) + (srcX * pixelDepth);

                            for (int byteIdx = 0; byteIdx < pixelDepth; byteIdx++)
                            {
                                tgtPixels[tgtRowIndex + (x * pixelDepth) + byteIdx] = srcPixels[srcIndex + byteIdx];
                            }
                        }
                    }
                });

                resizeThreads[threadId].Start();
            }

            WaitForThreadsCompletion(resizeThreads);

            Marshal.Copy(tgtPixels, 0, tgtData.Scan0, tgtPixels.Length);

            sourceBitmap.UnlockBits(srcData);
            targetBitmap.UnlockBits(tgtData);

            return targetBitmap;
        }

        private static void WaitForThreadsCompletion(Thread[] threads)
        {
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }
    }
}
