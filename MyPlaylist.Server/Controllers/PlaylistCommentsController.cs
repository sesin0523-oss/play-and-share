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
    [Route("api/playlists/{playlistId:int}/comments")]
    public class PlaylistCommentsController : ControllerBase
    {
        private readonly AppDbContext db;
        public PlaylistCommentsController(AppDbContext context)
        {
            db = context;
        }
        // GET: api/playlists/1/comments
        // 비로그인도 볼 수 있다. 오래된 댓글부터 순서대로 보여 준다.
        [HttpGet]
        public async Task<IActionResult> GetAll(int playlistId)
        {
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            if (!playlist.IsPublic && playlist.UserId != User.GetUserId())
                return StatusCode(403, "비공개 플레이리스트입니다.");
            var comments = await db.PlaylistComments
            .AsNoTracking()
            .Where(c => c.PlaylistId == playlistId)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.CommentId)
            .Select(CommentResponse.FromEntity)
            .ToListAsync();
            return Ok(comments);
        }
        // POST: api/playlists/1/comments
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(int playlistId, CommentRequest request)
        {
            int userId = User.GetUserId()!.Value;
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            if (!playlist.IsPublic && playlist.UserId != userId)
                return StatusCode(403, "비공개 플레이리스트입니다.");
            string content = request.Content.Trim();
            if (content.Length == 0)
                return BadRequest("댓글 내용을 입력해 주세요.");
            var comment = new PlaylistComment
            {
                UserId = userId,
                PlaylistId = playlistId,
                Content = content,
                CreatedAt = DateTime.Now
            };
            db.PlaylistComments.Add(comment);
            await db.SaveChangesAsync();
            var response = await db.PlaylistComments
            .AsNoTracking()
            .Where(c => c.CommentId == comment.CommentId)
            .Select(CommentResponse.FromEntity)
            .FirstAsync();
            return StatusCode(201, response);
        }
        // DELETE: api/playlists/1/comments/3
        // 댓글 작성자와 플레이리스트 주인만 삭제할 수 있다.
        [Authorize]
        [HttpDelete("{commentId:int}")]
        public async Task<IActionResult> Delete(int playlistId, int commentId)
        {
            int userId = User.GetUserId()!.Value;
            var comment = await db.PlaylistComments
            .Include(c => c.Playlist)
            .FirstOrDefaultAsync(c => c.CommentId == commentId && c.PlaylistId == playlistId);
            if (comment == null)
                return NotFound("댓글을 찾을 수 없습니다.");
            if (comment.UserId != userId && comment.Playlist!.UserId != userId)
                return StatusCode(403, "본인의 댓글이나 내 플레이리스트의 댓글만 삭제할 수 있습니다.");
            db.PlaylistComments.Remove(comment);
            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
