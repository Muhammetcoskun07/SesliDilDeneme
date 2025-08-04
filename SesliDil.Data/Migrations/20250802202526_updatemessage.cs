using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesliDil.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatemessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrammarErrors",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "TranslatedContent",
                table: "Message");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "GrammarErrors",
                table: "Message",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "TranslatedContent",
                table: "Message",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
