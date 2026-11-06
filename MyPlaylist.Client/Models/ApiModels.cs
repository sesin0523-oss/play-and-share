using System.Text.Json.Serialization;
using System.Windows;
namespace MyPlaylist.Client.Models
{
    // 회원 정보 응답
    public class UserInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
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
        // 분위기·장르 태그 (최대 3개)
        public List<string> Tags { get; set; } = new();
        // 아래는 화면 표시용 값이다.
        public string Info => $"{TrackCount}곡 · 좋아요 {LikeCount} · 클릭 {ViewCount}";
        public Visibility TagsVisibility =>
        Tags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public string PublicText => IsPublic ? "공개" : "비공개";
        [JsonIgnore]
        public string? ThumbnailUrl { get; set; }
        [JsonIgnore]
        public int Rank { get; set; }
        public Visibility RankVisibility =>
        Rank > 0 ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore]
        public bool ShowOwner { get; set; } = true;
        public Visibility OwnerVisibility =>
        ShowOwner ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore]
        public bool ShowPublic { get; set; }
        public Visibility PublicVisibility =>
        ShowPublic ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore]
        public bool ShowHeart { get; set; }
        public Visibility HeartVisibility =>
        ShowHeart ? Visibility.Visible : Visibility.Collapsed;
    }
    // "취향을 더 넓혀보세요" 추천 항목
    public class DiscoverItem
    {
        public PlaylistSummary Playlist { get; set; } = new();
        // 내가 열어본 플레이리스트에는 없던 태그
        public List<string> NewTags { get; set; } = new();
        // 카드의 태그 칩: 처음 만나는 태그는 골드로 표시한다.
        public List<TagView> TagViews => Playlist.Tags
        .Select(t => new TagView(t, NewTags.Contains(t))).ToList();
        public Visibility TagsVisibility => Playlist.TagsVisibility;
    }
    public record TagView(string Name, bool IsNew)
    {
        public string Text => IsNew ? $"NEW #{Name}" : $"#{Name}";
    }
    // 플레이리스트 검색 응답
    public class PlaylistSearchResult
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Offset { get; set; }
        public int TotalCount { get; set; }
        public List<PlaylistSummary> Items { get; set; } = new();
    }
    // 플레이리스트 상세 응답
    public class PlaylistDetail
    {
        public PlaylistSummary Playlist { get; set; } = new();
        public bool LikedByMe { get; set; }
        public List<PlaylistTrack> Tracks { get; set; } = new();
    }
    // 곡 응답
    public class PlaylistTrack
    {
        public int TrackId { get; set; }
        public int PlaylistId { get; set; }
        public string YoutubeVideoId { get; set; } = "";
        public string MusicTitle { get; set; } = "";
        public string? Artist { get; set; }
        public string YoutubeUrl { get; set; } = "";
        public int SortOrder { get; set; }
        public string ThumbnailUrl { get; set; } = "";
        // 삭제로 sort_order가 비어 있어도 화면에는 1번부터 표시한다.
        [JsonIgnore]
        public int DisplayOrder { get; set; }
        [JsonIgnore]
        public Visibility OwnerActionsVisibility { get; set; } = Visibility.Collapsed;
        // 앱 안의 플레이어에서 재생 중인 곡이면 골드로 표시한다.
        [JsonIgnore]
        public bool NowPlaying { get; set; }
    }
    // 댓글 응답
    public class PlaylistComment
    {
        public int CommentId { get; set; }
        public int PlaylistId { get; set; }
        // 탈퇴한 회원이면 null
        public int? UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string TimeText => CreatedAt.ToString("yyyy.MM.dd HH:mm");
        // 작성자 본인 또는 플레이리스트 주인에게만 삭제 버튼을 보여 준다.
        [JsonIgnore]
        public Visibility DeleteVisibility { get; set; } = Visibility.Collapsed;
    }
    // YouTube 검색 결과 응답
    public class YouTubeResult
    {
        public string YoutubeVideoId { get; set; } = "";
        public string MusicTitle { get; set; } = "";
        public string Artist { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public string YoutubeUrl { get; set; } = "";
    }
    // 내 활동 통계 응답
    public class MyStatistics
    {
        public int PlaylistCount { get; set; }
        public int TrackCount { get; set; }
        public int GivenLikeCount { get; set; }
        public int ReceivedLikeCount { get; set; }
        public int TotalViewCount { get; set; }
    }
    // 메인 화면의 전체 통계
    public class TotalStatistics
    {
        public int UserCount { get; set; }
        public int PlaylistCount { get; set; }
        public int TotalViewCount { get; set; }
        public int TotalLikeCount { get; set; }
    }
}
