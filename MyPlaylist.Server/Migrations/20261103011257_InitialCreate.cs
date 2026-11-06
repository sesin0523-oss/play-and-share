using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPlaylist.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "USER",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_name = table.Column<string>(type: "varchar(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_pw = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER", x => x.user_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PLAYLIST",
                columns: table => new
                {
                    playlist_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    playlist_title = table.Column<string>(type: "varchar(100)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cover_image = table.Column<string>(type: "varchar(500)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_public = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    view_count = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLAYLIST", x => x.playlist_id);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_USER_user_id",
                        column: x => x.user_id,
                        principalTable: "USER",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PLAYLIST_LIKE",
                columns: table => new
                {
                    like_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    playlist_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLAYLIST_LIKE", x => x.like_id);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_LIKE_PLAYLIST_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "PLAYLIST",
                        principalColumn: "playlist_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_LIKE_USER_user_id",
                        column: x => x.user_id,
                        principalTable: "USER",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PLAYLIST_TRACK",
                columns: table => new
                {
                    track_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    playlist_id = table.Column<int>(type: "int", nullable: false),
                    youtube_video_id = table.Column<string>(type: "varchar(30)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    music_title = table.Column<string>(type: "varchar(200)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    artist = table.Column<string>(type: "varchar(200)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    thumbnail_url = table.Column<string>(type: "varchar(500)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    youtube_url = table.Column<string>(type: "varchar(500)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    added_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLAYLIST_TRACK", x => x.track_id);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_TRACK_PLAYLIST_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "PLAYLIST",
                        principalColumn: "playlist_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_user_id",
                table: "PLAYLIST",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_LIKE_playlist_id",
                table: "PLAYLIST_LIKE",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_LIKE_user_id_playlist_id",
                table: "PLAYLIST_LIKE",
                columns: new[] { "user_id", "playlist_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_TRACK_playlist_id",
                table: "PLAYLIST_TRACK",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_USER_user_name",
                table: "USER",
                column: "user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PLAYLIST_LIKE");

            migrationBuilder.DropTable(
                name: "PLAYLIST_TRACK");

            migrationBuilder.DropTable(
                name: "PLAYLIST");

            migrationBuilder.DropTable(
                name: "USER");
        }
    }
}
