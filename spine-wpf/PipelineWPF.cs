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
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Spine
{
    /*
     * Define a primitive WPF
     */ 
    public class Primitive
    {
        public Color color;
        public BitmapSource image;
        public Int32Rect texcoords;
        public int blend;
        public int numVerts;
        public int firstIndex;
        public int primitive;        
        public FormattedText formattedText;

        static public int PRIM_DEFAULT = 0;
        static public int PRIM_RECT = 1;
        static public int PRIM_FILL_RECT = 2;
        static public int PRIM_TEXT = 3;


        public void Marshall(Point[] vertexBuffer, DrawingContext gr)
        {   
            
            if (primitive == PRIM_TEXT)
            {
                gr.DrawText(formattedText, vertexBuffer[firstIndex]);
            }
            else if (primitive == PRIM_FILL_RECT)
            {
                var brush = new SolidColorBrush(color);                
                gr.DrawRectangle(brush, null, new Rect(
                    (int)vertexBuffer[firstIndex + 0].X,
                    (int)vertexBuffer[firstIndex + 0].Y,
                    (int)(vertexBuffer[firstIndex + 1].X - vertexBuffer[firstIndex + 0].X),
                    (int)(vertexBuffer[firstIndex + 1].Y - vertexBuffer[firstIndex + 0].Y))
                    );
            }
            else if (primitive == PRIM_RECT)
            {
                
                var pen = new Pen(new SolidColorBrush(color), 1.0);
                gr.DrawRectangle(null, pen, new Rect(
                    (int)vertexBuffer[firstIndex + 0].X,
                    (int)vertexBuffer[firstIndex + 0].Y,
                    (int)(vertexBuffer[firstIndex + 1].X - vertexBuffer[firstIndex + 0].X),
                    (int)(vertexBuffer[firstIndex + 1].Y - vertexBuffer[firstIndex + 0].Y))
                    );
            }
            else if (image != null)
            {
                // The three Point structures represent the upper-left, upper-right, and lower-left corners of the parallelogram. 
                // The fourth point is extrapolated from the first three to form a parallelogram.
                var ptf = vertexBuffer.Skip(firstIndex).Take(3).ToArray();
                var ul = ptf[0];
                var ur = ptf[1];
                var bl = ptf[2];
              
                // Undefeated algo to simulate Windows GDI DrawBitmap with 3 points to WPF DrawImage
            
                var ab = (ur - ul);
                var ac = (bl - ul);
                var subRegion = texcoords == null ? image : new CroppedBitmap(image, texcoords);
                
                // This doesn't colorize
                gr.PushOpacityMask(new SolidColorBrush(color)); 
                

                var angle = Math.Atan2(ab.Y, ab.X) * 180.0F / Math.PI;
                gr.PushTransform(new RotateTransform(angle - 90, ul.X, ul.Y));                    
                var dstRect = new Rect(ul.X, ul.Y, ac.Length, ab.Length);
                
                    
                gr.DrawImage(subRegion, dstRect);
                gr.Pop();
                                   
                
                
                gr.Pop();
            }
            else
            {

                // Generic draw polygon
                StreamGeometry streamGeometry = new StreamGeometry();
                using (StreamGeometryContext geometryContext = streamGeometry.Open())
                {
                    geometryContext.BeginFigure(vertexBuffer[firstIndex], true, true);
                    PointCollection points = new PointCollection();
                    for (int i = 1; i< numVerts; i++)
                    {
                        points.Add(vertexBuffer[i + firstIndex]);
                    }
                                             
                    geometryContext.PolyLineTo(points, true, true);
                }

                gr.DrawGeometry(null, new Pen(new SolidColorBrush(color), 1.0), streamGeometry);
            }
        }
    }
    public class Pipeline
    {
        public List<Primitive> primitives;
        public Point[] vertexBuffer = new Point[65536];

        public Primitive NewPrimitive(Point[] verts)
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
        public void End(DrawingContext g)
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

        public void DrawLine(Pen pen, Point source, Point dest)
        {
            var points = new Point[2];
            points[0].X = source.X;
            points[0].Y = source.Y;
            points[1].X = dest.X;
            points[1].Y = dest.Y;
            var primitive = NewPrimitive(points);
            primitive.color = (pen.Brush as SolidColorBrush).Color;
            Marshall(primitive);
        }
        public void FillRectangle(Brush brush, Rect rect)
        {
            var points = new Point[2];
            points[0].X = rect.Left;
            points[0].Y = rect.Top;
            points[1].X = rect.Right;
            points[1].Y = rect.Bottom;
            var primitive = NewPrimitive(points);
            primitive.color = (brush as SolidColorBrush).Color;
            primitive.primitive = Primitive.PRIM_FILL_RECT;
            Marshall(primitive);
        }

        public void DrawRectangle(Pen pen, Rect rect)
        {
            var points = new Point[2];
            points[0].X = rect.Left;
            points[0].Y = rect.Top;
            points[1].X = rect.Right;
            points[1].Y = rect.Bottom;
            var primitive = NewPrimitive(points);
            primitive.color = ((SolidColorBrush)pen.Brush).Color;      
            primitive.primitive = Primitive.PRIM_RECT;
            Marshall(primitive);


        }

        public void DrawString(FormattedText formattedText, Brush brush, Point pos)
        {
    
            var points = new Point[1];
            points[0] = pos;
            var primitive = NewPrimitive(points);
            primitive.color = (brush as SolidColorBrush).Color;
            primitive.primitive = Primitive.PRIM_TEXT;
            primitive.formattedText = formattedText;            
            Marshall(primitive);
        }

        public void DrawImage(BitmapSource image, Point pos, Color color)
        {
            // upper-left, upper-right, and lower-left
            Point[] points = new Point[3];
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

        public void DrawImage(BitmapSource image, Rect rect, Color color)
        {
            Point[] points = new Point[3];
            points[0].X = rect.X;
            points[0].Y = rect.Y;
            points[1].X = rect.X + rect.Width;
            points[1].Y = rect.Y;
            points[2].X = rect.X;
            points[2].Y = rect.Y + rect.Height;
            var primitive = NewPrimitive(points);
            primitive.image = image;
            primitive.color = color;
            primitive.texcoords = new Int32Rect(0, 0, (int) image.Width, (int)image.Height);
            if (color.A > 0)
            {
                Marshall(primitive);
            }
        }


        public void DrawImage(BitmapSource image, Rect destRect, Int32Rect sourceRect, Color color)
        {
            var points = new Point[3];
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
