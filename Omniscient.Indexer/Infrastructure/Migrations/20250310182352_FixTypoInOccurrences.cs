using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omniscient.Indexer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTypoInOccurrences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Occurences_Emails_EmailId",
                table: "Occurences");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurences_Words_WordValue",
                table: "Occurences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Occurences",
                table: "Occurences");

            migrationBuilder.RenameTable(
                name: "Occurences",
                newName: "Occurrences");

            migrationBuilder.RenameIndex(
                name: "IX_Occurences_EmailId",
                table: "Occurrences",
                newName: "IX_Occurrences_EmailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Occurrences",
                table: "Occurrences",
                columns: new[] { "WordValue", "EmailId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Occurrences_Emails_EmailId",
                table: "Occurrences",
                column: "EmailId",
                principalTable: "Emails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Occurrences_Words_WordValue",
                table: "Occurrences",
                column: "WordValue",
                principalTable: "Words",
                principalColumn: "Value",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Occurrences_Emails_EmailId",
                table: "Occurrences");

            migrationBuilder.DropForeignKey(
                name: "FK_Occurrences_Words_WordValue",
                table: "Occurrences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Occurrences",
                table: "Occurrences");

            migrationBuilder.RenameTable(
                name: "Occurrences",
                newName: "Occurences");

            migrationBuilder.RenameIndex(
                name: "IX_Occurrences_EmailId",
                table: "Occurences",
                newName: "IX_Occurences_EmailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Occurences",
                table: "Occurences",
                columns: new[] { "WordValue", "EmailId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Occurences_Emails_EmailId",
                table: "Occurences",
                column: "EmailId",
                principalTable: "Emails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Occurences_Words_WordValue",
                table: "Occurences",
                column: "WordValue",
                principalTable: "Words",
                principalColumn: "Value",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
