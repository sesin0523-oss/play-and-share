using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPlaylist.Server.Migrations
{
    /// <inheritdoc />
    public partial class KeepViewsOnUserDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PLAYLIST_VIEW_USER_user_id",
                table: "PLAYLIST_VIEW");

            migrationBuilder.AddForeignKey(
                name: "FK_PLAYLIST_VIEW_USER_user_id",
                table: "PLAYLIST_VIEW",
                column: "user_id",
                principalTable: "USER",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PLAYLIST_VIEW_USER_user_id",
                table: "PLAYLIST_VIEW");

            migrationBuilder.AddForeignKey(
                name: "FK_PLAYLIST_VIEW_USER_user_id",
                table: "PLAYLIST_VIEW",
                column: "user_id",
                principalTable: "USER",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
