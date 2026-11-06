using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using MyPlaylist.Server.Models;
namespace MyPlaylist.Server.Dtos
{
    // 플레이리스트 생성 / 수정 요청
    public class PlaylistRequest
    {
        [Required, StringLength(100)]
        public string PlaylistTitle { get; set; } = "";
        [StringLength(500)]
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        // 태그 최대 3개 (추천 태그 또는 사용자가 만든 태그). null이면 수정할 때 태그를 바꾸지 않는다.
        public List<string>? Tags { get; set; }
    }
    // 플레이리스트 목록 응답
    public class PlaylistSummary
    {
        public int PlaylistId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string PlaylistTitle { get; set; } = "";
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int TrackCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        // 목록 API와 통계 API에서 함께 사용하는 조회 형식
        public static readonly Expression<Func<Playlist, PlaylistSummary>>
        FromEntity = p => new PlaylistSummary
        {
            PlaylistId = p.PlaylistId,
            UserId = p.UserId,
            UserName = p.User!.UserName,
            PlaylistTitle = p.PlaylistTitle,
            Description = p.Description,
            IsPublic = p.IsPublic,
            ViewCount = p.Views.Count,
            LikeCount = p.Likes.Count,
            TrackCount = p.Tracks.Count,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Tags = p.Tags.OrderBy(t => t.TagId).Select(t => t.TagName).ToList()
        };
        // 기간별 순위용: since 이후에 쌓인 클릭 수와 좋아요 수만 센다.
        public static Expression<Func<Playlist, PlaylistSummary>> FromEntitySince(DateTime since)
        {
            return p => new PlaylistSummary
            {
                PlaylistId = p.PlaylistId,
                UserId = p.UserId,
                UserName = p.User!.UserName,
                PlaylistTitle = p.PlaylistTitle,
                Description = p.Description,
                IsPublic = p.IsPublic,
                ViewCount = p.Views.Count(v => v.ViewedAt >= since),
                LikeCount = p.Likes.Count(l => l.CreatedAt >= since),
                TrackCount = p.Tracks.Count,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Tags = p.Tags.OrderBy(t => t.TagId).Select(t => t.TagName).ToList()
            };
        }
    }
    // 음악 추가 / 수정 요청
    // youtube_url과 sort_order는 서버에서 정하므로 받지 않는다.
    public class TrackRequest
    {
        [Required, RegularExpression("^[A-Za-z0-9_-]{1,30}$")]
        public string YoutubeVideoId { get; set; } = "";
        [Required, StringLength(200)]
        public string MusicTitle { get; set; } = "";
        [StringLength(200)]
        public string? Artist { get; set; }
    }
    // YouTube 검색 결과 응답
    // 썸네일은 화면 표시용이며 DB에는 저장하지 않는다.
    public class YouTubeSearchResult
    {
        public string YoutubeVideoId { get; set; } = "";
        public string MusicTitle { get; set; } = "";
        public string Artist { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public string YoutubeUrl { get; set; } = "";
    }
    // "취향을 더 넓혀보세요" 추천 항목
    public class DiscoverItem
    {
        public PlaylistSummary Playlist { get; set; } = new();
        // 이 사용자가 열어본 플레이리스트에는 한 번도 없던 태그 (비로그인이면 빈 목록)
        public List<string> NewTags { get; set; } = new();
    }
    // 댓글 작성 요청
    public class CommentRequest
    {
        [Required, StringLength(500, MinimumLength = 1)]
        public string Content { get; set; } = "";
    }
    // 댓글 응답
    public class CommentResponse
    {
        public int CommentId { get; set; }
        public int PlaylistId { get; set; }
        // 탈퇴한 회원이면 null
        public int? UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public static readonly Expression<Func<PlaylistComment, CommentResponse>>
        FromEntity = c => new CommentResponse
        {
            CommentId = c.CommentId,
            PlaylistId = c.PlaylistId,
            UserId = c.UserId,
            UserName = c.User != null ? c.User.UserName : "탈퇴한 회원",
            Content = c.Content,
            CreatedAt = c.CreatedAt
        };
    }
    // 음악 순서 변경 요청 (바뀐 순서대로 전체 track_id를 전달)
    public class TrackOrderRequest
    {
        [Required, MinLength(1)]
        public List<int> TrackIds { get; set; } = new();
    }
}
