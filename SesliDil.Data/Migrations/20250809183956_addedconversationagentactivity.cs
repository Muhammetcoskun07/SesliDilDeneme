using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SesliDil.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedconversationagentactivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationAgentActivity",
                columns: table => new
                {
                    ActivityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConversationId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AgentId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationAgentActivity", x => x.ActivityId);
                    table.ForeignKey(
                        name: "FK_ConversationAgentActivity_AIAgent_AgentId",
                        column: x => x.AgentId,
                        principalTable: "AIAgent",
                        principalColumn: "AgentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationAgentActivity_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversation",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationAgentActivity_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAgentActivity_AgentId",
                table: "ConversationAgentActivity",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAgentActivity_ConversationId",
                table: "ConversationAgentActivity",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAgentActivity_UserId",
                table: "ConversationAgentActivity",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationAgentActivity");
        }
    }
}
