using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpineWindowsForms
{
    class FlickerFreePanel : Panel
    {
        public PointF Center = new PointF();
        public float Zoom = 1.0f;
        public bool BluePrint = true;

        public FlickerFreePanel()
        {
            this.SetStyle(
   ControlStyles.AllPaintingInWmPaint |
   ControlStyles.UserPaint |
   ControlStyles.OptimizedDoubleBuffer |
   ControlStyles.DoubleBuffer,
   true);
        }


        protected override void OnPaint(PaintEventArgs pe)
        {
            DrawAsBluePrint(pe);
            base.OnPaint(pe);
        }

        void DrawAsBluePrint(PaintEventArgs pe)
        {

            this.BackColor = Color.FromArgb(255, 16, 79, 140);
            var g = pe.Graphics;
            g.ResetTransform();
            g.TranslateTransform(Center.X, Center.Y);            
            g.ScaleTransform(Zoom, Zoom);

            var cellSize = (int)(50);
            var left = -Center.X / Zoom;
            var top = -Center.Y / Zoom;
            var bottom = top + Height / Zoom;
            var right= left + Width / Zoom;
            var w = Width;
            var h = Height;
            var p = new Pen(Color.FromArgb(255, 77, 145, 206));

            for (var y = (int)(top / cellSize); y <= (int)(bottom / cellSize); ++y)
            {
                var z = y * cellSize;
                g.DrawLine(p, new PointF(left, z),
                              new PointF(right, z));
            }

            for (var x = (int)(left / cellSize); x <= (int)(right / cellSize); ++x)
            {
                var z = x * cellSize;
                g.DrawLine(p, new PointF(z, top),
                              new PointF(z, bottom));
            }

            


        }
    }

}
