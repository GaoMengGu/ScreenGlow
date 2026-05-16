using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenGlow
{
    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly AppConfig _config;
        private readonly Esp8266Client _client;
        private readonly NotifyIcon _notifyIcon;
        private BrightnessPopupForm _brightnessPopup;

        public TrayAppContext()
        {
            _config = AppConfig.Load();
            RepairStartupRegistration();
            _client = new Esp8266Client();
            _notifyIcon = CreateNotifyIcon();
            _notifyIcon.Visible = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_brightnessPopup != null)
                {
                    _brightnessPopup.Dispose();
                }

                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _client.Dispose();
            }

            base.Dispose(disposing);
        }

        private NotifyIcon CreateNotifyIcon()
        {
            var menu = new ContextMenuStrip();
            menu.Opening += delegate { BuildTrayMenu(menu); };

            var notifyIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = BuildTrayText(),
                ContextMenuStrip = menu
            };

            notifyIcon.MouseClick += HandleNotifyIconMouseClick;
            return notifyIcon;
        }

        private static Icon LoadTrayIcon()
        {
            try
            {
                var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                return icon ?? SystemIcons.Application;
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void HandleNotifyIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowBrightnessPopup();
            }
        }

        private void ShowBrightnessPopup()
        {
            if (_brightnessPopup == null || _brightnessPopup.IsDisposed)
            {
                _brightnessPopup = new BrightnessPopupForm(_config, _client, UpdateTrayText);
            }

            _brightnessPopup.ShowNearCursor();
        }

        private void BuildTrayMenu(ContextMenuStrip menu)
        {
            _config.Normalize();
            menu.Items.Clear();
            StyleMenu(menu);

            var startupItem = new ToolStripMenuItem("开机自动启动")
            {
                Checked = StartupManager.IsEnabledForCurrentExecutable()
            };
            startupItem.Click += delegate { ToggleStartup(); };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());

            var deviceItem = CreateMenuItem("设备设置");
            deviceItem.DropDownItems.Add("地址: " + Compact(_config.DeviceUrl, 34), null, delegate { EditDeviceUrl(); });
            deviceItem.DropDownItems.Add("实体名: " + Compact(EntitySummary(), 34), null, delegate { EditEntities(); });
            StyleDropDown(deviceItem);
            menu.Items.Add(deviceItem);

            if (_config.LightEntities.Length > 0)
            {
                var entitiesItem = CreateMenuItem("实体设置");
                for (var index = 0; index < _config.LightEntities.Length; index++)
                {
                    var entity = _config.LightEntities[index];
                    var lightItem = CreateMenuItem(_config.GetDisplayName(entity));
                    lightItem.DropDownItems.Add(new ToolStripMenuItem("实体: " + Compact(entity, 24)) { Enabled = false });
                    lightItem.DropDownItems.Add(new ToolStripMenuItem("亮度: " + _config.GetEntityBrightness(entity) + "%") { Enabled = false });
                    lightItem.DropDownItems.Add("显示名称: " + Compact(_config.GetDisplayName(entity), 24), null, delegate { EditDisplayName(entity); });
                    StyleDropDown(lightItem);
                    entitiesItem.DropDownItems.Add(lightItem);
                }

                StyleDropDown(entitiesItem);
                menu.Items.Add(entitiesItem);
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出", null, delegate { ExitThread(); });
            StyleItems(menu.Items);
        }

        private static ToolStripMenuItem CreateMenuItem(string text)
        {
            return new ToolStripMenuItem(text)
            {
                AutoSize = true,
                Padding = new Padding(3, 2, 3, 2)
            };
        }

        private static void StyleMenu(ContextMenuStrip menu)
        {
            menu.Renderer = new GlowMenuRenderer();
            menu.BackColor = Color.FromArgb(31, 32, 35);
            menu.ForeColor = Color.FromArgb(245, 245, 245);
            menu.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            menu.ShowImageMargin = true;
            menu.Padding = new Padding(2);
        }

        private static void StyleDropDown(ToolStripMenuItem item)
        {
            item.DropDown.Renderer = new GlowMenuRenderer();
            item.DropDown.BackColor = Color.FromArgb(31, 32, 35);
            item.DropDown.ForeColor = Color.FromArgb(245, 245, 245);
            item.DropDown.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            item.DropDown.Padding = new Padding(2);
        }

        private static void StyleItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                item.BackColor = Color.FromArgb(31, 32, 35);
                item.ForeColor = Color.FromArgb(245, 245, 245);
                item.Padding = new Padding(3, 2, 3, 2);

                var menuItem = item as ToolStripMenuItem;
                if (menuItem != null)
                {
                    StyleDropDown(menuItem);
                    StyleItems(menuItem.DropDownItems);
                }
            }
        }

        private void EditDeviceUrl()
        {
            if (PromptDialog.TryShow("设备地址", "ESPHome 地址", _config.DeviceUrl, out var value))
            {
                _config.DeviceUrl = value;
                _config.Save();
                UpdateTrayText();
            }
        }

        private void EditEntities()
        {
            if (PromptDialog.TryShow("实体名", "多个实体名用逗号分隔", _config.EntitiesText, out var value))
            {
                _config.EntitiesText = value;
                _config.Save();
                UpdateTrayText();
            }
        }

        private void ToggleStartup()
        {
            try
            {
                var enabled = !StartupManager.IsEnabledForCurrentExecutable();
                StartupManager.SetEnabled(enabled);
                _config.StartWithWindows = enabled;
                _config.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("开机启动设置失败：" + ex.Message, "ScreenGlow", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RepairStartupRegistration()
        {
            try
            {
                if (_config.StartWithWindows || StartupManager.NeedsPathRepair())
                {
                    StartupManager.SetEnabled(true);
                    _config.StartWithWindows = true;
                    _config.Save();
                }
            }
            catch
            {
                // The tray menu still lets the user retry and see the concrete error.
            }
        }

        private void EditDisplayName(string entity)
        {
            if (PromptDialog.TryShow("显示名称", "可以输入 emoji 名称", _config.GetDisplayName(entity), out var value))
            {
                _config.SetDisplayName(entity, value);
                _config.Save();
                UpdateTrayText();
            }
        }

        private void UpdateTrayText()
        {
            _notifyIcon.Text = BuildTrayText();
        }

        private string BuildTrayText()
        {
            _config.Normalize();

            if (_config.LightEntities.Length == 0)
            {
                return "ScreenGlow";
            }

            if (_config.LightEntities.Length == 1)
            {
                var entity = _config.LightEntities[0];
                return "ScreenGlow " + _config.GetDisplayName(entity) + " " + _config.GetEntityBrightness(entity) + "%";
            }

            return "ScreenGlow " + Compact(string.Join(" / ", Array.ConvertAll(_config.LightEntities, entity => _config.GetDisplayName(entity))), 48);
        }

        private string EntitySummary()
        {
            return _config.LightEntities.Length == 0 ? "未设置" : _config.EntitiesText;
        }

        private static string Compact(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength - 1) + "...";
        }
    }
}
