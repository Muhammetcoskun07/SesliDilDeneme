using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesliDil.Data.Migrations
{
    /// <inheritdoc />
    public partial class addpromptmessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PromptMessage",
                table: "Prompt",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromptMessage",
                table: "Prompt");
        }
    }
}
