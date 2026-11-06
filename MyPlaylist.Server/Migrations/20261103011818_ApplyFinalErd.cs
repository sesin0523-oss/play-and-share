using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPlaylist.Server.Migrations
{
    /// <inheritdoc />
    public partial class ApplyFinalErd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                table: "PLAYLIST_TRACK");

            migrationBuilder.DropColumn(
                name: "cover_image",
                table: "PLAYLIST");

            migrationBuilder.DropColumn(
                name: "view_count",
                table: "PLAYLIST");

            migrationBuilder.CreateTable(
                name: "PLAYLIST_VIEW",
                columns: table => new
                {
                    view_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    playlist_id = table.Column<int>(type: "int", nullable: false),
                    viewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLAYLIST_VIEW", x => x.view_id);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_VIEW_PLAYLIST_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "PLAYLIST",
                        principalColumn: "playlist_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_VIEW_USER_user_id",
                        column: x => x.user_id,
                        principalTable: "USER",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_VIEW_playlist_id",
                table: "PLAYLIST_VIEW",
                column: "playlist_id");

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_VIEW_user_id",
                table: "PLAYLIST_VIEW",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PLAYLIST_VIEW");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "PLAYLIST_TRACK",
                type: "varchar(500)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "cover_image",
                table: "PLAYLIST",
                type: "varchar(500)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "view_count",
                table: "PLAYLIST",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
