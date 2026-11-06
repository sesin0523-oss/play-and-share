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
    [Route("api/playlists/{playlistId:int}/tracks")]
    public class PlaylistTracksController : ControllerBase
    {
        private readonly AppDbContext db;
        public PlaylistTracksController(AppDbContext context)
        {
            db = context;
        }
        // GET: api/playlists/1/tracks
        [HttpGet]
        public async Task<IActionResult> GetAll(int playlistId)
        {
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            if (!playlist.IsPublic && playlist.UserId != User.GetUserId())
                return StatusCode(403, "비공개 플레이리스트입니다.");
            var list = await db.PlaylistTracks
            .AsNoTracking()
            .Where(t => t.PlaylistId == playlistId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.TrackId)
            .ToListAsync();
            return Ok(list);
        }
        // POST: api/playlists/1/tracks
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(
        int playlistId, TrackRequest request)
        {
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            // 소유자 확인
            if (playlist.UserId != User.GetUserId())
                return StatusCode(403, "본인의 플레이리스트만 수정할 수 있습니다.");
            // 맨 뒤에 추가
            int lastOrder = await db.PlaylistTracks
            .Where(t => t.PlaylistId == playlistId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;
            var track = new PlaylistTrack { PlaylistId = playlistId };
            Apply(track, request);
            track.SortOrder = lastOrder + 1;
            track.AddedAt = DateTime.Now;
            db.PlaylistTracks.Add(track);
            playlist.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();
            return Created(
            $"/api/playlists/{playlistId}/tracks/{track.TrackId}",
            track);
        }
        // PUT: api/playlists/1/tracks/3
        [Authorize]
        [HttpPut("{trackId:int}")]
        public async Task<IActionResult> Update(
        int playlistId, int trackId, TrackRequest request)
        {
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            // 소유자 확인
            if (playlist.UserId != User.GetUserId())
                return StatusCode(403, "본인의 플레이리스트만 수정할 수 있습니다.");
            var track = await db.PlaylistTracks.FirstOrDefaultAsync(
            t => t.TrackId == trackId && t.PlaylistId == playlistId);
            if (track == null)
                return NotFound("음악을 찾을 수 없습니다.");
            Apply(track, request);
            playlist.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();
            return NoContent();
        }
        // DELETE: api/playlists/1/tracks/3
        [Authorize]
        [HttpDelete("{trackId:int}")]
        public async Task<IActionResult> Delete(int playlistId, int trackId)
        {
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            // 소유자 확인
            if (playlist.UserId != User.GetUserId())
                return StatusCode(403, "본인의 플레이리스트만 수정할 수 있습니다.");
            var track = await db.PlaylistTracks.FirstOrDefaultAsync(
            t => t.TrackId == trackId && t.PlaylistId == playlistId);
            if (track == null)
                return NotFound("음악을 찾을 수 없습니다.");
            db.PlaylistTracks.Remove(track);
            playlist.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();
            return NoContent();
        }
        // PATCH: api/playlists/1/tracks/order
        [Authorize]
        [HttpPatch("order")]
        public async Task<IActionResult> ChangeOrder(
        int playlistId, TrackOrderRequest request)
        {
            var playlist = await db.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("플레이리스트를 찾을 수 없습니다.");
            // 소유자 확인
            if (playlist.UserId != User.GetUserId())
                return StatusCode(403, "본인의 플레이리스트만 수정할 수 있습니다.");
            var tracks = await db.PlaylistTracks
            .Where(t => t.PlaylistId == playlistId)
            .ToListAsync();
            // 플레이리스트의 모든 음악이 한 번씩 포함되어야 한다.
            bool sameSet = request.TrackIds.Count == tracks.Count
            && request.TrackIds.ToHashSet()
            .SetEquals(tracks.Select(t => t.TrackId));
            if (!sameSet)
                return BadRequest("플레이리스트의 모든 음악을 한 번씩 전달해야 합니다.");
            for (int i = 0; i < request.TrackIds.Count; i++)
            {
                tracks.First(t => t.TrackId == request.TrackIds[i])
                .SortOrder = i + 1;
            }
            playlist.UpdatedAt = DateTime.Now;
            await db.SaveChangesAsync();
            return NoContent();
        }
        private static void Apply(PlaylistTrack track, TrackRequest request)
        {
            track.YoutubeVideoId = request.YoutubeVideoId;
            track.MusicTitle = request.MusicTitle.Trim();
            track.Artist = request.Artist;
            // 클라이언트가 이 주소를 그대로 열기 때문에 다른 주소가 저장되지 않도록 영상 ID로 만든다.
            track.YoutubeUrl =
            $"https://www.youtube.com/watch?v={request.YoutubeVideoId}";
        }
    }
}
