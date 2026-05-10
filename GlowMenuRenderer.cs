using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenGlow
{
    internal sealed class GlowMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color MenuBack = Color.FromArgb(31, 32, 35);
        private static readonly Color MenuBorder = Color.FromArgb(72, 76, 84);
        private static readonly Color ItemSelected = Color.FromArgb(0, 105, 185);
        private static readonly Color TextColor = Color.FromArgb(245, 245, 245);
        private static readonly Color DisabledTextColor = Color.FromArgb(150, 150, 150);

        public GlowMenuRenderer() : base(new GlowColorTable())
        {
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new SolidBrush(MenuBack))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(MenuBorder))
            {
                var rect = new Rectangle(Point.Empty, e.ToolStrip.Size);
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected)
            {
                return;
            }

            using (var brush = new SolidBrush(ItemSelected))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(3, 2, e.Item.Width - 6, e.Item.Height - 4);
                using (var path = CreateRoundRect(rect, 5))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
        }

        private static GraphicsPath CreateRoundRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? TextColor : DisabledTextColor;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var pen = new Pen(MenuBorder))
            {
                e.Graphics.DrawLine(pen, 10, e.Item.Height / 2, e.Item.Width - 10, e.Item.Height / 2);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var rect = new Rectangle(e.ImageRectangle.X + 3, e.ImageRectangle.Y + 3, e.ImageRectangle.Width - 6, e.ImageRectangle.Height - 6);
            using (var brush = new SolidBrush(Color.FromArgb(55, 190, 245)))
            using (var pen = new Pen(Color.White, 2))
            {
                e.Graphics.FillEllipse(brush, rect);
                e.Graphics.DrawLines(pen, new[]
                {
                    new Point(rect.Left + rect.Width / 4, rect.Top + rect.Height / 2),
                    new Point(rect.Left + rect.Width / 2, rect.Bottom - rect.Height / 4),
                    new Point(rect.Right - rect.Width / 5, rect.Top + rect.Height / 4)
                });
            }
        }

        private sealed class GlowColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => ItemSelected;
            public override Color MenuItemBorder => ItemSelected;
            public override Color ToolStripDropDownBackground => MenuBack;
            public override Color ImageMarginGradientBegin => MenuBack;
            public override Color ImageMarginGradientMiddle => MenuBack;
            public override Color ImageMarginGradientEnd => MenuBack;
            public override Color SeparatorDark => MenuBorder;
            public override Color SeparatorLight => MenuBorder;
        }
    }
}
