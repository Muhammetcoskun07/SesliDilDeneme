using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesliDil.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatemessage2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TranslatedContent",
                table: "Message",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "GrammarErrors",
                table: "Message",
                type: "text[]",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TranslatedContent",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "GrammarErrors",
                table: "Message");
        }

    }
}
