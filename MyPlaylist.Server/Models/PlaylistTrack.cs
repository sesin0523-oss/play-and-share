using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace MyPlaylist.Server.Models
{
    [Table("PLAYLIST_TRACK")]
    public class PlaylistTrack
    {
        [Key]
        [Column("track_id")]
        public int TrackId { get; set; }
        [Column("playlist_id")]
        public int PlaylistId { get; set; }
        [Column("youtube_video_id", TypeName = "varchar(30)")]
        public string YoutubeVideoId { get; set; } = "";
        [Column("music_title", TypeName = "varchar(200)")]
        public string MusicTitle { get; set; } = "";
        [Column("artist", TypeName = "varchar(200)")]
        public string? Artist { get; set; }
        [Column("youtube_url", TypeName = "varchar(500)")]
        public string YoutubeUrl { get; set; } = "";
        [Column("sort_order")]
        public int SortOrder { get; set; }
        [Column("added_at")]
        public DateTime AddedAt { get; set; }
        // DB에 저장하지 않고 영상 ID로 만든다.
        [NotMapped]
        public string ThumbnailUrl =>
        $"https://i.ytimg.com/vi/{YoutubeVideoId}/mqdefault.jpg";
        // 응답 JSON에는 포함하지 않는다.
        [JsonIgnore]
        public Playlist? Playlist { get; set; }
    }
}
