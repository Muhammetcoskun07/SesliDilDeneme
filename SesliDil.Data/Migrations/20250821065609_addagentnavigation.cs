using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesliDil.Data.Migrations
{
    /// <inheritdoc />
    public partial class addagentnavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Prompt_AgentId",
                table: "Prompt",
                column: "AgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prompt_AIAgent_AgentId",
                table: "Prompt",
                column: "AgentId",
                principalTable: "AIAgent",
                principalColumn: "AgentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prompt_AIAgent_AgentId",
                table: "Prompt");

            migrationBuilder.DropIndex(
                name: "IX_Prompt_AgentId",
                table: "Prompt");
        }
    }
}
