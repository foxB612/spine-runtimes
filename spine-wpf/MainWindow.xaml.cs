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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace spine_wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        double RefreshRate = 1.0 / 30.0;

        public MainWindow()
        {
            InitializeComponent();
            LoadSkeleton("..\\..\\..\\examples\\spineboy\\export\\spineboy-ess.json",
                      "..\\..\\..\\examples\\spineboy\\export\\spineboy.atlas");

            panel1.Paint = panel1_Paint;            
            panel1.MouseMove = Form1_MouseMove;
            panel1.MouseDown = Form1_MouseDown;
            panel1.MouseWheel = Form1_MouseWheel;

            panel1.Center = new Point(Width / 2, Height);

            timer1 = new System.Windows.Threading.DispatcherTimer();
            timer1.Tick += new System.EventHandler(Tick);
            timer1.Interval = System.TimeSpan.FromMilliseconds(1000.0 * RefreshRate);

            timer1.Start();

        }

        System.Windows.Threading.DispatcherTimer timer1;

        public void panel1_Paint(DrawingContext drawingContext)
        {

            if (skeleton != null)
            {
                pipeline.Begin();
                drawingContext.PushTransform(new TranslateTransform(panel1.Center.X, panel1.Center.Y));
                drawingContext.PushTransform(new ScaleTransform(panel1.Zoom, -panel1.Zoom));
                skeletonAnimation.Apply(skeleton);
                WindowsRender.Draw(skeleton, pipeline);
                pipeline.End(drawingContext);
                drawingContext.Pop();
                drawingContext.Pop();
            }

        }
        

        Pipeline pipeline = new Pipeline();
        Skeleton skeleton;
        Atlas atlas;

        
        AnimationState skeletonAnimation;


        public void LoadSkeleton(string spine_path, string atlas_path)
        {
            var spineFullPath = System.IO.Path.GetFullPath(spine_path);
            var atlasFullPath = System.IO.Path.GetFullPath(atlas_path);
            var aLoader = new ResourceTextureLoader();
            atlas = new Atlas(atlasFullPath, aLoader);
            var mLoader = new ResourceAttachmentLoader(atlas);

            SkeletonData mSkeletonData;

            if (spineFullPath.ToLower().EndsWith(".skel"))
            {
                var json = new SkeletonBinary(mLoader);
                mSkeletonData = json.ReadSkeletonData(spineFullPath);
            }
            else
            {
                var json = new SkeletonJson(mLoader);
                mSkeletonData = json.ReadSkeletonData(spineFullPath);
            }

            skeleton = new Skeleton(mSkeletonData);
            skeletonAnimation = new AnimationState(new AnimationStateData(mSkeletonData));


            skeletonAnimation.SetAnimation(0, mSkeletonData.Animations.ElementAt(0), true);
            skeleton.SetToSetupPose();

            toolStripDropDownButton1.ItemsSource = mSkeletonData.Animations;
            toolStripDropDownButton1.SelectedIndex = 0;


            Form1.Title = "Spine Runtime Windows WPF - " + mSkeletonData.Version;


        }

        public void Form1_MouseWheel(MouseWheelEventArgs e)
        {
            var numberOfTextLinesToMove = e.Delta / 120.0f;
            panel1.Zoom += numberOfTextLinesToMove / 10.0f;

            if (panel1.Zoom < 0.125f)
            {
                panel1.Zoom = 0.125f;
            }
            else if (panel1.Zoom > 8.0)
            {
                panel1.Zoom = 8;
            }

            
            panel1.InvalidateVisual();            

        }

        public void Form1_MouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                panel1.Center = e.GetPosition(null);
                panel1.InvalidateVisual();
            }
        }

        public void Form1_MouseDown(MouseButtonEventArgs e)
        {
            panel1.Center = e.GetPosition(null);
            panel1.InvalidateVisual();
            
        }


        private void OnSelectAnimation(object sender, SelectionChangedEventArgs e)
        {

            var comboBox = e.OriginalSource as ComboBox;
            var index = comboBox.SelectedIndex;
            var list = comboBox.ItemsSource as ExposedList<Animation>;
            skeletonAnimation.SetAnimation(0, list.ElementAt(index).Name, true);
            panel1.InvalidateVisual();
        }

        private void Tick(object sender, System.EventArgs e)
        {
            if (skeleton != null)
            {
                skeletonAnimation.Update((float)RefreshRate);
                skeletonAnimation.Apply(skeleton);
                panel1.InvalidateVisual();
            }
        }
    }
}