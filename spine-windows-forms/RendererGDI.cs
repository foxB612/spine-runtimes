/******************************************************************************
 * Spine Runtimes Software License v2.5
 *
 * Copyright (c) 2013-2016, Esoteric Software
 * All rights reserved.
 *
 * You are granted a perpetual, non-exclusive, non-sublicensable, and
 * non-transferable license to use, install, execute, and perform the Spine
 * Runtimes software and derivative works solely for personal or internal
 * use. Without the written permission of Esoteric Software (see Section 2 of
 * the Spine Software License Agreement), you may not (a) modify, translate,
 * adapt, or develop new applications using the Spine Runtimes or otherwise
 * create derivative works or improvements of the Spine Runtimes or (b) remove,
 * delete, alter, or obscure any trademarks or any copyright, trademark, patent,
 * or other intellectual property or proprietary rights notices on or in the
 * Software, including any copy thereof. Redistributions in binary or source
 * form must include this license and terms.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE "AS IS" AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 * EVENT SHALL ESOTERIC SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS INTERRUPTION, OR LOSS OF
 * USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
 * IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 * *******************************************************************************/
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Spine
{
    /*!
     *   Basic texture loader, loading GDI Bitmap
     */
    class ResourceTextureLoader : TextureLoader
    {
        public void Load(AtlasPage page, string path)
        {
            Bitmap b = new Bitmap(path);
            BitmapData bd = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), 
                            ImageLockMode.ReadOnly, b.PixelFormat);
            // convert format from ARGB to PARGB without modifying the actual data
            Bitmap pb = new Bitmap(b.Width, b.Height, PixelFormat.Format32bppPArgb);
            BitmapData pbd = pb.LockBits(new Rectangle(0, 0, pb.Width, pb.Height),
                            ImageLockMode.ReadWrite, pb.PixelFormat);
            IntPtr ptr = bd.Scan0;
            int bytes = Math.Abs(bd.Stride) * b.Height;
            byte[] argbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, argbValues, 0, bytes);
            System.Runtime.InteropServices.Marshal.Copy(argbValues, 0, pbd.Scan0, bytes);
            pb.UnlockBits(pbd);

            pb.SetResolution(96, 96);
            page.rendererObject = pb;
        }
        public void Unload(Object texture)
        {
            texture = null;
        }
    }
   /*!
   *   Basic resource attachment loader
   */
    class ResourceAttachmentLoader : AttachmentLoader
    {
        Atlas mAtlas;
        public ResourceAttachmentLoader(Atlas atlas)
        {
            mAtlas = atlas;
        }
        /// <return>May be null to not load any attachment.</return>
        public RegionAttachment NewRegionAttachment(Skin skin, string name, string path)
        {
            var attachment = new RegionAttachment(name);
            var region = mAtlas.FindRegion(name);
            // if (region == null) throw new Error("Region not found in atlas: " + name);
            attachment.RendererObject = region;
            return attachment;
        }
        /// <return>May be null to not load any attachment.</return>
        public MeshAttachment NewMeshAttachment(Skin skin, string name, string path)
        {
            var attachment = new MeshAttachment(name);
            return attachment;
        }
        /// <return>May be null to not load any attachment.</return>
        public BoundingBoxAttachment NewBoundingBoxAttachment(Skin skin, string name)
        {
            var attachment = new BoundingBoxAttachment(name);
            return attachment;
        }
        /// <returns>May be null to not load any attachment</returns>
        public PathAttachment NewPathAttachment(Skin skin, string name)
        {
            var attachment = new PathAttachment(name);
            return attachment;
        }
    }
    /*
     * Undefeated Windows Forms renderer
     */
    public class GdiRenderer
    {
         /*
          * Draw as polygon 
          */ 
        public static void DrawAttachmnentPolygon(Pipeline pipeline, AtlasRegion region, float[] worldVertices, Color color, BlendMode blend)
        {
            var destPoints = new PointF[worldVertices.Length / 2];
            for (int i = 0; i < worldVertices.Length / 2; i++)
            {
                destPoints[i].X = worldVertices[i * 2 + 0];
                destPoints[i].Y = worldVertices[i * 2 + 1];
            }

            var primitive = pipeline.NewPrimitive(destPoints);
            primitive.color = color;
            primitive.blend = (int)blend;
            pipeline.Marshall(primitive);
        }
        /*
         * Draw attachment
         */
        public static void DrawRegionAttachmnent(Pipeline pipeline, AtlasRegion region, float[] worldVertices, Color color, BlendMode blend)
        {
            var bitmap = (Image)region.page.rendererObject;
            // Extract region
            var target = Gdi.ExtractRegion(region, bitmap);

            var destPoints = new PointF[3];
            // The three Point structures represent the upper-left, upper-right, and lower-left corners of the parallelogram. 
            // The fourth point is extrapolated from the first three to form a parallelogram.
            destPoints[0].X = worldVertices[RegionAttachment.X2];
            destPoints[0].Y = worldVertices[RegionAttachment.Y2];

            destPoints[1].X = worldVertices[RegionAttachment.X3];
            destPoints[1].Y = worldVertices[RegionAttachment.Y3];

            destPoints[2].X = worldVertices[RegionAttachment.X1];
            destPoints[2].Y = worldVertices[RegionAttachment.Y1];



            // Create new primitive
            var primitive = pipeline.NewPrimitive(destPoints);
            primitive.color = color;
            primitive.image = target;
            primitive.blend = (int)blend;
            primitive.texcoords = new RectangleF(0, 0, target.Width, target.Height);
            pipeline.Marshall(primitive);
        }

        public static void DrawMeshAttachmnent(Pipeline pipeline, AtlasRegion region, float[] regionUVs, float[] worldVertices, int[] triangles, Color color, BlendMode blend)
        {
            float worldMinX = float.MaxValue;
            float worldMaxX = float.MinValue;
            float worldMinY = float.MaxValue;
            float worldMaxY = float.MinValue;
            for (int i = 0; i < worldVertices.Length; i += 2)
            {
                if (worldMinX > worldVertices[i])
                    worldMinX = worldVertices[i];
                if (worldMaxX < worldVertices[i])
                    worldMaxX = worldVertices[i];
            }
            for (int i = 0; i < worldVertices.Length; i += 2)
            {
                if (worldMinY > worldVertices[i + 1])
                    worldMinY = worldVertices[i + 1];
                if (worldMaxY < worldVertices[i + 1])
                    worldMaxY = worldVertices[i + 1];
            }
            int minX = (int)Math.Round(worldMinX);
            int maxX = (int)Math.Round(worldMaxX);
            int minY = (int)Math.Round(worldMinY);
            int maxY = (int)Math.Round(worldMaxY);

            Image bitmap = (Image)region.page.rendererObject;
            //// Extract region
            Bitmap img = Gdi.ExtractRegion(region, bitmap);
            Bitmap result = new Bitmap(maxX - minX + 3, maxY - minY + 3, PixelFormat.Format32bppPArgb);
            BitmapData imgData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), 
                ImageLockMode.ReadOnly, img.PixelFormat);
            BitmapData resultData = result.LockBits(new Rectangle(0, 0, result.Width, result.Height),
                ImageLockMode.ReadWrite, result.PixelFormat);


            //Graphics e = Graphics.FromImage(result);
            //e.Clear(Color.Transparent);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Point[] srcP = new Point[3];
                Point[] destP = new Point[3];
                for (int j = 0; j < 3; j++)
                {
                    srcP[j] = new Point((int)Math.Round(regionUVs[triangles[i + j] * 2] * img.Width),
                        (int)Math.Round(regionUVs[triangles[i + j] * 2 + 1] * img.Height));
                    destP[j] = new Point((int)Math.Round(worldVertices[triangles[i + j] * 2]) - minX,
                        (int)Math.Round(worldVertices[triangles[i + j] * 2 + 1]) - minY);
                }

                //e.DrawPolygon(new Pen(Brushes.Black), destP);
                Gdi.TriangleTransform(imgData, resultData, srcP, destP);
            }
            result.UnlockBits(resultData);
            img.UnlockBits(imgData);
            var primitive = pipeline.NewPrimitive(new PointF[] {new PointF(minX, minY),
                                    new PointF(maxX, minY), new PointF(minX, maxY) });
            primitive.color = color;
            primitive.image = result;
            primitive.blend = (int)blend;
            primitive.texcoords = new RectangleF(0, 0, result.Width, result.Height);
            pipeline.Marshall(primitive);

        }

        /*
         *  Actual draw. Pipeline is 'deferred' list of GDI calls
         */
        public static void Draw(Skeleton skeleton, Pipeline pipeline)
        {
            skeleton.UpdateWorldTransform();

            foreach (Slot slot in skeleton.Slots)
            {
                if (slot.Attachment == null)
                    continue;
                if (slot.Attachment.GetType() == typeof(RegionAttachment))
                {
                    RegionAttachment attach = (RegionAttachment)slot.Attachment;
                    AtlasRegion region = (AtlasRegion)attach.RendererObject;
                    float[] worldVertices = new float[2 * 4];
                    attach.ComputeWorldVertices(slot.Bone, worldVertices);
                    var color = Color.FromArgb((int)(slot.A * 255), (int)(slot.R * 255), (int)(slot.G * 255), (int)(slot.B * 255));
                    DrawRegionAttachmnent(pipeline, region, worldVertices, color, slot.Data.BlendMode);
                }
                else if (slot.Attachment.GetType() == typeof(MeshAttachment))
                {
                    MeshAttachment attach = (MeshAttachment)slot.Attachment;
                    AtlasRegion region = (AtlasRegion)attach.RendererObject;
                    float[] worldVertices = new float[attach.WorldVerticesLength];
                    attach.ComputeWorldVertices(slot, worldVertices);
                    int[] triangles = attach.Triangles;
                    var color = Color.FromArgb((int)(slot.A * 255), (int)(slot.R * 255), (int)(slot.G * 255), (int)(slot.B * 255));
                    DrawMeshAttachmnent(pipeline, region, attach.RegionUVs, worldVertices, triangles, color, slot.Data.BlendMode);
                }
                else
                {

                }
            }

        }
    }
}
