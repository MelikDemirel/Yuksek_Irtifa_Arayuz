using GMap.NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GokhanUI;

namespace GokhanUI
{
    public partial class Map : Form
    {
     
        GMap.NET.WindowsForms.GMapControl gmap;
        public Map()
        {
            InitializeComponent();
            gmap = new GMap.NET.WindowsForms.GMapControl();
            gmap.MapProvider = GMap.NET.MapProviders.GMapProviders.GoogleMap;
            gmap.Dock = DockStyle.Fill;
            gmap.MapProvider = GMap.NET.MapProviders.BingMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            gmap.ShowCenter = false;
            gmap.MinZoom = 1;
            gmap.MaxZoom = 20;
            splitContainer1.Panel2.Controls.Add(gmap);
        }

        private void go_Coordinate_Click(object sender, EventArgs e)
        {
            gmap.Position = new PointLatLng(Convert.ToDouble(latitude.Text), Convert.ToDouble(longitude.Text));
            gmap.Zoom = 5;
            gmap.Update();
            gmap.Refresh();

        }
        private void change_zoom_Click(object sender, EventArgs e)
        {
            gmap.Zoom = Convert.ToDouble(zoom_level.Text);
            gmap.Update();
            gmap.Refresh();

        }

        private void activate_drag_Click(object sender, EventArgs e)
        {
            gmap.DragButton = MouseButtons.Left;
        }

        private void active_mouse_click_Click(object sender, EventArgs e)
        {
            gmap.MouseClick += gmap_MouseClick;
        }

        void gmap_MouseClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("X:" + e.X.ToString() + " and Y:" + e.Y.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Layer count is just a variable to add new OverLays with different names
            var markersOverlay = new GMap.NET.WindowsForms.GMapOverlay("marker1");

            //Marker far away in Quebec, Canada just to check my point in discussion        
            var marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                new PointLatLng(Convert.ToDouble(latitude.Text), Convert.ToDouble(longitude.Text)),
                GMap.NET.WindowsForms.Markers.GMarkerGoogleType.red_small);

            markersOverlay.Markers.Add(marker);
            gmap.Overlays.Add(markersOverlay);
        }

        private void Map_Load(object sender, EventArgs e)
        {

        }
    }
}
