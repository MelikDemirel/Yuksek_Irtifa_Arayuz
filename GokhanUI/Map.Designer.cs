namespace GokhanUI

{
    partial class Map
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.button1 = new System.Windows.Forms.Button();
            this.add_marker = new System.Windows.Forms.Button();
            this.active_mouse_click = new System.Windows.Forms.Button();
            this.activate_drag = new System.Windows.Forms.Button();
            this.change_zoom = new System.Windows.Forms.Button();
            this.zoom_level = new System.Windows.Forms.TextBox();
            this.longitude = new System.Windows.Forms.TextBox();
            this.latitude = new System.Windows.Forms.TextBox();
            this.go_Coordinate = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.button1);
            this.splitContainer1.Panel1.Controls.Add(this.add_marker);
            this.splitContainer1.Panel1.Controls.Add(this.active_mouse_click);
            this.splitContainer1.Panel1.Controls.Add(this.activate_drag);
            this.splitContainer1.Panel1.Controls.Add(this.change_zoom);
            this.splitContainer1.Panel1.Controls.Add(this.zoom_level);
            this.splitContainer1.Panel1.Controls.Add(this.longitude);
            this.splitContainer1.Panel1.Controls.Add(this.latitude);
            this.splitContainer1.Panel1.Controls.Add(this.go_Coordinate);
            this.splitContainer1.Size = new System.Drawing.Size(1035, 630);
            this.splitContainer1.SplitterDistance = 245;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(59, 138);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 28);
            this.button1.TabIndex = 7;
            this.button1.Text = "Add Marker";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // add_marker
            // 
            this.add_marker.Location = new System.Drawing.Point(59, 138);
            this.add_marker.Margin = new System.Windows.Forms.Padding(4);
            this.add_marker.Name = "add_marker";
            this.add_marker.Size = new System.Drawing.Size(100, 28);
            this.add_marker.TabIndex = 7;
            this.add_marker.Text = "Add Marker";
            this.add_marker.UseVisualStyleBackColor = true;
            // 
            // active_mouse_click
            // 
            this.active_mouse_click.Location = new System.Drawing.Point(16, 393);
            this.active_mouse_click.Margin = new System.Windows.Forms.Padding(4);
            this.active_mouse_click.Name = "active_mouse_click";
            this.active_mouse_click.Size = new System.Drawing.Size(99, 58);
            this.active_mouse_click.TabIndex = 6;
            this.active_mouse_click.Text = "Active Mouse Click";
            this.active_mouse_click.UseVisualStyleBackColor = true;
            this.active_mouse_click.Click += new System.EventHandler(this.active_mouse_click_Click);
            // 
            // activate_drag
            // 
            this.activate_drag.Location = new System.Drawing.Point(16, 316);
            this.activate_drag.Margin = new System.Windows.Forms.Padding(4);
            this.activate_drag.Name = "activate_drag";
            this.activate_drag.Size = new System.Drawing.Size(99, 43);
            this.activate_drag.TabIndex = 5;
            this.activate_drag.Text = "Active Drag";
            this.activate_drag.UseVisualStyleBackColor = true;
            this.activate_drag.Click += new System.EventHandler(this.activate_drag_Click);
            // 
            // change_zoom
            // 
            this.change_zoom.Location = new System.Drawing.Point(148, 219);
            this.change_zoom.Margin = new System.Windows.Forms.Padding(4);
            this.change_zoom.Name = "change_zoom";
            this.change_zoom.Size = new System.Drawing.Size(77, 53);
            this.change_zoom.TabIndex = 4;
            this.change_zoom.Text = "Change Zoom Level";
            this.change_zoom.UseVisualStyleBackColor = true;
            this.change_zoom.Click += new System.EventHandler(this.change_zoom_Click);
            // 
            // zoom_level
            // 
            this.zoom_level.Location = new System.Drawing.Point(7, 234);
            this.zoom_level.Margin = new System.Windows.Forms.Padding(4);
            this.zoom_level.Name = "zoom_level";
            this.zoom_level.Size = new System.Drawing.Size(132, 22);
            this.zoom_level.TabIndex = 3;
            this.zoom_level.Text = "12";
            // 
            // longitude
            // 
            this.longitude.Location = new System.Drawing.Point(59, 59);
            this.longitude.Margin = new System.Windows.Forms.Padding(4);
            this.longitude.Name = "longitude";
            this.longitude.Size = new System.Drawing.Size(132, 22);
            this.longitude.TabIndex = 2;
            this.longitude.Text = "18.4";
            // 
            // latitude
            // 
            this.latitude.Location = new System.Drawing.Point(59, 16);
            this.latitude.Margin = new System.Windows.Forms.Padding(4);
            this.latitude.Name = "latitude";
            this.latitude.Size = new System.Drawing.Size(132, 22);
            this.latitude.TabIndex = 1;
            this.latitude.Text = "-33.3";
            // 
            // go_Coordinate
            // 
            this.go_Coordinate.Location = new System.Drawing.Point(59, 102);
            this.go_Coordinate.Margin = new System.Windows.Forms.Padding(4);
            this.go_Coordinate.Name = "go_Coordinate";
            this.go_Coordinate.Size = new System.Drawing.Size(100, 28);
            this.go_Coordinate.TabIndex = 0;
            this.go_Coordinate.Text = "Go To Coordinate";
            this.go_Coordinate.UseVisualStyleBackColor = true;
            this.go_Coordinate.Click += new System.EventHandler(this.go_Coordinate_Click);
            // 
            // Map
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1035, 630);
            this.Controls.Add(this.splitContainer1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Map";
            this.Text = "Map";
            this.Load += new System.EventHandler(this.Map_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TextBox longitude;
        private System.Windows.Forms.TextBox latitude;
        private System.Windows.Forms.Button go_Coordinate;
        private System.Windows.Forms.Button change_zoom;
        private System.Windows.Forms.TextBox zoom_level;
        private System.Windows.Forms.Button activate_drag;
        private System.Windows.Forms.Button active_mouse_click;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button add_marker;
    }
}

