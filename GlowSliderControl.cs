using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenGlow
{
    internal sealed class GlowSliderControl : Control
    {
        private const int ThumbRadius = 6;
        private const int TrackHeight = 4;
        private int _value;
        private bool _isDragging;
        private bool _isHot;

        public event EventHandler ValueChanged;
        public event EventHandler Commit;

        public GlowSliderControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable |
                     ControlStyles.UserPaint, true);

            TabStop = true;
            Height = 24;
            BackColor = Color.FromArgb(30, 31, 34);
            ForeColor = Color.FromArgb(69, 202, 255);
        }

        public int Value
        {
            get { return _value; }
            set
            {
                var clamped = AppConfig.ClampPercent(value);
                if (_value == clamped)
                {
                    return;
                }

                _value = clamped;
                Invalidate();
                var handler = ValueChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);

            var track = GetTrackBounds();
            var fillWidth = (int)Math.Round(track.Width * (_value / 100.0));
            var fill = new Rectangle(track.X, track.Y, fillWidth, track.Height);
            var thumbX = track.X + fillWidth;
            var thumbSize = (_isHot || _isDragging || Focused) ? ThumbRadius * 2 + 3 : ThumbRadius * 2;

            using (var backgroundBrush = new SolidBrush(Color.FromArgb(94, 98, 106)))
            using (var glowBrush = new SolidBrush(Color.FromArgb(54, 55, 190, 245)))
            using (var thumbBrush = new LinearGradientBrush(
                new Rectangle(thumbX - ThumbRadius, Height / 2 - ThumbRadius, ThumbRadius * 2, ThumbRadius * 2),
                Color.White,
                Color.FromArgb(177, 235, 255),
                90F))
            using (var thumbBorder = new Pen(Color.FromArgb(116, 222, 255)))
            {
                FillRoundRect(e.Graphics, backgroundBrush, track, TrackHeight / 2);
                if (fill.Width > 0)
                {
                    using (var fillBrush = new LinearGradientBrush(fill, Color.FromArgb(0, 132, 255), Color.FromArgb(62, 218, 255), 0F))
                    {
                        FillRoundRect(e.Graphics, fillBrush, fill, TrackHeight / 2);
                    }
                }

                if (_isHot || _isDragging || Focused)
                {
                    e.Graphics.FillEllipse(glowBrush, thumbX - thumbSize / 2, Height / 2 - thumbSize / 2, thumbSize, thumbSize);
                }

                e.Graphics.FillEllipse(thumbBrush, thumbX - ThumbRadius, Height / 2 - ThumbRadius, ThumbRadius * 2, ThumbRadius * 2);
                e.Graphics.DrawEllipse(thumbBorder, thumbX - ThumbRadius, Height / 2 - ThumbRadius, ThumbRadius * 2, ThumbRadius * 2);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHot = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHot = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            Focus();
            _isDragging = true;
            Capture = true;
            SetValueFromX(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                SetValueFromX(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (_isDragging)
            {
                SetValueFromX(e.X);
                _isDragging = false;
                Capture = false;
                OnCommit();
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
            {
                Value -= e.Shift ? 10 : 1;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
            {
                Value += e.Shift ? 10 : 1;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Home)
            {
                Value = 0;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.End)
            {
                Value = 100;
                e.Handled = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            OnCommit();
        }

        private void SetValueFromX(int x)
        {
            var track = GetTrackBounds();
            var ratio = (x - track.X) / (double)Math.Max(1, track.Width);
            Value = (int)Math.Round(Math.Max(0, Math.Min(1, ratio)) * 100);
        }

        private Rectangle GetTrackBounds()
        {
            var padding = ThumbRadius + 3;
            return new Rectangle(padding, Height / 2 - TrackHeight / 2, Math.Max(1, Width - padding * 2), TrackHeight);
        }

        private void OnCommit()
        {
            var handler = Commit;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static void FillRoundRect(Graphics graphics, Brush brush, Rectangle rect, int radius)
        {
            using (var path = new GraphicsPath())
            {
                var diameter = radius * 2;
                path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
                path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
                path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                graphics.FillPath(brush, path);
            }
        }
    }
}
