using Microsoft.EntityFrameworkCore;
using MyPlaylist.Server.Models;
namespace MyPlaylist.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }
        public DbSet<User> Users => Set<User>();
        public DbSet<Playlist> Playlists => Set<Playlist>();
        public DbSet<PlaylistTrack> PlaylistTracks
        => Set<PlaylistTrack>();
        public DbSet<PlaylistLike> PlaylistLikes
        => Set<PlaylistLike>();
        public DbSet<PlaylistView> PlaylistViews
        => Set<PlaylistView>();
        public DbSet<PlaylistComment> PlaylistComments
        => Set<PlaylistComment>();
        public DbSet<PlaylistTag> PlaylistTags
        => Set<PlaylistTag>();
        protected override void OnModelCreating(
        ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 닉네임 중복 방지
            modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();
            // 회원과 플레이리스트 관계
            modelBuilder.Entity<Playlist>()
            .HasOne(p => p.User)
            .WithMany(u => u.Playlists)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            // 플레이리스트와 음악 관계
            modelBuilder.Entity<PlaylistTrack>()
            .HasOne(t => t.Playlist)
            .WithMany(p => p.Tracks)
            .HasForeignKey(t => t.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
            // 회원과 좋아요 관계
            modelBuilder.Entity<PlaylistLike>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            // 플레이리스트와 좋아요 관계
            modelBuilder.Entity<PlaylistLike>()
            .HasOne(l => l.Playlist)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
            // 동일 회원의 동일 플레이리스트 중복 좋아요 방지
            modelBuilder.Entity<PlaylistLike>()
            .HasIndex(l => new
            {
                l.UserId,
                l.PlaylistId
            })
            .IsUnique();
            // 회원과 조회 기록 관계
            // 탈퇴해도 다른 회원 플레이리스트의 조회수가 줄지 않도록 기록은 남긴다.
            modelBuilder.Entity<PlaylistView>()
            .HasOne(v => v.User)
            .WithMany(u => u.Views)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);
            // 플레이리스트와 조회 기록 관계
            modelBuilder.Entity<PlaylistView>()
            .HasOne(v => v.Playlist)
            .WithMany(p => p.Views)
            .HasForeignKey(v => v.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
            // 회원과 댓글 관계
            // 탈퇴해도 다른 사람의 대화 흐름이 끊기지 않도록 댓글은 "탈퇴한 회원"으로 남긴다.
            modelBuilder.Entity<PlaylistComment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);
            // 플레이리스트와 댓글 관계: 플레이리스트를 지우면 댓글도 함께 삭제
            modelBuilder.Entity<PlaylistComment>()
            .HasOne(c => c.Playlist)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
            // 플레이리스트별 댓글을 시간순으로 빠르게 가져오기 위한 인덱스
            modelBuilder.Entity<PlaylistComment>()
            .HasIndex(c => new { c.PlaylistId, c.CreatedAt });
            // 플레이리스트와 태그 관계: 플레이리스트를 지우면 태그도 함께 삭제
            modelBuilder.Entity<PlaylistTag>()
            .HasOne(t => t.Playlist)
            .WithMany(p => p.Tags)
            .HasForeignKey(t => t.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
            // 같은 플레이리스트에 같은 태그 중복 방지
            modelBuilder.Entity<PlaylistTag>()
            .HasIndex(t => new { t.PlaylistId, t.TagName })
            .IsUnique();
            // 태그로 걸러 볼 때 사용
            modelBuilder.Entity<PlaylistTag>()
            .HasIndex(t => t.TagName);
        }
    }
}
