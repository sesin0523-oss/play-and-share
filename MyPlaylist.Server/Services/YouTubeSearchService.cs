using System.Net;
using System.Text.Json;
using MyPlaylist.Server.Dtos;
namespace MyPlaylist.Server.Services
{
    // YouTube Data API v3 음악 검색
    public class YouTubeSearchService
    {
        private const string SearchUrl =
        "https://www.googleapis.com/youtube/v3/search";
        // YouTube의 음악 카테고리 번호
        private const string MusicCategoryId = "10";
        private readonly HttpClient http;
        private readonly IConfiguration configuration;
        public YouTubeSearchService(
        HttpClient client, IConfiguration config)
        {
            http = client;
            configuration = config;
        }
        public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["YouTube:ApiKey"]);
        public async Task<List<YouTubeSearchResult>> SearchAsync(
        string keyword, int maxResults)
        {
            string url = SearchUrl
            + "?part=snippet&type=video"
            + $"&videoCategoryId={MusicCategoryId}"
            + $"&maxResults={maxResults}"
            + $"&q={Uri.EscapeDataString(keyword)}";
            // 주소에 넣으면 로그에 키가 남을 수 있으므로 헤더로 보낸다.
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add(
            "X-Goog-Api-Key", configuration["YouTube:ApiKey"]);
            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);
            var results = new List<YouTubeSearchResult>();
            if (!json.RootElement.TryGetProperty("items", out var items))
                return results;
            foreach (var item in items.EnumerateArray())
            {
                // type=video로 검색해도 채널, 재생목록 결과가 섞여 오는 경우가 있다.
                // 이런 항목에는 videoId가 없으므로 건너뛴다.
                if (!item.TryGetProperty("id", out var id)
                || !id.TryGetProperty("videoId", out var videoIdElement)
                || !item.TryGetProperty("snippet", out var snippet))
                    continue;
                string videoId = videoIdElement.GetString() ?? "";
                if (videoId.Length == 0)
                    continue;
                results.Add(new YouTubeSearchResult
                {
                    YoutubeVideoId = videoId,
                    // YouTube는 제목을 &#39; 같은 HTML 형식으로 돌려준다.
                    MusicTitle = WebUtility.HtmlDecode(GetText(snippet, "title")),
                    Artist = CleanChannelTitle(GetText(snippet, "channelTitle")),
                    ThumbnailUrl =
                    $"https://i.ytimg.com/vi/{videoId}/mqdefault.jpg",
                    YoutubeUrl =
                    $"https://www.youtube.com/watch?v={videoId}"
                });
            }
            return results;
        }
        private static string GetText(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value)
            ? value.GetString() ?? "" : "";
        }
        // 음원 전용 채널은 "아이유 - Topic" 형식이다.
        private static string CleanChannelTitle(string channelTitle)
        {
            string title = WebUtility.HtmlDecode(channelTitle);
            const string suffix = " - Topic";
            return title.EndsWith(suffix)
            ? title[..^suffix.Length]
            : title;
        }
    }
}
