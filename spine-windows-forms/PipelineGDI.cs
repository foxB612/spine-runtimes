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


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spine
{
    /*
     * Define a primitive GDI
     */ 
    public class Primitive
    {
        public Color color;
        public Image image;
        public RectangleF texcoords;
        public int blend;
        public int numVerts;
        public int firstIndex;
        public int primitive;
        public string text;
        public Font font;

        static public int PRIM_DEFAULT = 0;
        static public int PRIM_RECT = 1;
        static public int PRIM_FILL_RECT = 2;
        static public int PRIM_TEXT = 3;


        public void Marshall(PointF[] vertexBuffer, System.Drawing.Graphics gr)
        {
            // The three Point structures represent the upper-left, upper-right, and lower-left corners of the parallelogram. 
            // The fourth point is extrapolated from the first three to form a parallelogram.

            if (primitive == PRIM_TEXT)
            {
                gr.DrawString(text, font, new SolidBrush(color), vertexBuffer[firstIndex]);
            }
            else if (primitive == PRIM_FILL_RECT)
            {
                gr.FillRectangle(new SolidBrush(color), new Rectangle(
                    (int)vertexBuffer[firstIndex + 0].X,
                    (int)vertexBuffer[firstIndex + 0].Y,
                    (int)(vertexBuffer[firstIndex + 1].X - vertexBuffer[firstIndex + 0].X),
                    (int)(vertexBuffer[firstIndex + 1].Y - vertexBuffer[firstIndex + 0].Y))
                    );
            }
            else if (primitive == PRIM_RECT)
            {
                gr.DrawRectangle(new Pen(color), new Rectangle(
                    (int)vertexBuffer[firstIndex + 0].X,
                    (int)vertexBuffer[firstIndex + 0].Y,
                    (int)(vertexBuffer[firstIndex + 1].X - vertexBuffer[firstIndex + 0].X),
                    (int)(vertexBuffer[firstIndex + 1].Y - vertexBuffer[firstIndex + 0].Y))
                    );
            }
            else if (image != null)
            {
                var imageAttr = new ImageAttributes();
                float r = (float)color.R / 255.0f;
                float g = (float)color.G / 255.0f;
                float b = (float)color.B / 255.0f;
                float a = (float)color.A / 255.0f;
                float[][] colorMatrixElements = {
                    new float[] {r,  0,  0,  0, 0},        // red scaling factor of 2
                    new float[] {0,  g,  0,  0, 0},        // green scaling factor of 1
                    new float[] {0,  0,  b,  0, 0},        // blue scaling factor of 1
                    new float[] {0,  0,  0,  a, 0},        // alpha scaling factor of 1
                    new float[] {0,  0,  0,  0, 1}};       // three translations of 0.2
                ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);
                imageAttr.SetColorMatrix(colorMatrix);
                gr.DrawImage(image, vertexBuffer.Skip(firstIndex).Take(3).ToArray(), texcoords, GraphicsUnit.Pixel, imageAttr);
            }
            else
            {
                gr.DrawPolygon(new Pen(color), vertexBuffer.Skip(firstIndex).Take(numVerts).ToArray());
            }
        }
    }
    public class Pipeline
    {
        public List<Primitive> primitives;
        public PointF[] vertexBuffer = new PointF[65536];

        public Primitive NewPrimitive(PointF[] verts)
        {
            Primitive ret = new Primitive();
            ret.firstIndex = firstIndex;
            ret.primitive = 0;
            ret.numVerts = verts.Length;
            foreach (var p in verts)
            {
                vertexBuffer[firstIndex] = p;
                firstIndex++;
            }


            return ret;
        }

        int firstIndex = 0;

        public void Begin()
        {
            primitives = new List<Primitive>();
            firstIndex = 0;
        }
        public void End(System.Drawing.Graphics g)
        {
            foreach (var it in primitives)
            {
                it.Marshall(vertexBuffer, g);
            }
        }

        public void Marshall(Primitive p)
        {
            primitives.Add(p);

        }

        // Wrapper

        public void DrawLine(Pen brush, PointF source, PointF dest)
        {
            PointF[] points = new PointF[2];
            points[0].X = source.X;
            points[0].Y = source.Y;
            points[1].X = dest.X;
            points[1].Y = dest.Y;
            var primitive = NewPrimitive(points);
            primitive.color = brush.Color;
            Marshall(primitive);
        }
        public void DrawLine(Pen brush, Point source, Point dest)
        {
            PointF[] points = new PointF[2];
            points[0].X = source.X;
            points[0].Y = source.Y;
            points[1].X = dest.X;
            points[1].Y = dest.Y;
            var primitive = NewPrimitive(points);
            primitive.color = brush.Color;
            Marshall(primitive);
        }
        public void FillRectangle(Brush brush, Rectangle rect)
        {
            PointF[] points = new PointF[2];
            points[0].X = rect.Left;
            points[0].Y = rect.Top;
            points[1].X = rect.Right;
            points[1].Y = rect.Bottom;
            var primitive = NewPrimitive(points);
            primitive.color = (brush as SolidBrush).Color;
            primitive.primitive = Primitive.PRIM_FILL_RECT;
            Marshall(primitive);
        }

        public void DrawRectangle(Pen brush, Rectangle rect)
        {
            PointF[] points = new PointF[2];
            points[0].X = rect.Left;
            points[0].Y = rect.Top;
            points[1].X = rect.Right;
            points[1].Y = rect.Bottom;
            var primitive = NewPrimitive(points);
            primitive.color = brush.Color;
            primitive.primitive = Primitive.PRIM_RECT;
            Marshall(primitive);


        }

        public void DrawString(string text, Font font, Brush brush, PointF pos)
        {
            if (text == null || text.Length == 0)
            {
                return;
            }
            PointF[] points = new PointF[1];
            points[0] = pos;
            var primitive = NewPrimitive(points);
            primitive.color = (brush as SolidBrush).Color;
            primitive.primitive = Primitive.PRIM_TEXT;
            primitive.font = font;
            primitive.text = text;
            Marshall(primitive);
        }

        public void DrawImage(Image image, Point pos, Color color)
        {
            // upper-left, upper-right, and lower-left
            PointF[] points = new PointF[3];
            points[0].X = pos.X;
            points[0].Y = pos.Y;
            points[1].X = pos.X + image.Width;
            points[1].Y = pos.Y;
            points[2].X = pos.X;
            points[2].Y = pos.Y + image.Height;
            var primitive = NewPrimitive(points);
            primitive.image = image;
            Marshall(primitive);
        }

        public void DrawImage(Image image, Rectangle rect, Color color)
        {
            PointF[] points = new PointF[3];
            points[0].X = rect.X;
            points[0].Y = rect.Y;
            points[1].X = rect.X + rect.Width;
            points[1].Y = rect.Y;
            points[2].X = rect.X;
            points[2].Y = rect.Y + rect.Height;
            var primitive = NewPrimitive(points);
            primitive.image = image;
            primitive.color = color;
            primitive.texcoords = new RectangleF(0, 0, image.Width, image.Height);
            if (color.A > 0)
            {
                Marshall(primitive);
            }
        }


        public void DrawImage(Image image, Rectangle destRect, Rectangle sourceRect, Color color)
        {
            PointF[] points = new PointF[3];
            points[0].X = destRect.X;
            points[0].Y = destRect.Y;
            points[1].X = destRect.X + destRect.Width;
            points[1].Y = destRect.Y;
            points[2].X = destRect.X;
            points[2].Y = destRect.Y + destRect.Height;
            var primitive = NewPrimitive(points);
            primitive.image = image;
            primitive.color = color;
            primitive.texcoords = sourceRect;
            if (color.A > 0)
            {
                Marshall(primitive);
            }
        }

    }

}
