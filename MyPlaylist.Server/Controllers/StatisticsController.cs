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
    [Route("api/statistics")]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext db;
        public StatisticsController(AppDbContext context)
        {
            db = context;
        }
        // GET: api/statistics/users/count
        [HttpGet("users/count")]
        public async Task<IActionResult> GetUserCount()
        {
            int userCount = await db.Users.CountAsync();
            return Ok(new { userCount });
        }
        // GET: api/statistics/playlists/count
        [HttpGet("playlists/count")]
        public async Task<IActionResult> GetPlaylistCount()
        {
            int playlistCount = await db.Playlists.CountAsync();
            return Ok(new { playlistCount });
        }
        // GET: api/statistics/playlists/views
        [HttpGet("playlists/views")]
        public async Task<IActionResult> GetTotalViews()
        {
            int totalViewCount = await db.PlaylistViews.CountAsync();
            return Ok(new { totalViewCount });
        }
        // GET: api/statistics/playlists/likes
        [HttpGet("playlists/likes")]
        public async Task<IActionResult> GetTotalLikes()
        {
            int totalLikeCount = await db.PlaylistLikes.CountAsync();
            return Ok(new { totalLikeCount });
        }
        // GET: api/statistics/playlists/popular?sort=likes&limit=10
        // 공개 플레이리스트만 순위에 포함한다.
        [HttpGet("playlists/popular")]
        public async Task<IActionResult> GetPopular(
        string sort = "likes",
        [Range(1, 50)] int limit = 10)
        {
            var query = db.Playlists
            .AsNoTracking()
            .Where(p => p.IsPublic);
            IOrderedQueryable<Playlist> ordered;
            switch (sort)
            {
                case "likes":
                    ordered = query
                    .OrderByDescending(p => p.Likes.Count)
                    .ThenByDescending(p => p.Views.Count);
                    break;
                case "views":
                    ordered = query
                    .OrderByDescending(p => p.Views.Count)
                    .ThenByDescending(p => p.Likes.Count);
                    break;
                default:
                    return BadRequest("sort는 likes, views 중 하나여야 합니다.");
            }
            var list = await ordered
            .ThenByDescending(p => p.PlaylistId)
            .Take(limit)
            .Select(PlaylistSummary.FromEntity)
            .ToListAsync();
            return Ok(list);
        }
        // GET: api/statistics/users/me
        [Authorize]
        [HttpGet("users/me")]
        public async Task<IActionResult> GetMyStatistics()
        {
            int userId = User.GetUserId()!.Value;
            int playlistCount = await db.Playlists
            .CountAsync(p => p.UserId == userId);
            int trackCount = await db.PlaylistTracks
            .CountAsync(t => t.Playlist!.UserId == userId);
            // 내가 누른 좋아요
            int givenLikeCount = await db.PlaylistLikes
            .CountAsync(l => l.UserId == userId);
            // 내 플레이리스트가 받은 좋아요
            int receivedLikeCount = await db.PlaylistLikes
            .CountAsync(l => l.Playlist!.UserId == userId);
            // 내 플레이리스트가 받은 조회수
            int totalViewCount = await db.PlaylistViews
            .CountAsync(v => v.Playlist!.UserId == userId);
            return Ok(new
            {
                playlistCount,
                trackCount,
                givenLikeCount,
                receivedLikeCount,
                totalViewCount
            });
        }
    }
}
