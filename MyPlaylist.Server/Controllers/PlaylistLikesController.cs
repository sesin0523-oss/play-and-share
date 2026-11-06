using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPlaylist.Server.Auth;
using MyPlaylist.Server.Data;
using MyPlaylist.Server.Models;
using MySqlConnector;
namespace MyPlaylist.Server.Controllers
{
    [ApiController]
    [Route("api/playlists/{playlistId:int}/likes")]
    public class PlaylistLikesController : ControllerBase
    {
        private readonly AppDbContext db;
        public PlaylistLikesController(AppDbContext context)
        {
            db = context;
        }
        // POST: api/playlists/1/likes
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(int playlistId)
        {
            int userId = User.GetUserId()!.Value;
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            if (!playlist.IsPublic && playlist.UserId != userId)
                return StatusCode(403, "비공개 플레이리스트입니다.");
            // 중복 좋아요 확인
            bool duplicated = await db.PlaylistLikes.AnyAsync(
            l => l.UserId == userId && l.PlaylistId == playlistId);
            if (duplicated)
                return Conflict("이미 좋아요를 누른 플레이리스트입니다.");
            db.PlaylistLikes.Add(new PlaylistLike
            {
                UserId = userId,
                PlaylistId = playlistId,
                CreatedAt = DateTime.Now
            });
            try
            {
                await db.SaveChangesAsync();
            }
            // 동시에 요청이 들어와 위의 확인을 통과한 경우 UNIQUE 제약조건이 막아 준다.
            catch (DbUpdateException ex) when (
            ex.InnerException is MySqlException
            {
                ErrorCode: MySqlErrorCode.DuplicateKeyEntry
            })
            {
                return Conflict("이미 좋아요를 누른 플레이리스트입니다.");
            }
            return StatusCode(201);
        }
        // DELETE: api/playlists/1/likes
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> Delete(int playlistId)
        {
            int userId = User.GetUserId()!.Value;
            var like = await db.PlaylistLikes.FirstOrDefaultAsync(
            l => l.UserId == userId && l.PlaylistId == playlistId);
            if (like == null)
                return NotFound("좋아요 기록이 없습니다.");
            db.PlaylistLikes.Remove(like);
            await db.SaveChangesAsync();
            return NoContent();
        }
        // GET: api/playlists/1/likes/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMine(int playlistId)
        {
            int userId = User.GetUserId()!.Value;
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            if (!playlist.IsPublic && playlist.UserId != userId)
                return StatusCode(403, "비공개 플레이리스트입니다.");
            bool liked = await db.PlaylistLikes.AnyAsync(
            l => l.UserId == userId && l.PlaylistId == playlistId);
            return Ok(new { liked });
        }
        // GET: api/playlists/1/likes/count
        [HttpGet("count")]
        public async Task<IActionResult> GetCount(int playlistId)
        {
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            if (!playlist.IsPublic && playlist.UserId != User.GetUserId())
                return StatusCode(403, "비공개 플레이리스트입니다.");
            int likeCount = await db.PlaylistLikes.CountAsync(
            l => l.PlaylistId == playlistId);
            return Ok(new { likeCount });
        }
    }
}
