using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTrainerIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = ActiveProvider.Contains("SqlServer", StringComparison.Ordinal);
            var integerType = isSqlServer ? "int" : "INTEGER";
            var bigIntegerType = isSqlServer ? "bigint" : "INTEGER";
            var booleanType = isSqlServer ? "bit" : "INTEGER";
            var dateTimeType = isSqlServer ? "datetime2" : "TEXT";
            var guidType = isSqlServer ? "uniqueidentifier" : "TEXT";
            var textType = isSqlServer ? "nvarchar(max)" : "TEXT";

            migrationBuilder.CreateTable(
                name: "AiTrainerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: integerType, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true).Annotation("SqlServer:Identity", "1, 1"),
                    TrainingId = table.Column<int>(type: integerType, nullable: false),
                    DisplayName = table.Column<string>(type: isSqlServer ? "nvarchar(150)" : "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: isSqlServer ? "nvarchar(1000)" : "TEXT", maxLength: 1000, nullable: true),
                    Provider = table.Column<string>(type: isSqlServer ? "nvarchar(100)" : "TEXT", maxLength: 100, nullable: false),
                    AvatarId = table.Column<string>(type: isSqlServer ? "nvarchar(250)" : "TEXT", maxLength: 250, nullable: true),
                    VoiceId = table.Column<string>(type: isSqlServer ? "nvarchar(250)" : "TEXT", maxLength: 250, nullable: true),
                    LanguageCode = table.Column<string>(type: isSqlServer ? "nvarchar(20)" : "TEXT", maxLength: 20, nullable: false),
                    SystemPrompt = table.Column<string>(type: isSqlServer ? "nvarchar(max)" : "TEXT", maxLength: 10000, nullable: true),
                    WelcomeMessage = table.Column<string>(type: isSqlServer ? "nvarchar(2000)" : "TEXT", maxLength: 2000, nullable: true),
                    FallbackMessage = table.Column<string>(type: isSqlServer ? "nvarchar(1000)" : "TEXT", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: booleanType, nullable: false),
                    AllowTextInput = table.Column<bool>(type: booleanType, nullable: false),
                    AllowAudioInput = table.Column<bool>(type: booleanType, nullable: false),
                    AllowAudioOutput = table.Column<bool>(type: booleanType, nullable: false),
                    AllowAvatar = table.Column<bool>(type: booleanType, nullable: false),
                    MaximumMessagesPerSession = table.Column<int>(type: integerType, nullable: false),
                    MaximumSessionMinutes = table.Column<int>(type: integerType, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: dateTimeType, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTrainerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiTrainerProfiles_Trainings_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiUserConsents",
                columns: table => new
                {
                    Id = table.Column<int>(type: integerType, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true).Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: isSqlServer ? "nvarchar(450)" : "TEXT", nullable: false),
                    ConsentType = table.Column<string>(type: isSqlServer ? "nvarchar(100)" : "TEXT", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: isSqlServer ? "nvarchar(100)" : "TEXT", maxLength: 100, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    RevokedAt = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    PolicyVersion = table.Column<string>(type: isSqlServer ? "nvarchar(50)" : "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: dateTimeType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUserConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiUserConsents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiConversationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: guidType, nullable: false),
                    AiTrainerProfileId = table.Column<int>(type: integerType, nullable: false),
                    EnrollmentId = table.Column<int>(type: integerType, nullable: true),
                    LessonId = table.Column<int>(type: integerType, nullable: false),
                    UserId = table.Column<string>(type: isSqlServer ? "nvarchar(450)" : "TEXT", nullable: false),
                    ProviderSessionId = table.Column<string>(type: isSqlServer ? "nvarchar(500)" : "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: integerType, nullable: false),
                    StartedAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    EndedAt = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    MessageCount = table.Column<int>(type: integerType, nullable: false),
                    InputAudioSeconds = table.Column<int>(type: integerType, nullable: false),
                    OutputAudioSeconds = table.Column<int>(type: integerType, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: isSqlServer ? "decimal(18,6)" : "TEXT", precision: 18, scale: 6, nullable: true),
                    FailureReason = table.Column<string>(type: isSqlServer ? "nvarchar(2000)" : "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiConversationSessions_AiTrainerProfiles_AiTrainerProfileId",
                        column: x => x.AiTrainerProfileId,
                        principalTable: "AiTrainerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiConversationSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiConversationSessions_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiConversationSessions_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiConversationMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: bigIntegerType, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true).Annotation("SqlServer:Identity", "1, 1"),
                    AiConversationSessionId = table.Column<Guid>(type: guidType, nullable: false),
                    Role = table.Column<int>(type: integerType, nullable: false),
                    TextContent = table.Column<string>(type: textType, nullable: false),
                    TranscriptionText = table.Column<string>(type: textType, nullable: true),
                    AudioStoragePath = table.Column<string>(type: isSqlServer ? "nvarchar(500)" : "TEXT", maxLength: 500, nullable: true),
                    ProviderMessageId = table.Column<string>(type: isSqlServer ? "nvarchar(500)" : "TEXT", maxLength: 500, nullable: true),
                    SequenceNumber = table.Column<int>(type: integerType, nullable: false),
                    TokenCount = table.Column<int>(type: integerType, nullable: true),
                    AudioDurationSeconds = table.Column<int>(type: integerType, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    IsModerated = table.Column<bool>(type: booleanType, nullable: false),
                    ModerationReason = table.Column<string>(type: isSqlServer ? "nvarchar(1000)" : "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConversationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiConversationMessages_AiConversationSessions_AiConversationSessionId",
                        column: x => x.AiConversationSessionId,
                        principalTable: "AiConversationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiProviderUsageRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: bigIntegerType, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true).Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<Guid>(type: guidType, nullable: false),
                    Provider = table.Column<string>(type: isSqlServer ? "nvarchar(100)" : "TEXT", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: isSqlServer ? "nvarchar(100)" : "TEXT", maxLength: 100, nullable: false),
                    InputUnits = table.Column<int>(type: integerType, nullable: true),
                    OutputUnits = table.Column<int>(type: integerType, nullable: true),
                    AudioSeconds = table.Column<int>(type: integerType, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: isSqlServer ? "decimal(18,6)" : "TEXT", precision: 18, scale: 6, nullable: true),
                    Succeeded = table.Column<bool>(type: booleanType, nullable: false),
                    ErrorCode = table.Column<string>(type: isSqlServer ? "nvarchar(200)" : "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: dateTimeType, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProviderUsageRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiProviderUsageRecords_AiConversationSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AiConversationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationMessages_AiConversationSessionId_SequenceNumber",
                table: "AiConversationMessages",
                columns: new[] { "AiConversationSessionId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationSessions_AiTrainerProfileId",
                table: "AiConversationSessions",
                column: "AiTrainerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationSessions_EnrollmentId",
                table: "AiConversationSessions",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationSessions_LessonId",
                table: "AiConversationSessions",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationSessions_Status",
                table: "AiConversationSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationSessions_UserId_LessonId",
                table: "AiConversationSessions",
                columns: new[] { "UserId", "LessonId" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AiConversationSessions_UserId_StartedAt",
                table: "AiConversationSessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderUsageRecords_Provider_CreatedAt",
                table: "AiProviderUsageRecords",
                columns: new[] { "Provider", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderUsageRecords_SessionId",
                table: "AiProviderUsageRecords",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTrainerProfiles_TrainingId",
                table: "AiTrainerProfiles",
                column: "TrainingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiUserConsents_UserId_ConsentType_Provider_PolicyVersion",
                table: "AiUserConsents",
                columns: new[] { "UserId", "ConsentType", "Provider", "PolicyVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiConversationMessages");

            migrationBuilder.DropTable(
                name: "AiProviderUsageRecords");

            migrationBuilder.DropTable(
                name: "AiUserConsents");

            migrationBuilder.DropTable(
                name: "AiConversationSessions");

            migrationBuilder.DropTable(
                name: "AiTrainerProfiles");
        }
    }
}
