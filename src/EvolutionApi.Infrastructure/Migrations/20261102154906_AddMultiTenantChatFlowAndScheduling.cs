using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvolutionApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantChatFlowAndScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "OutboundMessage",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Message",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Instance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Contact",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Chat",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatFlowSession",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    RemoteJid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CurrentNodeId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VariablesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastInteractionAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatFlowSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatFlowSession_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledMessageJob",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    HangfireJobId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledMessageJob", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledMessageJob_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatFlowSession_InstanceId_RemoteJid",
                table: "ChatFlowSession",
                columns: new[] { "InstanceId", "RemoteJid" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMessageJob_InstanceId",
                table: "ScheduledMessageJob",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMessageJob_Status_ScheduledAtUtc",
                table: "ScheduledMessageJob",
                columns: new[] { "Status", "ScheduledAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatFlowSession");

            migrationBuilder.DropTable(
                name: "ScheduledMessageJob");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OutboundMessage");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Message");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Instance");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Contact");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Chat");
        }
    }
}
