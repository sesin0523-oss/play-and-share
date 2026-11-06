using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyPlaylist.Server.Models
{
    [Table("PLAYLIST_VIEW")]
    public class PlaylistView
    {
        [Key]
        [Column("view_id")]
        public int ViewId { get; set; }
        // 비로그인 조회와 탈퇴한 회원의 조회는 null
        [Column("user_id")]
        public int? UserId { get; set; }
        [Column("playlist_id")]
        public int PlaylistId { get; set; }
        [Column("viewed_at")]
        public DateTime ViewedAt { get; set; }
        public User? User { get; set; }
        public Playlist? Playlist { get; set; }
    }
}
