using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Migrations
{
    /// <inheritdoc />
    public partial class g : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EncryptedAesKey",
                table: "Articles",
                newName: "Content");

            migrationBuilder.AddColumn<string>(
                name: "AnonymizedContent",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnonymizedContent",
                table: "Articles");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "Articles",
                newName: "EncryptedAesKey");
        }
    }
}
