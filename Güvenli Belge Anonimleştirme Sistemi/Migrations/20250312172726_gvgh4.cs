using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Migrations
{
    /// <inheritdoc />
    public partial class gvgh4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "reviews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArticleId",
                table: "reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
