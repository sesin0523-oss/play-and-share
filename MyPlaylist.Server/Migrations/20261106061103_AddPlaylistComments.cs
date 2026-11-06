using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPlaylist.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PLAYLIST_COMMENT",
                columns: table => new
                {
                    comment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    playlist_id = table.Column<int>(type: "int", nullable: false),
                    content = table.Column<string>(type: "varchar(500)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLAYLIST_COMMENT", x => x.comment_id);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_COMMENT_PLAYLIST_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "PLAYLIST",
                        principalColumn: "playlist_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_COMMENT_USER_user_id",
                        column: x => x.user_id,
                        principalTable: "USER",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_COMMENT_playlist_id_created_at",
                table: "PLAYLIST_COMMENT",
                columns: new[] { "playlist_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_COMMENT_user_id",
                table: "PLAYLIST_COMMENT",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PLAYLIST_COMMENT");
        }
    }
}
