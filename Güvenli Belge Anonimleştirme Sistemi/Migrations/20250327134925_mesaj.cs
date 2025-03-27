using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Migrations
{
    /// <inheritdoc />
    public partial class mesaj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_messages_Articles_ArticleId",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "IX_messages_ArticleId",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "messages");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverEmail",
                table: "messages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropColumn(
                name: "ReceiverEmail",
                table: "messages");

            migrationBuilder.AddColumn<int>(
                name: "ArticleId",
                table: "messages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_messages_ArticleId",
                table: "messages",
                column: "ArticleId");

            migrationBuilder.AddForeignKey(
                name: "FK_messages_Articles_ArticleId",
                table: "messages",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
