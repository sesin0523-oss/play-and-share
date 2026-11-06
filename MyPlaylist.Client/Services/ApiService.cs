using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MyPlaylist.Client.Models;
namespace MyPlaylist.Client.Services
{
    // 서버가 실패 응답을 보냈을 때 상태 코드와 메시지를 전달한다.
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public ApiException(HttpStatusCode statusCode, string message)
        : base(message)
        {
            StatusCode = statusCode;
        }
    }
    public class ApiService
    {
        private readonly HttpClient client;
        public ApiService()
        {
            // 로그인 쿠키를 유지하도록 HttpClient 하나를 계속 사용한다.
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };
            string serverUrl =
            Environment.GetEnvironmentVariable("MYPLAYLIST_SERVER_URL")
            ?? "http://localhost:5080";
            client = new HttpClient(handler)
            {
                BaseAddress = new Uri(serverUrl),
                Timeout = TimeSpan.FromSeconds(20)
            };
        }
        // ----------------------
        // 회원
        // ----------------------
        public async Task<UserInfo> Signup(string userName, string password)
        {
            var response = await client.PostAsJsonAsync(
            "api/users/signup", new { userName, password });
            return await Read<UserInfo>(response);
        }
        public async Task<UserInfo> Login(string userName, string password)
        {
            var response = await client.PostAsJsonAsync(
            "api/users/login", new { userName, password });
            return await Read<UserInfo>(response);
        }
        public async Task Logout()
        {
            await EnsureSuccess(await client.PostAsync("api/users/logout", null));
        }
        public async Task<bool> CheckName(string userName)
        {
            var response = await client.GetAsync(
            $"api/users/check-name?userName={Uri.EscapeDataString(userName)}");
            var result = await Read<JsonElement>(response);
            return result.GetProperty("available").GetBoolean();
        }
        public async Task UpdateUser(int userId, string userName,
        string? currentPassword, string? newPassword)
        {
            var response = await client.PutAsJsonAsync(
            $"api/users/{userId}",
            new { userName, currentPassword, newPassword });
            await EnsureSuccess(response);
        }
        public async Task DeleteUser(int userId)
        {
            await EnsureSuccess(await client.DeleteAsync($"api/users/{userId}"));
        }
        // ----------------------
        // 플레이리스트
        // ----------------------
        public async Task<PlaylistSearchResult> SearchPlaylists(
        string? keyword, string sort, int page, int size, int offset = 0, int days = 0,
        string? tag = null)
        {
            // days: 0 전체 기간, 7 / 30 최근 N일 동안의 좋아요·클릭으로 순위를 매긴다.
            // keyword: 플레이리스트 제목, 아이디, 곡 제목, 가수 / tag: 태그로 걸러 보기
            string url = $"api/playlists?sort={sort}&page={page}&size={size}&offset={offset}&days={days}";
            if (!string.IsNullOrWhiteSpace(keyword))
                url += $"&keyword={Uri.EscapeDataString(keyword)}";
            if (!string.IsNullOrWhiteSpace(tag))
                url += $"&tag={Uri.EscapeDataString(tag)}";
            return await Read<PlaylistSearchResult>(await client.GetAsync(url));
        }
        // 고를 수 있는 태그 목록
        public async Task<List<string>> GetTags()
        {
            return await Read<List<string>>(await client.GetAsync("api/playlists/tags"));
        }
        // 취향을 더 넓혀보세요: 아직 열어보지 않은 플레이리스트 (sort: latest / likes)
        public async Task<List<DiscoverItem>> GetDiscover(string sort, int size)
        {
            return await Read<List<DiscoverItem>>(await client.GetAsync(
            $"api/playlists/discover?sort={sort}&size={size}"));
        }
        // 오늘의 추천 (공개 플레이리스트가 없으면 null)
        public async Task<PlaylistSummary?> GetTodayPick()
        {
            var response = await client.GetAsync("api/playlists/today");
            if (response.StatusCode == HttpStatusCode.NoContent)
                return null;
            return await Read<PlaylistSummary>(response);
        }
        // 호출할 때마다 클릭 수가 1 오르므로 화면을 처음 열 때만 호출한다.
        public async Task<PlaylistDetail> GetPlaylistDetail(int playlistId)
        {
            return await Read<PlaylistDetail>(
            await client.GetAsync($"api/playlists/{playlistId}"));
        }
        public async Task<List<PlaylistSummary>> GetMyPlaylists()
        {
            return await Read<List<PlaylistSummary>>(
            await client.GetAsync("api/users/me/playlists"));
        }
        public async Task<List<PlaylistSummary>> GetLikedPlaylists()
        {
            return await Read<List<PlaylistSummary>>(
            await client.GetAsync("api/users/me/likes"));
        }
        // 최근 열어본 플레이리스트 (최근 순, 최대 20개)
        public async Task<List<PlaylistSummary>> GetRecentPlaylists()
        {
            return await Read<List<PlaylistSummary>>(
            await client.GetAsync("api/users/me/recent"));
        }
        public async Task<PlaylistSummary> CreatePlaylist(
        string playlistTitle, string? description, bool isPublic, List<string> tags)
        {
            var response = await client.PostAsJsonAsync(
            "api/playlists", new { playlistTitle, description, isPublic, tags });
            return await Read<PlaylistSummary>(response);
        }
        public async Task UpdatePlaylist(int playlistId,
        string playlistTitle, string? description, bool isPublic, List<string> tags)
        {
            var response = await client.PutAsJsonAsync(
            $"api/playlists/{playlistId}",
            new { playlistTitle, description, isPublic, tags });
            await EnsureSuccess(response);
        }
        public async Task DeletePlaylist(int playlistId)
        {
            await EnsureSuccess(
            await client.DeleteAsync($"api/playlists/{playlistId}"));
        }
        // ----------------------
        // 곡
        // ----------------------
        public async Task<List<PlaylistTrack>> GetTracks(int playlistId)
        {
            return await Read<List<PlaylistTrack>>(
            await client.GetAsync($"api/playlists/{playlistId}/tracks"));
        }
        public async Task AddTrack(int playlistId,
        string youtubeVideoId, string musicTitle, string? artist)
        {
            var response = await client.PostAsJsonAsync(
            $"api/playlists/{playlistId}/tracks",
            new { youtubeVideoId, musicTitle, artist });
            await EnsureSuccess(response);
        }
        public async Task UpdateTrack(int playlistId, int trackId,
        string youtubeVideoId, string musicTitle, string? artist)
        {
            var response = await client.PutAsJsonAsync(
            $"api/playlists/{playlistId}/tracks/{trackId}",
            new { youtubeVideoId, musicTitle, artist });
            await EnsureSuccess(response);
        }
        public async Task DeleteTrack(int playlistId, int trackId)
        {
            await EnsureSuccess(await client.DeleteAsync(
            $"api/playlists/{playlistId}/tracks/{trackId}"));
        }
        public async Task ChangeTrackOrder(int playlistId, List<int> trackIds)
        {
            var request = new HttpRequestMessage(
            HttpMethod.Patch, $"api/playlists/{playlistId}/tracks/order")
            {
                Content = JsonContent.Create(new { trackIds })
            };
            await EnsureSuccess(await client.SendAsync(request));
        }
        // ----------------------
        // 댓글
        // ----------------------
        public async Task<List<PlaylistComment>> GetComments(int playlistId)
        {
            return await Read<List<PlaylistComment>>(
            await client.GetAsync($"api/playlists/{playlistId}/comments"));
        }
        public async Task AddComment(int playlistId, string content)
        {
            var response = await client.PostAsJsonAsync(
            $"api/playlists/{playlistId}/comments", new { content });
            await EnsureSuccess(response);
        }
        public async Task DeleteComment(int playlistId, int commentId)
        {
            await EnsureSuccess(await client.DeleteAsync(
            $"api/playlists/{playlistId}/comments/{commentId}"));
        }
        // ----------------------
        // 좋아요
        // ----------------------
        public async Task AddLike(int playlistId)
        {
            await EnsureSuccess(
            await client.PostAsync($"api/playlists/{playlistId}/likes", null));
        }
        public async Task RemoveLike(int playlistId)
        {
            await EnsureSuccess(
            await client.DeleteAsync($"api/playlists/{playlistId}/likes"));
        }
        public async Task<bool> IsLiked(int playlistId)
        {
            var result = await Read<JsonElement>(
            await client.GetAsync($"api/playlists/{playlistId}/likes/me"));
            return result.GetProperty("liked").GetBoolean();
        }
        public async Task<int> GetLikeCount(int playlistId)
        {
            var result = await Read<JsonElement>(
            await client.GetAsync($"api/playlists/{playlistId}/likes/count"));
            return result.GetProperty("likeCount").GetInt32();
        }
        // ----------------------
        // 통계
        // ----------------------
        public async Task<TotalStatistics> GetTotalStatistics()
        {
            var users = GetCount("api/statistics/users/count", "userCount");
            var playlists = GetCount("api/statistics/playlists/count", "playlistCount");
            var views = GetCount("api/statistics/playlists/views", "totalViewCount");
            var likes = GetCount("api/statistics/playlists/likes", "totalLikeCount");
            return new TotalStatistics
            {
                UserCount = await users,
                PlaylistCount = await playlists,
                TotalViewCount = await views,
                TotalLikeCount = await likes
            };
        }
        public async Task<MyStatistics> GetMyStatistics()
        {
            return await Read<MyStatistics>(
            await client.GetAsync("api/statistics/users/me"));
        }
        private async Task<int> GetCount(string url, string name)
        {
            var result = await Read<JsonElement>(await client.GetAsync(url));
            return result.GetProperty(name).GetInt32();
        }
        // ----------------------
        // YouTube 검색
        // ----------------------
        public async Task<List<YouTubeResult>> SearchYouTube(string keyword)
        {
            return await Read<List<YouTubeResult>>(await client.GetAsync(
            $"api/youtube/search?keyword={Uri.EscapeDataString(keyword)}&maxResults=10"));
        }
        // ----------------------
        // 공통 처리
        // ----------------------
        // 목록에 쓸 썸네일을 첫 번째 곡에서 가져온다. 클릭 수가 오르지 않는 API를 사용한다.
        public async Task FillThumbnails(IEnumerable<PlaylistSummary> playlists)
        {
            await Task.WhenAll(playlists.Select(async playlist =>
            {
                var tracks = await GetTracks(playlist.PlaylistId);
                playlist.ThumbnailUrl = tracks.FirstOrDefault()?.ThumbnailUrl;
            }));
        }
        private static async Task<T> Read<T>(HttpResponseMessage response)
        {
            await EnsureSuccess(response);
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result!;
        }
        private static async Task EnsureSuccess(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;
            string body = await response.Content.ReadAsStringAsync();
            throw new ApiException(
            response.StatusCode, GetMessage(response.StatusCode, body));
        }
        private static string GetMessage(HttpStatusCode status, string body)
        {
            // 입력값 검증 실패는 JSON으로, 나머지는 한글 문장으로 온다.
            if (!string.IsNullOrWhiteSpace(body) && !body.TrimStart().StartsWith('{'))
                return body;
            return status switch
            {
                HttpStatusCode.BadRequest => "입력값을 확인해 주세요.",
                HttpStatusCode.Unauthorized => "로그인이 필요합니다.",
                HttpStatusCode.Forbidden => "권한이 없습니다.",
                HttpStatusCode.NotFound => "대상을 찾을 수 없습니다.",
                _ => "요청을 처리하지 못했습니다."
            };
        }
    }
}
