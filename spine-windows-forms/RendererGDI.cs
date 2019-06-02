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
using Spine;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Spine
{
    /*!
     *   Basic texture loader, loading GDI Bitmap
     */ 
    class ResourceTextureLoader : TextureLoader
    {
        public void Load(AtlasPage page, string path)
        {
            var b = new Bitmap(path);
            b.SetResolution(96, 96);            
            page.rendererObject = b;
        }
        public void Unload(Object texture)
        {
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
        public PointAttachment NewPointAttachment(Skin skin, string name)
        {
            var attachment = new PointAttachment(name);
            return attachment;
        }
        public ClippingAttachment NewClippingAttachment(Skin skin, string name)
        {
            var attachment = new ClippingAttachment(name);
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
        public static void DrawAttachmnent(Pipeline pipeline, AtlasRegion region, float[] worldVertices, Color color, BlendMode blend)
        {
            var bitmap = (Image)region.page.rendererObject;
            var destPoints = new PointF[3];

            // The three Point structures represent the upper-left, upper-right, and lower-left corners of the parallelogram. 
            // The fourth point is extrapolated from the first three to form a parallelogram.
              
            destPoints[0].X = worldVertices[RegionAttachment.ULX+2];
            destPoints[0].Y = worldVertices[RegionAttachment.ULY+2];

            destPoints[1].X = worldVertices[RegionAttachment.URX+2];
            destPoints[1].Y = worldVertices[RegionAttachment.URY+2];

            destPoints[2].X = worldVertices[RegionAttachment.BLX+2];
            destPoints[2].Y = worldVertices[RegionAttachment.BLY+2];

            // Extract region
            var target = Gdi.ExtractRegion(region, bitmap);

            // Create new primitive
            var primitive = pipeline.NewPrimitive(destPoints);
            primitive.color = color;
            primitive.image = target;
            primitive.blend = (int)blend;
            primitive.texcoords = new RectangleF(0, 0, target.Width, target.Height);
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
                // Only draw region attachment are supported here
                if (slot.Attachment == null || slot.Attachment.GetType() != typeof(RegionAttachment))
                    continue;
                RegionAttachment attach = (RegionAttachment)slot.Attachment;
                AtlasRegion region = (AtlasRegion)attach.RendererObject;
                float[] worldVertices = new float[2 * 4];
                attach.ComputeWorldVertices(slot.Bone, worldVertices, 0);
                var color = Color.FromArgb((int)(slot.A * 255), (int)(slot.R * 255), (int)(slot.G * 255), (int)(slot.B * 255));
                DrawAttachmnent(pipeline, region, worldVertices, color, slot.Data.BlendMode);
            }

        }
    }
}
