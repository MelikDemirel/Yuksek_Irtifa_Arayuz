using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GokhanUI
{
    public partial class GaugeControl : UserControl
    {
        [Category("Gauge")]
        public float Value { get; set; } = 0;

        [Category("Gauge")]
        public float Min { get; set; } = 0;

        [Category("Gauge")]
        public float Max { get; set; } = 100;

        [Category("Gauge")]
        public string Unit { get; set; } = "";

        [Category("Gauge")]
        public Color BarColor { get; set; } = Color.Lime;

        [Category("Gauge")]
        public string Caption { get; set; } = "";

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int margin = 10;
            Rectangle rect = new Rectangle(margin, margin, Width - 2 * margin, Height * 2 - 2 * margin);

            // Arka plan (gri)
            using (var pen = new Pen(Color.FromArgb(60, 60, 60), 12))
                g.DrawArc(pen, rect, 180, 180);

            // Değer barı
            float sweep = 180f * (Value - Min) / (Max - Min);
            using (var pen = new Pen(BarColor, 12))
                g.DrawArc(pen, rect, 180, sweep);

            // Değer yazısı
            string text = $"{Value:0.0} {Unit}";
            var size = g.MeasureString(text, Font);
            g.DrawString(text, Font, Brushes.White, (Width - size.Width) / 2, Height / 2 - size.Height / 2);

            // Caption
            if (!string.IsNullOrEmpty(Caption))
            {
                var capSize = g.MeasureString(Caption, Font);
                g.DrawString(Caption, Font, Brushes.Gray, (Width - capSize.Width) / 2, Height - capSize.Height - 2);
            }
        }
    }
}
