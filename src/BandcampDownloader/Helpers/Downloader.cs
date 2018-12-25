using System;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BandcampDownloader {
    internal class Downloader {

        internal static async Task<String> HttpDownload(String url, ProxyType proxyType, CancellationToken ct, Action<String,LogType> fLog) {
            using (WebClient webClient = new WebClient() {
                Encoding = Encoding.UTF8
            }) {
                switch (proxyType) {
                    case ProxyType.None:
                        webClient.Proxy = null;
                        break;
                    case ProxyType.System:
                        if (webClient.Proxy != null) {
                            webClient.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                        }
                        break;
                    case ProxyType.Manual:
                        webClient.Proxy = new WebProxy(App.UserSettings.ProxyHttpAddress, App.UserSettings.ProxyHttpPort);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(proxyType)); // Shouldn't happen
                }

                ct.ThrowIfCancellationRequested();

                try {
                    String htmlCode = await webClient.DownloadStringTaskAsync(url);
                    return htmlCode;
                } catch {
                    fLog($"Could not retrieve data for {url}", LogType.Error);
                    return null;
                }
            }
        }
    }
}