using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvolutionApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalKeyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Instance",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Integration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConnectionStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OwnerJid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BusinessId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProfileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePicUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboundMessage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDelivery",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    HeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDelivery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chat",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UnreadCount = table.Column<int>(type: "int", nullable: false),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    LastMessageJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chat", x => new { x.Id, x.InstanceId });
                    table.ForeignKey(
                        name: "FK_Chat_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatwootSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameInbox = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoCreate = table.Column<bool>(type: "bit", nullable: false),
                    ImportContacts = table.Column<bool>(type: "bit", nullable: false),
                    ImportMessages = table.Column<bool>(type: "bit", nullable: false),
                    DaysLimitImportMessages = table.Column<int>(type: "int", nullable: false),
                    SignMsg = table.Column<bool>(type: "bit", nullable: false),
                    ReOpenConversation = table.Column<bool>(type: "bit", nullable: false),
                    ConversationPending = table.Column<bool>(type: "bit", nullable: false),
                    MergeBrazilContacts = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatwootSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatwootSetting_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    PushName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    VerifiedName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProfilePicUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsWhatsApp = table.Column<bool>(type: "bit", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => new { x.Id, x.InstanceId });
                    table.ForeignKey(
                        name: "FK_Contact_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    RemoteJid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FromMe = table.Column<bool>(type: "bit", nullable: false),
                    Participant = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PushName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MessageJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => new { x.Id, x.InstanceId });
                    table.ForeignKey(
                        name: "FK_Message_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NatsSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Uri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NatsSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NatsSetting_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenAiSession",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    RemoteJid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ThreadId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenAiSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenAiSession_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenAiSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    SpeechToText = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenAiSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenAiSetting_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Proxy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Host = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Port = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordEncrypted = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proxy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proxy_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RabbitMqSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Uri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GlobalEvents = table.Column<bool>(type: "bit", nullable: false),
                    EventsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RabbitMqSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RabbitMqSetting_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionAuth",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    SessionKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EncryptedJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAuth", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAuth_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Setting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    RejectCall = table.Column<bool>(type: "bit", nullable: false),
                    MsgCall = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GroupsIgnore = table.Column<bool>(type: "bit", nullable: false),
                    AlwaysOnline = table.Column<bool>(type: "bit", nullable: false),
                    ReadMessages = table.Column<bool>(type: "bit", nullable: false),
                    ReadStatus = table.Column<bool>(type: "bit", nullable: false),
                    SyncFullHistory = table.Column<bool>(type: "bit", nullable: false),
                    WavoipToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Setting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Setting_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignalKey",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    KeyId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    KeyData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignalKey_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SqsSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessKeyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretAccessKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SqsSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SqsSetting_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TypebotSession",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    RemoteJid = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypebotSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypebotSession_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TypebotSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TypebotId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Expire = table.Column<int>(type: "int", nullable: false),
                    KeywordFinish = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DelayMessage = table.Column<int>(type: "int", nullable: false),
                    UnknownMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ListeningFromMe = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypebotSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypebotSetting_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Webhook",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    EventsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ByEvents = table.Column<bool>(type: "bit", nullable: false),
                    Base64 = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhook", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Webhook_Instance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "Instance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chat_InstanceId",
                table: "Chat",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatwootSetting_InstanceId",
                table: "ChatwootSetting",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contact_InstanceId",
                table: "Contact",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Instance_Name",
                table: "Instance",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Message_InstanceId_RemoteJid_Timestamp",
                table: "Message",
                columns: new[] { "InstanceId", "RemoteJid", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_NatsSetting_InstanceId",
                table: "NatsSetting",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpenAiSession_InstanceId_RemoteJid",
                table: "OpenAiSession",
                columns: new[] { "InstanceId", "RemoteJid" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenAiSetting_InstanceId",
                table: "OpenAiSetting",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundMessage_Status_NextAttemptAt",
                table: "OutboundMessage",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Proxy_InstanceId",
                table: "Proxy",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RabbitMqSetting_InstanceId",
                table: "RabbitMqSetting",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionAuth_InstanceId_SessionKey",
                table: "SessionAuth",
                columns: new[] { "InstanceId", "SessionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Setting_InstanceId",
                table: "Setting",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalKey_InstanceId_Category_KeyId",
                table: "SignalKey",
                columns: new[] { "InstanceId", "Category", "KeyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SqsSetting_InstanceId",
                table: "SqsSetting",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypebotSession_InstanceId_RemoteJid",
                table: "TypebotSession",
                columns: new[] { "InstanceId", "RemoteJid" });

            migrationBuilder.CreateIndex(
                name: "IX_TypebotSetting_InstanceId",
                table: "TypebotSetting",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Webhook_InstanceId",
                table: "Webhook",
                column: "InstanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDelivery_Status_NextAttemptAt",
                table: "WebhookDelivery",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chat");

            migrationBuilder.DropTable(
                name: "ChatwootSetting");

            migrationBuilder.DropTable(
                name: "Contact");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "NatsSetting");

            migrationBuilder.DropTable(
                name: "OpenAiSession");

            migrationBuilder.DropTable(
                name: "OpenAiSetting");

            migrationBuilder.DropTable(
                name: "OutboundMessage");

            migrationBuilder.DropTable(
                name: "Proxy");

            migrationBuilder.DropTable(
                name: "RabbitMqSetting");

            migrationBuilder.DropTable(
                name: "SessionAuth");

            migrationBuilder.DropTable(
                name: "Setting");

            migrationBuilder.DropTable(
                name: "SignalKey");

            migrationBuilder.DropTable(
                name: "SqsSetting");

            migrationBuilder.DropTable(
                name: "TypebotSession");

            migrationBuilder.DropTable(
                name: "TypebotSetting");

            migrationBuilder.DropTable(
                name: "Webhook");

            migrationBuilder.DropTable(
                name: "WebhookDelivery");

            migrationBuilder.DropTable(
                name: "Instance");
        }
    }
}
