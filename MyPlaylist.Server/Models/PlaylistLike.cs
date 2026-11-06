using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyPlaylist.Server.Models
{
    [Table("PLAYLIST_LIKE")]
    public class PlaylistLike
    {
        [Key]
        [Column("like_id")]
        public int LikeId { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("playlist_id")]
        public int PlaylistId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        public User? User { get; set; }
        public Playlist? Playlist { get; set; }
    }
}
