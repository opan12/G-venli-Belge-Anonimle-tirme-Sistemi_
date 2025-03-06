using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Migrations
{
    /// <inheritdoc />
    public partial class ghjk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "EncryptedAesKey",
                table: "Articles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedAesKey",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
