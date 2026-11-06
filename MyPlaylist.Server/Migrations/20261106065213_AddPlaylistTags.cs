using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPlaylist.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PLAYLIST_TAG",
                columns: table => new
                {
                    tag_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    playlist_id = table.Column<int>(type: "int", nullable: false),
                    tag_name = table.Column<string>(type: "varchar(20)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLAYLIST_TAG", x => x.tag_id);
                    table.ForeignKey(
                        name: "FK_PLAYLIST_TAG_PLAYLIST_playlist_id",
                        column: x => x.playlist_id,
                        principalTable: "PLAYLIST",
                        principalColumn: "playlist_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_TAG_playlist_id_tag_name",
                table: "PLAYLIST_TAG",
                columns: new[] { "playlist_id", "tag_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PLAYLIST_TAG_tag_name",
                table: "PLAYLIST_TAG",
                column: "tag_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PLAYLIST_TAG");
        }
    }
}
