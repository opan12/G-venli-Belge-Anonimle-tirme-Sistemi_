using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeanonymizedContent",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeanonymizedContent",
                table: "Articles");
        }
    }
}
