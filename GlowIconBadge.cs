using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenGlow
{
    internal sealed class GlowIconBadge : Control
    {
        public GlowIconBadge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.UserPaint, true);

            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Font = new Font("Segoe UI Emoji", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            Size = new Size(26, 26);
        }

        public string IconText { get; set; } = "💡";

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(2, 2, Width - 4, Height - 4);
            using (var path = new GraphicsPath())
            using (var brush = new LinearGradientBrush(rect, Color.FromArgb(66, 72, 82), Color.FromArgb(40, 44, 52), 45F))
            using (var borderPen = new Pen(Color.FromArgb(82, 90, 102)))
            using (var textBrush = new SolidBrush(ForeColor))
            {
                path.AddEllipse(rect);
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(borderPen, path);

                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(IconText, Font, textBrush, rect, format);
            }
        }
    }
}
