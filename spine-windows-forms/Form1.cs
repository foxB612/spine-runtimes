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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpineWindowsForms
{
    public partial class Form1 : Form
    {
        Pipeline pipeline = new Pipeline();
        Skeleton skeleton;
        Atlas atlas;

        PointF Center = new PointF(0, 0);
        float Zoom = 0.50F;
        AnimationState skeletonAnimation;
        public Form1()
        {
            InitializeComponent();
            this.MouseWheel += new MouseEventHandler(this.OnMouseWheel);

            // Run from F5
            LoadSkeleton("..\\..\\..\\examples\\spineboy\\export\\spineboy-ess.json",                         
                         "..\\..\\..\\examples\\spineboy\\export\\spineboy.atlas");
            
        }

        void LoadSkeleton(string spine_path, string atlas_path)
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


            /* Add animation list */
            toolStripDropDownButton1.DropDownItems.Clear();
            foreach (var a in mSkeletonData.Animations)
            {
                var item = toolStripDropDownButton1.DropDownItems.Add(a.Name);
                item.Click += OnSelectAnimation;
            }

            /* Update 30 fps */
            timer1.Interval = 1000 / 30;
            timer1.Start();

            Center = new Point(Width / 2, Height / 2);

            this.Text = "Spine Runtime - " + mSkeletonData.Version;



        }

        private void OnSelectAnimation(object sender, EventArgs e)
        {
            skeletonAnimation.SetAnimation(0, (sender as ToolStripMenuItem).Text, true);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            pipeline.Begin();
            if (skeleton != null)
            {
                skeletonAnimation.Apply(skeleton);
                GdiRenderer.Draw(skeleton, pipeline);
            }
            e.Graphics.ResetTransform();
            e.Graphics.TranslateTransform(Center.X, Center.Y);
            e.Graphics.ScaleTransform(Zoom, -Zoom);
            pipeline.End(e.Graphics);

            e.Graphics.ResetTransform();            
        }

        private void OnMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            {
                var numberOfTextLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
                this.Zoom += numberOfTextLinesToMove / 10.0f;

                if (this.Zoom < 0.125f)
                {
                    this.Zoom = 0.125f;
                }
                else if (this.Zoom > 8.0)
                {
                    this.Zoom = 8;
                }

                this.panel1.Zoom = this.Zoom;
              
                this.panel1.Invalidate();
            }

        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Center = e.Location;
                this.panel1.Center = this.Center;
                this.panel1.Invalidate();
            }
        }

        PointF HoldPosition = new PointF();
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            HoldPosition = e.Location;
        }

        private void Tick(object sender, EventArgs e)
        {
            if (skeleton != null)
            {
                skeletonAnimation.Update(1.0F / 30.0F);
                skeletonAnimation.Apply(skeleton);
                this.panel1.Invalidate();
            }
        }
    }
}
