using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenGlow
{
    internal sealed class PromptDialog : Form
    {
        private readonly TextBox _input;
        private const int CornerRadius = 12;

        private PromptDialog(string title, string label, string value)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Size = new Size(390, 168);
            BackColor = Color.FromArgb(30, 31, 34);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BackColor,
                Padding = new Padding(18, 16, 18, 16),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(new Label
            {
                Text = title,
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 0);

            root.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 184, 192),
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 1);

            _input = new TextBox
            {
                Text = value,
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(245, 247, 250),
                ForeColor = Color.FromArgb(24, 26, 30),
                Height = 24,
                Margin = new Padding(0, 0, 0, 14)
            };
            root.Controls.Add(_input, 0, 2);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0)
            };

            var okButton = CreateButton("确定", true);
            okButton.DialogResult = DialogResult.OK;
            var cancelButton = CreateButton("取消", false);
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Margin = new Padding(8, 0, 0, 0);

            buttons.Controls.Add(okButton);
            buttons.Controls.Add(cancelButton);
            root.Controls.Add(buttons, 0, 3);

            AcceptButton = okButton;
            CancelButton = cancelButton;
            Controls.Add(root);
        }

        protected override void OnShown(System.EventArgs e)
        {
            base.OnShown(e);
            _input.SelectAll();
            _input.Focus();
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            using (var path = CreateRoundRect(new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = CreateRoundRect(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (var pen = new Pen(Color.FromArgb(82, 87, 96)))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        public static bool TryShow(string title, string label, string value, out string result)
        {
            using (var dialog = new PromptDialog(title, label, value))
            {
                var outcome = dialog.ShowDialog();
                result = dialog._input.Text.Trim();
                return outcome == DialogResult.OK;
            }
        }

        private static Button CreateButton(string text, bool primary)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Width = 78,
                Height = 30,
                BackColor = primary ? Color.FromArgb(0, 120, 212) : Color.FromArgb(48, 51, 58),
                ForeColor = Color.White,
                Margin = new Padding(0),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(0, 120, 212) : Color.FromArgb(76, 82, 92);
            return button;
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
    }
}
