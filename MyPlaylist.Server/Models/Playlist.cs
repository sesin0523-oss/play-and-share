using System.ComponentModel.DataAnnotations.Schema;
namespace MyPlaylist.Server.Models
{
    [Table("PLAYLIST")]
    public class Playlist
    {
        [Column("playlist_id")]
        public int PlaylistId { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("playlist_title", TypeName = "varchar(100)")]
        public string PlaylistTitle { get; set; } = "";
        [Column("description", TypeName = "varchar(500)")]
        public string? Description { get; set; }
        [Column("is_public")]
        public bool IsPublic { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        // 플레이리스트와 회원, 음악, 좋아요, 조회 기록, 댓글의 관계
        public User? User { get; set; }
        public List<PlaylistTrack> Tracks { get; set; } = new();
        public List<PlaylistLike> Likes { get; set; } = new();
        public List<PlaylistView> Views { get; set; } = new();
        public List<PlaylistComment> Comments { get; set; } = new();
        public List<PlaylistTag> Tags { get; set; } = new();
    }
}
