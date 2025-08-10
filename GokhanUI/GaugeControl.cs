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

        [Browsable(true)]
        [Category("Appearance")]
        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                Invalidate(); // yazı değiştiğinde otomatik yeniden çiz
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int margin = 10;

            // Geçerli bir iç dikdörtgen üret (pozitif genişlik/yükseklik)
            int w = Math.Max(1, Width - 2 * margin);
            int h = Math.Max(1, Height - 2 * margin);

            // Yarım daireyi kontrolün altına oturt: genişliğe göre yükseklik = w/2
            int arcHeight = Math.Min(h, w / 2);
            var rect = new Rectangle(
                x: margin,
                y: Height - margin - arcHeight, // altta yarım daire
                width: w,
                height: arcHeight * 2          // tam daire yüksekliği, yarısı görünür
            );

            // Kenar durumları
            float range = Max - Min;
            if (range <= 0f) range = 1f; // 0'a bölmeyi önle
            float clampedValue = Math.Min(Max, Math.Max(Min, Value));
            float sweep = 180f * (clampedValue - Min) / range;
            // Güvenlik için sınırla
            if (float.IsNaN(sweep) || float.IsInfinity(sweep)) sweep = 0f;
            sweep = Math.Min(180f, Math.Max(0f, sweep));

            // Arka plan yay
            using (var backPen = new Pen(Color.FromArgb(60, 60, 60), 12))
                g.DrawArc(backPen, rect, 180, 180);

            // Değer barı
            using (var valPen = new Pen(BarColor, 12))
                g.DrawArc(valPen, rect, 180, sweep);

            // Değer yazısı
            string text = !string.IsNullOrWhiteSpace(Text)
                ? Text
                : $"{clampedValue:0.0} {Unit}";

            var size = g.MeasureString(text, Font);
            g.DrawString(text, Font, Brushes.White,
                (Width - size.Width) / 2f,
                (Height - size.Height) / 2f - 4f);

            // Caption
            if (!string.IsNullOrEmpty(Caption))
            {
                using (var captionFont = new Font(Font.FontFamily, Font.Size + 2.5f, FontStyle.Regular))
                {
                    var capSize = g.MeasureString(Caption, captionFont);
                    g.DrawString(Caption, captionFont, Brushes.White,
                        (Width - capSize.Width) / 2f,
                        Height - capSize.Height - 2f);
                }
            }

        }
    }
}
