using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenGlow
{
    internal sealed class BrightnessPopupForm : Form
    {
        private readonly AppConfig _config;
        private readonly Esp8266Client _client;
        private readonly Action _onBrightnessSent;
        private readonly Dictionary<string, GlowSliderControl> _sliders = new Dictionary<string, GlowSliderControl>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _pendingEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly TableLayoutPanel _root;
        private readonly TableLayoutPanel _sliderPanel;
        private readonly System.Windows.Forms.Timer _sendTimer;
        private bool _isSending;
        private bool _isSyncing;
        private const int CsDropShadow = 0x00020000;

        public BrightnessPopupForm(AppConfig config, Esp8266Client client, Action onBrightnessSent)
        {
            _config = config;
            _client = client;
            _onBrightnessSent = onBrightnessSent;

            Text = "ScreenGlow";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(300, 84);
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(30, 31, 34);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            _sendTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _sendTimer.Tick += async delegate
            {
                _sendTimer.Stop();
                await SendPendingBrightnessAsync();
            };

            _root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 31, 34),
                Padding = new Padding(10, 8, 12, 8),
                ColumnCount = 1,
                RowCount = 1
            };
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _sliderPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 31, 34),
                ColumnCount = 2
            };
            _sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            _sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _root.Controls.Add(_sliderPanel, 0, 0);
            Controls.Add(_root);

            BuildSliders();
            SyncControlsFromConfig();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= CsDropShadow;
                return cp;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = CreateRoundRect(rect, 12))
            using (var pen = new Pen(Color.FromArgb(82, 87, 96)))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }

        public void ShowNearCursor()
        {
            BuildSliders();
            SyncControlsFromConfig();

            var cursor = Cursor.Position;
            var area = Screen.FromPoint(cursor).WorkingArea;
            var x = Math.Min(Math.Max(cursor.X - Width / 2, area.Left), area.Right - Width);
            var y = Math.Min(Math.Max(cursor.Y - Height - 12, area.Top), area.Bottom - Height);

            Location = new Point(x, y);
            Show();
            Activate();
        }

        public void RefreshFromConfig()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshFromConfig));
                return;
            }

            if (NeedsSliderRebuild())
            {
                BuildSliders();
            }

            SyncControlsFromConfig();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            FlushPendingBrightness();
            Hide();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sendTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void BuildSliders()
        {
            _config.Normalize();
            _sliderPanel.SuspendLayout();
            _sliderPanel.Controls.Clear();
            _sliderPanel.RowStyles.Clear();
            _sliders.Clear();

            var entities = _config.LightEntities;
            _sliderPanel.RowCount = Math.Max(1, entities.Length);

            foreach (var entity in entities)
            {
                _sliderPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
                var row = _sliderPanel.RowStyles.Count - 1;

                var iconLabel = new GlowIconBadge
                {
                    Dock = DockStyle.Fill,
                    ForeColor = Color.White,
                    IconText = GetDisplayIcon(_config.GetDisplayName(entity)),
                    Margin = new Padding(0, 4, 8, 4)
                };

                var slider = new GlowSliderControl
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(30, 31, 34),
                    Margin = new Padding(0, 5, 0, 5),
                    Tag = entity
                };

                slider.ValueChanged += delegate
                {
                    if (_isSyncing)
                    {
                        return;
                    }

                    var changedEntity = (string)slider.Tag;
                    _config.SetEntityBrightness(changedEntity, slider.Value);
                    QueueSend(changedEntity);
                };
                slider.Commit += delegate
                {
                    var changedEntity = (string)slider.Tag;
                    _config.SetEntityBrightness(changedEntity, slider.Value);
                    QueueSendNow(changedEntity);
                };

                _sliders[entity] = slider;
                _sliderPanel.Controls.Add(iconLabel, 0, row);
                _sliderPanel.Controls.Add(slider, 1, row);
            }

            Height = Math.Max(52, 16 + entities.Length * 34);
            _sliderPanel.ResumeLayout();
            ApplyRoundedRegion();
        }

        private void SyncControlsFromConfig()
        {
            _isSyncing = true;

            foreach (var entity in _config.LightEntities)
            {
                if (_sliders.TryGetValue(entity, out var slider))
                {
                    slider.Value = _config.GetEntityBrightness(entity);
                }
            }

            _isSyncing = false;
        }

        private bool NeedsSliderRebuild()
        {
            _config.Normalize();

            if (_sliders.Count != _config.LightEntities.Length)
            {
                return true;
            }

            foreach (var entity in _config.LightEntities)
            {
                if (!_sliders.ContainsKey(entity))
                {
                    return true;
                }
            }

            return false;
        }

        private void QueueSend(string entity)
        {
            _pendingEntities.Add(entity);
            QueueSendCore(false);
        }

        private void QueueSendNow(string entity)
        {
            _pendingEntities.Add(entity);
            QueueSendCore(true);
        }

        private void QueueSendCore(bool sendNow)
        {
            _config.Save();
            _sendTimer.Stop();
            _onBrightnessSent();

            if (sendNow)
            {
                FlushPendingBrightness();
            }
            else
            {
                _sendTimer.Start();
            }
        }

        private void FlushPendingBrightness()
        {
            _sendTimer.Stop();
            var ignored = SendPendingBrightnessAsync();
        }

        private async Task SendPendingBrightnessAsync()
        {
            if (_pendingEntities.Count == 0)
            {
                return;
            }

            if (_isSending)
            {
                _sendTimer.Stop();
                _sendTimer.Start();
                return;
            }

            _isSending = true;
            try
            {
                while (_pendingEntities.Count > 0)
                {
                    var entities = new List<string>(_pendingEntities);
                    _pendingEntities.Clear();

                    foreach (var entity in entities)
                    {
                        var brightness = _config.GetEntityBrightness(entity);
                        await _client.SendBrightnessAsync(_config, entity, brightness, CancellationToken.None);
                    }

                    _onBrightnessSent();
                }
            }
            catch (Exception ex)
            {
                Text = "ScreenGlow 发送失败：" + ex.Message;
            }
            finally
            {
                _isSending = false;

                if (_pendingEntities.Count > 0)
                {
                    _sendTimer.Stop();
                    _sendTimer.Start();
                }
            }
        }

        private static string GetDisplayIcon(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return "💡";
            }

            var enumerator = StringInfo.GetTextElementEnumerator(displayName.Trim());
            return enumerator.MoveNext() ? enumerator.GetTextElement() : "💡";
        }

        private void ApplyRoundedRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            using (var path = CreateRoundRect(new Rectangle(0, 0, Width, Height), 12))
            {
                Region = new Region(path);
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
    }
}
