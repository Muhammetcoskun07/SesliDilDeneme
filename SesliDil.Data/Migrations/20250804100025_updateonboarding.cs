using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesliDil.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateonboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JsonDocument>(
                name: "ImprovementGoals",
                table: "User",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<JsonDocument>(
                name: "TopicInterests",
                table: "User",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeeklySpeakingGoal",
                table: "User",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AudioUrl",
                table: "Message",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImprovementGoals",
                table: "User");

            migrationBuilder.DropColumn(
                name: "TopicInterests",
                table: "User");

            migrationBuilder.DropColumn(
                name: "WeeklySpeakingGoal",
                table: "User");

            migrationBuilder.AlterColumn<string>(
                name: "AudioUrl",
                table: "Message",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
