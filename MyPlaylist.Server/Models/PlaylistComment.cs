using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyPlaylist.Server.Models
{
    [Table("PLAYLIST_COMMENT")]
    public class PlaylistComment
    {
        [Key]
        [Column("comment_id")]
        public int CommentId { get; set; }
        // 탈퇴한 회원의 댓글은 null (댓글은 "탈퇴한 회원"으로 남는다)
        [Column("user_id")]
        public int? UserId { get; set; }
        [Column("playlist_id")]
        public int PlaylistId { get; set; }
        [Column("content", TypeName = "varchar(500)")]
        public string Content { get; set; } = "";
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        public User? User { get; set; }
        public Playlist? Playlist { get; set; }
    }
}
