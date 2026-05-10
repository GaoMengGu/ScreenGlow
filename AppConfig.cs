using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ScreenGlow
{
    internal sealed class AppConfig
    {
        public const int MaxBrightnessPercent = 100;

        public string DeviceUrl { get; set; } = "http://your-device.local/";
        public string[] LightEntities { get; set; } = new string[0];
        public Dictionary<string, string> LightDisplayNames { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public int BrightnessPercent { get; set; } = 100;
        public Dictionary<string, int> EntityBrightnessPercent { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> EntityLastOnBrightnessPercent { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public int RequestTimeoutMs { get; set; } = 2500;
        public bool StartWithWindows { get; set; }

        public static string ConfigDirectory
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "ScreenGlow");
            }
        }

        public static string ConfigPath => Path.Combine(ConfigDirectory, "appsettings.json");

        public static AppConfig Load()
        {
            Directory.CreateDirectory(ConfigDirectory);

            if (!File.Exists(ConfigPath))
            {
                var created = new AppConfig();
                created.Save();
                return created;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                return config == null ? new AppConfig() : config.Normalize();
            }
            catch
            {
                var backupPath = ConfigPath + ".broken-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                File.Copy(ConfigPath, backupPath, true);

                var fresh = new AppConfig();
                fresh.Save();
                return fresh;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(ConfigDirectory);
            var json = JsonSerializer.Serialize(Normalize(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
        }

        public Uri BuildLightUri(string entity, int brightnessPercent)
        {
            var baseUri = new Uri(NormalizedDeviceUrl());
            var safeEntity = Uri.EscapeDataString(entity);

            if (ClampPercent(brightnessPercent) == 0)
            {
                return new Uri(baseUri, "light/" + safeEntity + "/turn_off");
            }

            var brightness255 = PercentToEspHomeBrightness(brightnessPercent);
            return new Uri(baseUri, "light/" + safeEntity + "/turn_on?brightness=" + brightness255);
        }

        public string EntitiesText
        {
            get
            {
                return string.Join(", ", LightEntities ?? new string[0]);
            }
            set
            {
                LightEntities = (value ?? string.Empty)
                    .Split(new[] { ',', ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(entity => entity.Trim())
                    .Where(entity => entity.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        public AppConfig Normalize()
        {
            DeviceUrl = string.IsNullOrWhiteSpace(DeviceUrl) ? "http://your-device.local/" : DeviceUrl.Trim();
            if (!DeviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !DeviceUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                DeviceUrl = "http://" + DeviceUrl;
            }

            if (!DeviceUrl.EndsWith("/", StringComparison.Ordinal))
            {
                DeviceUrl += "/";
            }

            if (LightEntities == null)
            {
                LightEntities = new string[0];
            }

            LightEntities = LightEntities
                .Where(entity => !string.IsNullOrWhiteSpace(entity))
                .Select(entity => entity.Trim().Trim('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            BrightnessPercent = ClampPercent(BrightnessPercent);
            RequestTimeoutMs = Math.Max(500, RequestTimeoutMs);

            if (EntityBrightnessPercent == null)
            {
                EntityBrightnessPercent = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                EntityBrightnessPercent = new Dictionary<string, int>(EntityBrightnessPercent, StringComparer.OrdinalIgnoreCase);
            }

            foreach (var entity in LightEntities)
            {
                if (!EntityBrightnessPercent.ContainsKey(entity))
                {
                    EntityBrightnessPercent[entity] = BrightnessPercent;
                }

                EntityBrightnessPercent[entity] = ClampPercent(EntityBrightnessPercent[entity]);
            }

            if (EntityLastOnBrightnessPercent == null)
            {
                EntityLastOnBrightnessPercent = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                EntityLastOnBrightnessPercent = new Dictionary<string, int>(EntityLastOnBrightnessPercent, StringComparer.OrdinalIgnoreCase);
            }

            foreach (var entity in LightEntities)
            {
                var current = EntityBrightnessPercent[entity];
                if (!EntityLastOnBrightnessPercent.ContainsKey(entity))
                {
                    EntityLastOnBrightnessPercent[entity] = current > 0 ? current : 60;
                }

                EntityLastOnBrightnessPercent[entity] = ClampPercent(EntityLastOnBrightnessPercent[entity]);
                if (EntityLastOnBrightnessPercent[entity] == 0)
                {
                    EntityLastOnBrightnessPercent[entity] = 60;
                }
            }

            if (LightDisplayNames == null)
            {
                LightDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                LightDisplayNames = new Dictionary<string, string>(LightDisplayNames, StringComparer.OrdinalIgnoreCase);
            }

            foreach (var entity in LightEntities)
            {
                if (!LightDisplayNames.ContainsKey(entity) || string.IsNullOrWhiteSpace(LightDisplayNames[entity]))
                {
                    LightDisplayNames[entity] = DefaultDisplayName(entity);
                }
                else
                {
                    LightDisplayNames[entity] = LightDisplayNames[entity].Trim();
                }
            }

            return this;
        }

        public int GetEntityBrightness(string entity)
        {
            Normalize();
            if (EntityBrightnessPercent.TryGetValue(entity, out var value))
            {
                return ClampPercent(value);
            }

            return BrightnessPercent;
        }

        public void SetEntityBrightness(string entity, int brightnessPercent)
        {
            Normalize();
            var clamped = ClampPercent(brightnessPercent);
            EntityBrightnessPercent[entity] = clamped;
            if (clamped > 0)
            {
                EntityLastOnBrightnessPercent[entity] = clamped;
            }

            if (LightEntities.Length > 0)
            {
                BrightnessPercent = (int)Math.Round(LightEntities.Average(name => GetEntityBrightness(name)));
            }
            else
            {
                BrightnessPercent = ClampPercent(brightnessPercent);
            }
        }

        public void SetAllBrightness(int brightnessPercent)
        {
            Normalize();
            var clamped = ClampPercent(brightnessPercent);
            BrightnessPercent = clamped;

            foreach (var entity in LightEntities)
            {
                EntityBrightnessPercent[entity] = clamped;
                if (clamped > 0)
                {
                    EntityLastOnBrightnessPercent[entity] = clamped;
                }
            }
        }

        public void SetPower(bool isOn)
        {
            Normalize();

            foreach (var entity in LightEntities)
            {
                if (isOn)
                {
                    EntityBrightnessPercent[entity] = Math.Max(1, EntityLastOnBrightnessPercent[entity]);
                }
                else
                {
                    var current = EntityBrightnessPercent[entity];
                    if (current > 0)
                    {
                        EntityLastOnBrightnessPercent[entity] = current;
                    }

                    EntityBrightnessPercent[entity] = 0;
                }
            }

            BrightnessPercent = LightEntities.Length > 0
                ? (int)Math.Round(LightEntities.Average(name => EntityBrightnessPercent[name]))
                : 0;
        }

        public bool IsAnyLightOn()
        {
            Normalize();
            return LightEntities.Any(entity => EntityBrightnessPercent[entity] > 0);
        }

        public string GetDisplayName(string entity)
        {
            Normalize();
            if (LightDisplayNames.TryGetValue(entity, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return DefaultDisplayName(entity);
        }

        public void SetDisplayName(string entity, string displayName)
        {
            Normalize();
            LightDisplayNames[entity] = string.IsNullOrWhiteSpace(displayName) ? DefaultDisplayName(entity) : displayName.Trim();
        }

        public void UpdateLightEntity(int index, string newEntity)
        {
            Normalize();

            if (index < 0 || index >= LightEntities.Length || string.IsNullOrWhiteSpace(newEntity))
            {
                return;
            }

            var oldEntity = LightEntities[index];
            var cleanEntity = newEntity.Trim().Trim('/');
            if (cleanEntity.Length == 0 || oldEntity.Equals(cleanEntity, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var brightness = GetEntityBrightness(oldEntity);
            var lastOnBrightness = EntityLastOnBrightnessPercent.ContainsKey(oldEntity)
                ? EntityLastOnBrightnessPercent[oldEntity]
                : Math.Max(1, brightness);
            var displayName = GetDisplayName(oldEntity);

            LightEntities[index] = cleanEntity;
            LightEntities = LightEntities
                .Where(entity => !string.IsNullOrWhiteSpace(entity))
                .Select(entity => entity.Trim().Trim('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            EntityBrightnessPercent[cleanEntity] = brightness;
            EntityLastOnBrightnessPercent[cleanEntity] = lastOnBrightness;
            LightDisplayNames[cleanEntity] = displayName;
            Normalize();
        }

        public static int ClampPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > MaxBrightnessPercent)
            {
                return MaxBrightnessPercent;
            }

            return value;
        }

        private string NormalizedDeviceUrl()
        {
            Normalize();
            return DeviceUrl;
        }

        private static int PercentToEspHomeBrightness(int percent)
        {
            var clamped = ClampPercent(percent);
            if (clamped <= 0)
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Round(clamped * 255 / 100.0));
        }

        private static string DefaultDisplayName(string entity)
        {
            return "💡 " + entity;
        }
    }
}
