using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ScreenGlow
{
    internal sealed class Esp8266Client : IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task SendBrightnessAsync(AppConfig config, string entity, int brightnessPercent, CancellationToken cancellationToken)
        {
            config.Normalize();

            using (var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeoutCancellation.CancelAfter(TimeSpan.FromMilliseconds(config.RequestTimeoutMs));
                var uri = config.BuildLightUri(entity, brightnessPercent);

                using (var response = await _httpClient.PostAsync(uri, new StringContent(string.Empty), timeoutCancellation.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public async Task<int?> GetBrightnessAsync(AppConfig config, string entity, CancellationToken cancellationToken)
        {
            config.Normalize();

            using (var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeoutCancellation.CancelAfter(TimeSpan.FromMilliseconds(config.RequestTimeoutMs));
                var uri = config.BuildLightStateUri(entity);

                using (var response = await _httpClient.GetAsync(uri, timeoutCancellation.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return ParseBrightness(json);
                }
            }
        }

        private static int? ParseBrightness(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var serializer = new JavaScriptSerializer();
            var state = serializer.Deserialize<Dictionary<string, object>>(json);
            if (state == null)
            {
                return null;
            }

            object stateValue;
            if (state.TryGetValue("state", out stateValue) &&
                string.Equals(Convert.ToString(stateValue), "OFF", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            object brightnessValue;
            if (state.TryGetValue("brightness", out brightnessValue))
            {
                return AppConfig.ClampPercent(ConvertBrightnessToPercent(brightnessValue));
            }

            if (state.TryGetValue("state", out stateValue) &&
                string.Equals(Convert.ToString(stateValue), "ON", StringComparison.OrdinalIgnoreCase))
            {
                return AppConfig.MaxBrightnessPercent;
            }

            return null;
        }

        private static int ConvertBrightnessToPercent(object brightnessValue)
        {
            double brightness;
            try
            {
                brightness = Convert.ToDouble(brightnessValue);
            }
            catch
            {
                return AppConfig.MaxBrightnessPercent;
            }

            if (brightness <= 0)
            {
                return 0;
            }

            if (brightness <= 1)
            {
                return (int)Math.Round(brightness * 100);
            }

            return (int)Math.Round(brightness * 100 / 255.0);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
