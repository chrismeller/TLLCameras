using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TLLCameras.Client
{
    public class Scraper
    {
        public const string BASE_URL = "http://ristmikud.tallinn.ee/last";

        public async Task<Stream> GetImage(int camera)
        {
            using (var http = new HttpClient())
            {
                var nonce = (DateTimeOffset.UtcNow - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                    .TotalSeconds;
                var response = await http.GetAsync($"{BASE_URL}/cam{camera:D3}.jpg?{nonce}");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
        }
    }
}