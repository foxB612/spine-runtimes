using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace spine_wpf
{
    /// <summary>
    /// Interaction logic for FlickerFreePanel.xaml
    /// </summary>
    public partial class FlickerFreePanel : UserControl
    {
        public delegate void PaintFunc(DrawingContext drawingContext);
        public delegate void MouseMoveFunc(MouseEventArgs e);
        public delegate void MouseButtonFunc(MouseButtonEventArgs e);
        public delegate void MouseWheelFunc(MouseWheelEventArgs e);

        public PaintFunc Paint;
        public new MouseMoveFunc MouseMove;
        public new MouseButtonFunc MouseDown;
        public new MouseWheelFunc MouseWheel;
        

        public FlickerFreePanel() 
        {
            InitializeComponent();
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            DrawAsBluePrint(drawingContext);
            if (Paint != null)
            {
                Paint(drawingContext);
            }
        }

      
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (MouseMove!=null)
            {
                MouseMove(e);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (MouseDown != null)
            {
                MouseDown(e);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (MouseWheel != null)
            {
                MouseWheel(e);
            }
        }



        public Point Center = new Point();
        public float Zoom = 1.0F;

        void DrawAsBluePrint(DrawingContext g)
        {
            var BackColor = Color.FromArgb(255, 16, 79, 140);
            g.DrawRectangle(new SolidColorBrush(BackColor), (System.Windows.Media.Pen)null, new Rect(0, 0, Width, Height));

            g.PushTransform(new TranslateTransform(Center.X, Center.Y));
            g.PushTransform(new ScaleTransform(Zoom, Zoom));
            int cellSize = (int)(50);

            var left = -Center.X / Zoom;
            var top = -Center.Y / Zoom;
            var bottom = top + Height / Zoom;
            var right = left + Width / Zoom;

            var w = Width;
            var h = Height;

            Pen p = new Pen(new SolidColorBrush(Color.FromArgb(255, 77, 145, 206)), 2.0);



            for (int y = (int)(top / cellSize); y <= (int)(bottom / cellSize); ++y)
            {
                var z = y * cellSize;
                g.DrawLine(p, new Point(left, z),
                              new Point(right, z));
            }

            for (int x = (int)(left / cellSize); x <= (int)(right / cellSize); ++x)
            {
                var z = x * cellSize;
                g.DrawLine(p, new Point(z, top),
                              new Point(z, bottom));
            }


            g.Pop();
            g.Pop();

          
        }
    }
}
