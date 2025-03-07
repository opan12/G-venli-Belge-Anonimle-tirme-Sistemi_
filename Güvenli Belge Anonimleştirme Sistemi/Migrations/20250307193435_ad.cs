using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Migrations
{
    /// <inheritdoc />
    public partial class ad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_Reviewer_ReviewerId",
                table: "reviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reviewer",
                table: "Reviewer");

            migrationBuilder.RenameTable(
                name: "Reviewer",
                newName: "Reviewers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reviewers",
                table: "Reviewers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_Reviewers_ReviewerId",
                table: "reviews",
                column: "ReviewerId",
                principalTable: "Reviewers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_Reviewers_ReviewerId",
                table: "reviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reviewers",
                table: "Reviewers");

            migrationBuilder.RenameTable(
                name: "Reviewers",
                newName: "Reviewer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reviewer",
                table: "Reviewer",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_Reviewer_ReviewerId",
                table: "reviews",
                column: "ReviewerId",
                principalTable: "Reviewer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
