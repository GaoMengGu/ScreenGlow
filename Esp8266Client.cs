using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenGlow
{
    internal sealed class Esp8266Client : IDisposable
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task SendBrightnessAsync(AppConfig config, int brightnessPercent, CancellationToken cancellationToken)
        {
            config.Normalize();

            foreach (var entity in config.LightEntities)
            {
                await SendBrightnessAsync(config, entity, brightnessPercent, cancellationToken).ConfigureAwait(false);
            }
        }

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

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
