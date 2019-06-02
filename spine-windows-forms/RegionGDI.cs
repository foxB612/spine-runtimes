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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            if (region.rotate)
            {
                // swap width, height, X, Y
                target = new Bitmap(region.originalHeight, region.originalWidth);
                using (Graphics g2 = Graphics.FromImage(target))
                {
                    /* Pixels stripped from the bottom left, unrotated. */

                    g2.DrawImage(bitmap, new RectangleF(region.originalHeight - (region.height + region.offsetY),
                                                        region.originalWidth - (region.width + region.offsetX), region.height, region.width),
                                  new RectangleF(region.x, region.y, region.height, region.width),
                                  GraphicsUnit.Pixel);
                    target.RotateFlip(RotateFlipType.Rotate90FlipNone);
                }

            }
            else
            {
                target = new Bitmap(region.originalWidth, region.originalHeight);
                using (Graphics g2 = Graphics.FromImage(target))
                {
                    /* Pixels stripped from the bottom left, unrotated. */

                    g2.DrawImage(bitmap, new RectangleF(region.offsetX, region.originalHeight - (region.height + region.offsetY), region.width, region.height),
                                     new RectangleF(region.x, region.y, region.width, region.height),
                                     GraphicsUnit.Pixel);


                }
            }

            Caches.Add(region, target);

            return target;
        }

    }
}
