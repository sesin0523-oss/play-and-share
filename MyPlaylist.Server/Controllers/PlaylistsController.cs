using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPlaylist.Server.Auth;
using MyPlaylist.Server.Data;
using MyPlaylist.Server.Dtos;
using MyPlaylist.Server.Models;
namespace MyPlaylist.Server.Controllers
{
    [ApiController]
    [Route("api/playlists")]
    public class PlaylistsController : ControllerBase
    {
        private readonly AppDbContext db;
        public PlaylistsController(AppDbContext context)
        {
            db = context;
        }
        // GET: api/playlists?keyword=새벽&sort=latest&page=1&size=20
        // 공개 플레이리스트를 제목 또는 제작자 닉네임으로 검색
        // offset은 페이지를 나누기 전에 건너뛸 개수이다. 메인 화면은 1위를 따로 보여 주므로 1을 보낸다.
        // days(7 또는 30)를 주면 좋아요순 / 클릭순을 최근 N일 동안 쌓인 수로 정렬하고,
        // 그 기간에 좋아요(클릭순은 클릭)가 하나도 없는 플레이리스트는 뺀다. 0이면 전체 기간.
        [HttpGet]
        public async Task<IActionResult> Search(
        string? keyword,
        string sort = "latest",
        [Range(1, int.MaxValue)] int page = 1,
        [Range(1, 100)] int size = 20,
        [Range(0, 100)] int offset = 0,
        int days = 0,
        string? tag = null)
        {
            if (days is not (0 or 7 or 30))
                return BadRequest("days는 0, 7, 30 중 하나여야 합니다.");
            var query = db.Playlists
            .AsNoTracking()
            .Where(p => p.IsPublic);
            // 플레이리스트 제목, 만든 사람 아이디, 담긴 곡의 제목과 가수로 검색한다.
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string word = keyword.Trim();
                query = query.Where(p =>
                p.PlaylistTitle.Contains(word)
                || p.User!.UserName.Contains(word)
                || p.Tracks.Any(t => t.MusicTitle.Contains(word)
                || (t.Artist != null && t.Artist.Contains(word))));
            }
            // 태그로 걸러 보기 (메인 화면의 칩)
            if (!string.IsNullOrWhiteSpace(tag))
            {
                string tagName = tag.Trim();
                query = query.Where(p => p.Tags.Any(t => t.TagName == tagName));
            }
            // 전체 기간이면 아주 오래전부터 센다. (조건식을 하나로 쓰기 위해)
            var since = days == 0 ? DateTime.MinValue : DateTime.Now.AddDays(-days);
            IOrderedQueryable<Playlist> ordered;
            switch (sort)
            {
                case "latest":
                    ordered = query.OrderByDescending(p => p.CreatedAt);
                    break;
                case "views":
                    if (days > 0)
                        query = query.Where(p => p.Views.Any(v => v.ViewedAt >= since));
                    ordered = query
                    .OrderByDescending(p => p.Views.Count(v => v.ViewedAt >= since))
                    .ThenByDescending(p => p.Likes.Count(l => l.CreatedAt >= since));
                    break;
                case "likes":
                    if (days > 0)
                        query = query.Where(p => p.Likes.Any(l => l.CreatedAt >= since));
                    ordered = query
                    .OrderByDescending(p => p.Likes.Count(l => l.CreatedAt >= since))
                    .ThenByDescending(p => p.Views.Count(v => v.ViewedAt >= since));
                    break;
                default:
                    return BadRequest("sort는 latest, views, likes 중 하나여야 합니다.");
            }
            int totalCount = await query.CountAsync();
            var items = await ordered
            .ThenByDescending(p => p.PlaylistId)
            .Skip(offset + (page - 1) * size)
            .Take(size)
            // 기간을 고른 경우 목록에 표시하는 좋아요 / 클릭 수도 그 기간의 수로 보낸다.
            .Select(days == 0 || sort == "latest"
            ? PlaylistSummary.FromEntity
            : PlaylistSummary.FromEntitySince(since))
            .ToListAsync();
            return Ok(new { page, size, offset, days, tag, totalCount, items });
        }
        // GET: api/playlists/tags
        // 고를 수 있는 태그 목록: 공개 플레이리스트에 많이 쓰인 순서 + 아직 안 쓰인 기본 태그
        [HttpGet("tags")]
        public async Task<IActionResult> GetTags()
        {
            var used = await db.PlaylistTags
            .AsNoTracking()
            .Where(t => t.Playlist!.IsPublic)
            .GroupBy(t => t.TagName)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Name)
            .Take(100)
            .ToListAsync();
            var names = used.Select(t => t.Name).ToList();
            names.AddRange(PlaylistTags.Defaults.Where(d => !names.Contains(d)));
            return Ok(names);
        }
        // GET: api/playlists/discover?sort=latest&size=3
        // 취향을 더 넓혀보세요: 내가 한 번도 열어보지 않은 공개 플레이리스트를 추천한다. (내 플레이리스트 제외)
        // 그중에서도 내가 열어본 플레이리스트에 없던 태그가 붙은 것을 먼저 보여 준다.
        // 비로그인은 열어본 기록을 알 수 없으므로 공개 플레이리스트를 정렬 순서대로 보여 준다.
        [HttpGet("discover")]
        public async Task<IActionResult> Discover(string sort = "latest", [Range(1, 12)] int size = 3)
        {
            if (sort is not ("latest" or "likes"))
                return BadRequest("sort는 latest, likes 중 하나여야 합니다.");
            int? userId = User.GetUserId();
            var viewedIds = userId == null ? new List<int>()
            : await db.PlaylistViews
            .Where(v => v.UserId == userId)
            .Select(v => v.PlaylistId)
            .Distinct()
            .ToListAsync();
            // 내가 이미 만나 본 태그 = 열어본 플레이리스트에 붙어 있던 태그
            var knownTags = (userId == null ? new List<string>()
            : await db.PlaylistTags
            .Where(t => viewedIds.Contains(t.PlaylistId))
            .Select(t => t.TagName)
            .Distinct()
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidates = await db.Playlists
            .AsNoTracking()
            .Where(p => p.IsPublic && p.Tracks.Any()
            && !viewedIds.Contains(p.PlaylistId)
            && (userId == null || p.UserId != userId))
            .Select(PlaylistSummary.FromEntity)
            .ToListAsync();
            var items = candidates
            .Select(p => new DiscoverItem
            {
                Playlist = p,
                NewTags = userId == null ? new()
                : p.Tags.Where(t => !knownTags.Contains(t)).ToList()
            })
            .OrderByDescending(x => x.NewTags.Count > 0)
            .ThenByDescending(x => sort == "likes" ? x.Playlist.LikeCount : 0)
            .ThenByDescending(x => x.Playlist.CreatedAt)
            .ThenByDescending(x => x.Playlist.PlaylistId)
            .Take(size)
            .ToList();
            return Ok(items);
        }
        // GET: api/playlists/today
        // 오늘의 추천: 곡이 1개 이상 있는 공개 플레이리스트 중 하나를 날짜에 따라 고른다.
        // 같은 날에는 누가 봐도 같은 플레이리스트가 나오고, 날짜가 바뀌면 다음 플레이리스트로 넘어간다.
        [HttpGet("today")]
        public async Task<IActionResult> GetToday()
        {
            var ids = await db.Playlists
            .AsNoTracking()
            .Where(p => p.IsPublic && p.Tracks.Any())
            .OrderBy(p => p.PlaylistId)
            .Select(p => p.PlaylistId)
            .ToListAsync();
            if (ids.Count == 0)
                return NoContent();
            int dayNumber = DateOnly.FromDateTime(DateTime.Today).DayNumber;
            int pickId = ids[dayNumber % ids.Count];
            var pick = await db.Playlists
            .AsNoTracking()
            .Where(p => p.PlaylistId == pickId)
            .Select(PlaylistSummary.FromEntity)
            .FirstAsync();
            return Ok(pick);
        }
        // GET: api/users/me/playlists
        [Authorize]
        [HttpGet("/api/users/me/playlists")]
        public async Task<IActionResult> GetMine()
        {
            int userId = User.GetUserId()!.Value;
            var list = await db.Playlists
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PlaylistId)
            .Select(PlaylistSummary.FromEntity)
            .ToListAsync();
            return Ok(list);
        }
        // GET: api/users/me/likes
        // 좋아요한 뒤 비공개로 바뀐 다른 회원의 플레이리스트는 제외한다.
        [Authorize]
        [HttpGet("/api/users/me/likes")]
        public async Task<IActionResult> GetLiked()
        {
            int userId = User.GetUserId()!.Value;
            var list = await db.PlaylistLikes
            .AsNoTracking()
            .Where(l => l.UserId == userId
            && (l.Playlist!.IsPublic || l.Playlist.UserId == userId))
            .OrderByDescending(l => l.LikeId)
            .Select(l => l.Playlist!)
            .Select(PlaylistSummary.FromEntity)
            .ToListAsync();
            return Ok(list);
        }
        // GET: api/users/me/recent
        // 내가 최근에 열어본 플레이리스트 (상세 조회 기록 PLAYLIST_VIEW 사용)
        // 같은 플레이리스트는 마지막으로 연 한 번만, 최근 순으로 20개까지 보여 준다.
        // 열어본 뒤 비공개로 바뀐 다른 회원의 플레이리스트는 제외한다.
        [Authorize]
        [HttpGet("/api/users/me/recent")]
        public async Task<IActionResult> GetRecent()
        {
            int userId = User.GetUserId()!.Value;
            var recent = await db.PlaylistViews
            .AsNoTracking()
            .Where(v => v.UserId == userId
            && (v.Playlist!.IsPublic || v.Playlist.UserId == userId))
            .GroupBy(v => v.PlaylistId)
            .Select(g => new { PlaylistId = g.Key, LastViewedAt = g.Max(v => v.ViewedAt) })
            .OrderByDescending(r => r.LastViewedAt)
            .Take(20)
            .ToListAsync();
            var ids = recent.Select(r => r.PlaylistId).ToList();
            var playlists = await db.Playlists
            .AsNoTracking()
            .Where(p => ids.Contains(p.PlaylistId))
            .Select(PlaylistSummary.FromEntity)
            .ToListAsync();
            // DB에서 가져온 순서는 보장되지 않으므로 최근 순으로 다시 맞춘다.
            var list = ids
            .Select(id => playlists.First(p => p.PlaylistId == id))
            .ToList();
            return Ok(list);
        }
        // GET: api/playlists/1
        // 상세 조회 시 조회 기록이 남고 조회수가 1 증가한다. (비로그인 포함)
        [HttpGet("{playlistId:int}")]
        public async Task<IActionResult> GetById(int playlistId)
        {
            int? userId = User.GetUserId();
            var playlist = await db.Playlists
            .AsNoTracking()
            .Where(p => p.PlaylistId == playlistId)
            .Select(PlaylistSummary.FromEntity)
            .FirstOrDefaultAsync();
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            if (!playlist.IsPublic && playlist.UserId != userId)
                return StatusCode(403, "비공개 플레이리스트입니다.");
            db.PlaylistViews.Add(new PlaylistView
            {
                UserId = userId,
                PlaylistId = playlistId,
                ViewedAt = DateTime.Now
            });
            await db.SaveChangesAsync();
            playlist.ViewCount++;
            var tracks = await db.PlaylistTracks
            .AsNoTracking()
            .Where(t => t.PlaylistId == playlistId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.TrackId)
            .ToListAsync();
            bool likedByMe = userId != null
            && await db.PlaylistLikes.AnyAsync(l =>
            l.PlaylistId == playlistId && l.UserId == userId);
            return Ok(new { playlist, likedByMe, tracks });
        }
        // POST: api/playlists
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(PlaylistRequest request)
        {
            var tags = CleanTags(request.Tags, out string? tagError);
            if (tagError != null)
                return BadRequest(tagError);
            var now = DateTime.Now;
            var playlist = new Playlist
            {
                UserId = User.GetUserId()!.Value,
                PlaylistTitle = request.PlaylistTitle.Trim(),
                Description = request.Description,
                IsPublic = request.IsPublic,
                CreatedAt = now,
                UpdatedAt = now,
                Tags = (tags ?? new()).Select(t => new PlaylistTag { TagName = t }).ToList()
            };
            db.Playlists.Add(playlist);
            await db.SaveChangesAsync();
            return CreatedAtAction(
            nameof(GetById),
            new { playlistId = playlist.PlaylistId },
            new
            {
                playlist.PlaylistId,
                playlist.UserId,
                playlist.PlaylistTitle,
                playlist.Description,
                playlist.IsPublic,
                playlist.CreatedAt,
                playlist.UpdatedAt,
                Tags = playlist.Tags.Select(t => t.TagName).ToList()
            });
        }
        // 태그 요청값 확인: 사용자가 만든 태그도 받되, 20자 이내의 한글·영문·숫자·공백만, 중복 없이 최대 3개
        private static List<string>? CleanTags(List<string>? tags, out string? error)
        {
            error = null;
            if (tags == null)
                return null;
            var clean = tags.Select(PlaylistTags.Normalize)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
            if (clean.Any(t => t.Length > PlaylistTags.MaxLength))
                error = $"태그는 {PlaylistTags.MaxLength}자 이내로 입력해 주세요.";
            else if (clean.Any(t => !PlaylistTags.Allowed.IsMatch(t)))
                error = "태그에는 한글, 영문, 숫자, 공백만 쓸 수 있습니다.";
            else if (clean.Count > PlaylistTags.MaxPerPlaylist)
                error = $"태그는 최대 {PlaylistTags.MaxPerPlaylist}개까지 고를 수 있습니다.";
            return clean;
        }
        // PUT: api/playlists/1
        [Authorize]
        [HttpPut("{playlistId:int}")]
        public async Task<IActionResult> Update(
        int playlistId, PlaylistRequest request)
        {
            var tags = CleanTags(request.Tags, out string? tagError);
            if (tagError != null)
                return BadRequest(tagError);
            var playlist = await db.Playlists
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.PlaylistId == playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            // 소유자 확인
            if (playlist.UserId != User.GetUserId())
                return StatusCode(403, "본인의 플레이리스트만 수정할 수 있습니다.");
            // 태그를 보냈으면 통째로 바꾼다.
            if (tags != null)
            {
                db.PlaylistTags.RemoveRange(playlist.Tags.Where(t => !tags.Contains(t.TagName)));
                foreach (var name in tags.Where(n => playlist.Tags.All(t => t.TagName != n)))
                    playlist.Tags.Add(new PlaylistTag { TagName = name });
            }
            playlist.PlaylistTitle = request.PlaylistTitle.Trim();
            playlist.Description = request.Description;
            playlist.IsPublic = request.IsPublic;
            playlist.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();
            return NoContent();
        }
        // DELETE: api/playlists/1
        [Authorize]
        [HttpDelete("{playlistId:int}")]
        public async Task<IActionResult> Delete(int playlistId)
        {
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            // 소유자 확인
            if (playlist.UserId != User.GetUserId())
                return StatusCode(403, "본인의 플레이리스트만 삭제할 수 있습니다.");
            db.Playlists.Remove(playlist);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
