/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated May 1, 2019. Replaces all prior versions.
 *
 * Copyright (c) 2013-2019, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
 * NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS
 * INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/


using Spine;
using System.Collections.Generic;
using System.Drawing;
using MatrixCalculation;
using System;
using System.Drawing.Imaging;

namespace Spine
{
    class Gdi
    {
        /*
         * Spine exports texture atlases using the libgdx atlas format.
         * his is a simple, line-based text format made up of one or more page entries,
         * each with any number of region entries. 
         * 
         * Page entries are separated by a blank line. Here is an example atlas with 2 pages:
         */

        static public Dictionary<AtlasRegion, Bitmap> Caches = new Dictionary<AtlasRegion, Bitmap>();
        static public Bitmap ExtractRegion(AtlasRegion region, Image bitmap)
        {

            Bitmap target = null;

            // Re-use cache
            if (Caches.ContainsKey(region))
            {
                return Caches[region];
            }

            RectangleF dstRect;
            RectangleF srcRect;

            // Subregion
            if (region.rotate)
            {
                dstRect = new RectangleF(region.originalHeight - (region.height + region.offsetY), region.originalWidth - (region.width + region.offsetX), region.height, region.width);
                srcRect = new RectangleF(region.x, region.y, region.height, region.width);
            }
            else
            {
                dstRect = new RectangleF(region.offsetX, region.originalHeight - (region.height + region.offsetY), region.width, region.height);
                srcRect = new RectangleF(region.x, region.y, region.width, region.height);
            }


            if (region.rotate)
            {
                // swap width, height, X, Y
                target = new Bitmap(region.originalHeight, region.originalWidth);
              
                using (var g2 = Graphics.FromImage(target))
                {
                    /* Pixels stripped from the bottom left, unrotated. */
                  
                    g2.DrawImage(bitmap, dstRect, srcRect, GraphicsUnit.Pixel);
                    target.RotateFlip(RotateFlipType.Rotate90FlipNone);
                }
            }
            else
            {
                target = new Bitmap(region.originalWidth, region.originalHeight);

                using (Graphics g2 = Graphics.FromImage(target))
                {
                    /* Pixels stripped from the bottom left, unrotated. */
                    g2.DrawImage(bitmap, dstRect, srcRect, GraphicsUnit.Pixel);
                }
            }

            Caches.Add(region, target);

            return target;
        }

        /// <summary>
        /// Get the required triangular region from the srcImg, then draw it at required location on destImg
        /// </summary>
        /// <param name="srcImg"></param>
        /// <param name="destImg"></param>
        /// <param name="srcPoints"></param>
        /// <param name="destPoints"></param>
        public static void TriangleTransform(BitmapData srcImg, BitmapData destImg, Point[] srcPoints, Point[] destPoints)
        {
            if (srcPoints.Length != 3 || destPoints.Length != 3)
                throw new System.Exception("Number of vertices of a triangle should be 3.");
            // sort dest point arrays
            for (int i = 0; i < destPoints.Length - 1; i++)
            {
                for (int j = i + 1; j < destPoints.Length; j++)
                {
                    if (destPoints[i].Y > destPoints[j].Y)
                    {
                        Point temp = destPoints[i];
                        destPoints[i] = destPoints[j];
                        destPoints[j] = temp;
                        temp = srcPoints[i];
                        srcPoints[i] = srcPoints[j];
                        srcPoints[j] = temp;
                    }
                }
            }
            double[][] original = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                original[i] = new double[3];
            }
            for (int i = 0; i < 3; i++)
            {
                original[0][i] = srcPoints[i].X;
                original[1][i] = srcPoints[i].Y;
                original[2][i] = 1;
            }

            double[][] after = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                after[i] = new double[3];
            }
            for (int i = 0; i < 3; i++)
            {
                after[0][i] = destPoints[i].X;
                after[1][i] = destPoints[i].Y;
                after[2][i] = 1;
            }

            //calculate transformation matrix
            double[][] Ap = Matrix.MatrixProduct(original, Matrix.InverseMatrix(after));
            if (Ap == null) return;
            if (destPoints[0].Y == destPoints[1].Y)
            {
                Scan(srcImg, destImg, destPoints, Ap, 0);
            }
            else if (destPoints[1].Y == destPoints[2].Y)
            {
                Scan(srcImg, destImg, destPoints, Ap, 1);
            }
            else
            {
                //split the triangle into two horizontal triangles

                int x1 = destPoints[0].X;
                int y1 = destPoints[0].Y;
                int y2 = destPoints[1].Y;
                int x3 = destPoints[2].X;
                int y3 = destPoints[2].Y;

                Point newDestP = new Point((x1 - x3) * (y2 - y3) / (y1 - y3) + x3, y2);

                Scan(srcImg, destImg, new Point[] { destPoints[0], destPoints[1], newDestP }, Ap, 1);

                Scan(srcImg, destImg, new Point[] { destPoints[1], newDestP, destPoints[2] }, Ap, 0);
            }
        }

        /// <summary>
        /// Scan by honrizomtal line
        /// </summary>
        /// <param name="type"> 0: upper, 1: lower </param>
        private static void Scan(BitmapData srcImg, BitmapData destImg, Point[] destPoints, double[][] Ap, int type)
        {
            int x1 = destPoints[0].X;
            int y1 = destPoints[0].Y;
            int x2 = destPoints[1].X;
            int y2 = destPoints[1].Y;
            int x3 = destPoints[2].X;
            int y3 = destPoints[2].Y;
            if (type == 0)
            {
                for (int y = destPoints[0].Y; y <= destPoints[2].Y; y++)
                {
                    int startX = (y - y3) * (x1 - x3) / (y1 - y3) + x3;
                    int endX = (y - y3) * (x2 - x3) / (y2 - y3) + x3;
                    if (endX < startX)
                    {
                        int temp = endX;
                        endX = startX;
                        startX = temp;
                    }
                    DrawLine(srcImg, destImg, startX, endX, y, Ap);
                }
            }
            else
            {
                for (int y = destPoints[2].Y; y >= destPoints[0].Y; y--)
                {
                    int startX = (y - y3) * (x1 - x3) / (y1 - y3) + x3;
                    int endX = (y - y2) * (x1 - x2) / (y1 - y2) + x2;
                    if (endX < startX)
                    {
                        int temp = endX;
                        endX = startX;
                        startX = temp;
                    }
                    DrawLine(srcImg, destImg, startX, endX, y, Ap);
                }
            }
        }

        private static void DrawLine(BitmapData srcImg, BitmapData destImg, int startX, int endX, int y, double[][] Ap)
        {
            double[][] destPVector = new double[3][];
            for (int i = 0; i < 3; i++)
            {
                destPVector[i] = new double[] { 0 };
            }
            destPVector[0][0] = startX;
            destPVector[1][0] = y;
            destPVector[2][0] = 1;
            // simplified matrix production
            double[,] srcPVector = new double[3, 1];
            srcPVector[0, 0] = Ap[0][0] * destPVector[0][0] + Ap[0][1] * destPVector[1][0] + Ap[0][2] * destPVector[2][0];
            srcPVector[1, 0] = Ap[1][0] * destPVector[0][0] + Ap[1][1] * destPVector[1][0] + Ap[1][2] * destPVector[2][0];
            srcPVector[2, 0] = destPVector[2][0];

            for (int x = startX; x <= endX; x++)
            {
                unsafe
                {
                    byte* a = (byte*)destImg.Scan0;
                    a += y * destImg.Stride + x * 4;
                    *(uint*)a = GetPixelArgb(srcImg, srcPVector[0, 0], srcPVector[1, 0]);
                }
                srcPVector[0, 0] += Ap[0][0];
                srcPVector[1, 0] += Ap[1][0];
                // Ap[2][0] must be 0
                //srcPVector[2][0] += Ap[2][0];
            }
        }

        private static uint GetPixelArgb(BitmapData srcImg, double xf, double yf)
        {
            int x1 = (int)Math.Floor(xf);
            int x2 = (int)Math.Ceiling(xf);
            int y1 = (int)Math.Floor(yf);
            int y2 = (int)Math.Ceiling(yf);

            if (x1 < 0) return 0;
            if (x2 >= srcImg.Width) return 0;
            if (y1 < 0) return 0;
            if (y2 >= srcImg.Height) return 0;

            if (x2 == x1)
            {
                if (x1 + 1 >= srcImg.Width)
                    x1--;
                else
                    x2++;
            }
            if (y2 == y1)
            {
                if (y1 + 1 >= srcImg.Width)
                    y1--;
                else
                    y2++;
            }

            unsafe
            {
                byte* ptr = (byte*)srcImg.Scan0;
                ptr += y1 * srcImg.Stride + x1 * 4;

                int xu = (int)((x2 - xf) * 2048);
                int xb = (int)((xf - x1) * 2048);
                int yu = (int)((y2 - yf) * 2048);
                int yb = (int)((yf - y1) * 2048);

                int bx = ((*(ptr + 0)) * xu + (*(ptr + 4)) * xb);
                int gx = ((*(ptr + 1)) * xu + (*(ptr + 5)) * xb);
                int rx = ((*(ptr + 2)) * xu + (*(ptr + 6)) * xb);
                int ax = ((*(ptr + 3)) * xu + (*(ptr + 7)) * xb);

                ptr += srcImg.Stride;
                int by = ((*(ptr + 0)) * xu + (*(ptr + 4)) * xb);
                int gy = ((*(ptr + 1)) * xu + (*(ptr + 5)) * xb);
                int ry = ((*(ptr + 2)) * xu + (*(ptr + 6)) * xb);
                int ay = ((*(ptr + 3)) * xu + (*(ptr + 7)) * xb);

                byte b = (byte)((bx * yu + by * yb) >> 22);
                byte g = (byte)((gx * yu + gy * yb) >> 22);
                byte r = (byte)((rx * yu + ry * yb) >> 22);
                byte a = (byte)((ax * yu + ay * yb) >> 22);

                return (uint)(a << 24 | r << 16 | g << 8 | b);
            }
        }

    }
}
