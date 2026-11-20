using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPlaylist.Server.Data;
using MyPlaylist.Server.Dtos;
using MyPlaylist.Server.Models;
using MyPlaylist.Server.Services;
namespace MyPlaylist.Server.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        // 같은 태그의 인기 플레이리스트에서 가져올 곡 수와 초안 전체 곡 수
        private const int MaxPlaylistTracks = 4;
        private const int MaxTracks = 10;
        // 가수 3명 × 검색 1회 = 일일 할당량 300
        private const int MaxQueries = 3;
        // 모음 영상을 걸러 내고도 남도록 넉넉히 받는다.
        private const int ResultsPerQuery = 8;
        // 한 시간짜리 모음·믹스 영상 제목에 자주 들어가는 말
        private static readonly string[] CompilationWords =
        {
            "playlist", "플레이리스트", "플리", "모음", "연속", "1시간", "hour", "mix", "full album", "full set", "kpop chart", "ai cover",
            "station", "24/7", "radio", "best of", "greatest hits", "most loved", "베스트", "전곡", "(full)"
        };
        private readonly AppDbContext db;
        private readonly OllamaService ai;
        private readonly YouTubeSearchService youtube;
        private readonly ILogger<AiController> logger;
        public AiController(AppDbContext context, OllamaService ollama,
        YouTubeSearchService youTube, ILogger<AiController> log)
        {
            db = context;
            ai = ollama;
            youtube = youTube;
            logger = log;
        }
        // POST: api/ai/playlist-draft  { "prompt": "비 오는 밤에 듣기 좋은 잔잔한 인디" }
        // AI가 제목·소개·태그·검색어를 만들고, 곡은 실제 데이터에서 고른다.
        // ① 같은 태그가 붙은 인기 공개 플레이리스트의 곡 ② AI 검색어로 찾은 YouTube 영상
        // 아무것도 저장하지 않는다. 사용자가 만들기 화면에서 확인한 뒤 저장한다.
        [Authorize]
        [HttpPost("playlist-draft")]
        public async Task<IActionResult> CreateDraft(AiDraftRequest request, CancellationToken token)
        {
            string prompt = request.Prompt.Trim();
            var knownTags = await db.PlaylistTags
            .Where(t => t.Playlist!.IsPublic)
            .GroupBy(t => t.TagName)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(30)
            .ToListAsync(token);
            knownTags.AddRange(PlaylistTags.Defaults.Where(d => !knownTags.Contains(d)));
            PlaylistIdea idea;
            try
            {
                idea = await ai.CreateIdeaAsync(prompt, knownTags, token);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return StatusCode(503, $"AI 모델({ai.Model})이 설치되어 있지 않습니다. 'ollama pull {ai.Model}'로 받아 주세요.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning("Ollama 호출 실패: {Message}", ex.Message);
                return StatusCode(503, "AI 서버(Ollama)가 실행 중이 아닙니다. Ollama를 켠 뒤 다시 시도해 주세요.");
            }
            catch (TaskCanceledException) when (!token.IsCancellationRequested)
            {
                logger.LogWarning("Ollama 응답 시간 초과");
                return StatusCode(504, "AI 응답 시간이 초과되었습니다. 잠시 후 다시 시도해 주세요.");
            }
            catch (JsonException)
            {
                return StatusCode(502, "AI가 올바른 형식으로 답하지 않았습니다. 다시 시도해 주세요.");
            }
            var draft = new AiDraftResponse
            {
                PlaylistTitle = Cut(idea.Title.Length > 0 ? idea.Title : prompt, 100),
                Description = idea.Description.Length > 0 ? Cut(idea.Description, 500) : null,
                Tags = CleanTags(idea.Tags, knownTags),
                SearchQueries = idea.SearchQueries
                .Select(q => Cut(q, 100))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxQueries)
                .ToList()
            };
            // 검색어를 못 만들었으면 사용자가 쓴 문장으로 검색한다.
            if (draft.SearchQueries.Count == 0)
                draft.SearchQueries.Add(Cut(prompt, 100));
            var seen = new HashSet<string>();
            await AddPlaylistTracks(draft, seen, token);
            await AddYouTubeTracks(draft, seen);
            return Ok(draft);
        }
        // 같은 태그의 인기 공개 플레이리스트(좋아요순 5개)에서 플레이리스트마다 돌아가며 곡을 고른다.
        private async Task AddPlaylistTracks(AiDraftResponse draft, HashSet<string> seen, CancellationToken token)
        {
            if (draft.Tags.Count == 0)
                return;
            var tags = draft.Tags;
            var playlistIds = await db.Playlists
            .Where(p => p.IsPublic && p.Tags.Any(t => tags.Contains(t.TagName)))
            .OrderByDescending(p => p.Likes.Count)
            .ThenByDescending(p => p.PlaylistId)
            .Select(p => p.PlaylistId)
            .Take(5)
            .ToListAsync(token);
            var tracks = await db.PlaylistTracks
            .AsNoTracking()
            .Where(t => playlistIds.Contains(t.PlaylistId))
            .OrderBy(t => t.SortOrder)
            .ToListAsync(token);
            var queues = playlistIds
            .Select(id => new Queue<PlaylistTrack>(tracks.Where(t => t.PlaylistId == id)))
            .ToList();
            int added = 0;
            while (added < MaxPlaylistTracks && queues.Any(q => q.Count > 0))
            {
                foreach (var queue in queues.Where(q => q.Count > 0))
                {
                    var track = queue.Dequeue();
                    if (added >= MaxPlaylistTracks || !seen.Add(track.YoutubeVideoId))
                        continue;
                    draft.Tracks.Add(new AiDraftTrack
                    {
                        YoutubeVideoId = track.YoutubeVideoId,
                        MusicTitle = track.MusicTitle,
                        Artist = track.Artist,
                        ThumbnailUrl = track.ThumbnailUrl,
                        Source = "playlist"
                    });
                    added++;
                }
            }
        }
        // AI가 고른 가수 이름으로 YouTube를 검색해 가수마다 고르게 곡을 담는다.
        private async Task AddYouTubeTracks(AiDraftResponse draft, HashSet<string> seen)
        {
            if (!youtube.IsConfigured)
            {
                draft.Notice = "YouTube API 키가 없어 인기 플레이리스트의 곡만 담았어요.";
                return;
            }
            // 가수 몫을 넘어 담지 못한 영상. 다른 가수 검색이 비었을 때 빈자리를 채운다.
            var spare = new List<YouTubeSearchResult>();
            try
            {
                for (int i = 0; i < draft.SearchQueries.Count; i++)
                {
                    // 남은 자리를 남은 가수 수로 나눠, 앞 가수가 자리를 다 차지하지 않게 한다.
                    int share = (int)Math.Ceiling((MaxTracks - draft.Tracks.Count) / (double)(draft.SearchQueries.Count - i));
                    int taken = 0;
                    var names = ArtistNames(draft.SearchQueries[i]);
                    // "한글(영문)"의 영문은 틀릴 때가 있어 첫 이름으로만 검색한다.
                    string query = draft.SearchQueries[i].Split('(')[0].Trim();
                    var results = (await youtube.SearchAsync(query.Length > 0 ? query : draft.SearchQueries[i], ResultsPerQuery))
                    .Where(r => !IsCompilation(r.MusicTitle))
                    .ToList();
                    // 제목이나 채널에 그 가수 이름이 없는 영상은 AI가 가수가 아닌 말을 넣었을 때 섞인 것이다.
                    var matched = results.Where(r => MentionsArtist(r, names)).ToList();
                    // 다만 "악동뮤지션"으로 찾으면 제목이 "AKMU"로만 나오듯 표기가 다를 수 있다.
                    // 이름이 하나도 안 맞으면, 같은 채널 영상이 3개 이상 나온 경우 그 채널을 가수의 공식 채널로 본다.
                    if (matched.Count == 0)
                    {
                        var official = results.GroupBy(r => r.Artist)
                        .Where(g => g.Count() >= 3)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();
                        if (official != null)
                            matched = official.ToList();
                    }
                    foreach (var result in matched)
                    {
                        if (seen.Contains(result.YoutubeVideoId))
                            continue;
                        if (taken >= share || draft.Tracks.Count >= MaxTracks)
                        {
                            spare.Add(result);
                            continue;
                        }
                        taken++;
                        AddYouTubeTrack(draft, seen, result);
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // YouTube가 안 돼도 AI가 만든 제목·태그와 플레이리스트 곡은 돌려준다.
                logger.LogWarning("AI 초안용 YouTube 검색 실패: {Message}", ex.Message);
                draft.Notice = "YouTube 검색에 실패해 일부 곡만 담았어요.";
            }
            foreach (var result in spare)
            {
                if (draft.Tracks.Count >= MaxTracks)
                    break;
                AddYouTubeTrack(draft, seen, result);
            }
        }
        private static void AddYouTubeTrack(AiDraftResponse draft, HashSet<string> seen, YouTubeSearchResult result)
        {
            if (!seen.Add(result.YoutubeVideoId))
                return;
            draft.Tracks.Add(new AiDraftTrack
            {
                YoutubeVideoId = result.YoutubeVideoId,
                MusicTitle = Cut(result.MusicTitle, 200),
                Artist = Cut(result.Artist, 200),
                ThumbnailUrl = result.ThumbnailUrl,
                Source = "youtube"
            });
        }
        // 플레이리스트 저장 규칙(최대 3개, 20자, 한글·영문·숫자·공백)에 맞는 태그만 남긴다.
        private static List<string> CleanTags(List<string> tags, List<string> knownTags)
        {
            return tags
            .Select(PlaylistTags.Normalize)
            .Where(t => t.Length > 0 && t.Length <= PlaylistTags.MaxLength && PlaylistTags.Allowed.IsMatch(t))
            // 대소문자만 다른 기존 태그가 있으면 그 이름을 쓴다.
            .Select(t => knownTags.FirstOrDefault(k => string.Equals(k, t, StringComparison.OrdinalIgnoreCase)) ?? t)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(PlaylistTags.MaxPerPlaylist)
            .ToList();
        }
        // 𝗣𝗟𝗔𝗬𝗟𝗜𝗦𝗧 같은 꾸민 글자도 잡도록 일반 글자로 바꿔 비교한다.
        private static bool IsCompilation(string title)
        {
            string text = title.Normalize(System.Text.NormalizationForm.FormKC).ToLowerInvariant();
            return CompilationWords.Any(w => text.Contains(w));
        }
        // "악동뮤지션(AKMU)" → ["악동뮤지션", "akmu"] (공백을 빼고 소문자로 비교한다)
        private static List<string> ArtistNames(string query)
        {
            return query.Split('(', ')', '/', ',')
            .Select(Simplify)
            .Where(n => n.Length > 0)
            .ToList();
        }
        private static bool MentionsArtist(YouTubeSearchResult result, List<string> names)
        {
            string text = Simplify(result.MusicTitle + result.Artist);
            return names.Any(n => text.Contains(n));
        }
        private static string Simplify(string text)
        {
            return string.Concat(text.Normalize(System.Text.NormalizationForm.FormKC)
            .ToLowerInvariant()
            .Where(c => !char.IsWhiteSpace(c)));
        }
        private static string Cut(string text, int max)
        {
            text = text.Trim();
            return text.Length <= max ? text : text[..max];
        }
    }
}
