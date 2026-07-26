using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificatesAndCompletionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlServer = ActiveProvider.Contains("SqlServer", StringComparison.Ordinal);
            var integerType = isSqlServer ? "int" : "INTEGER";
            var booleanType = isSqlServer ? "bit" : "INTEGER";
            var dateTimeType = isSqlServer ? "datetime2" : "TEXT";

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_LearnerId_TrainingId",
                table: "Enrollments");

            migrationBuilder.AddColumn<bool>(
                name: "CertificateEnabled",
                table: "Trainings",
                type: booleanType,
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateTemplateName",
                table: "Trainings",
                type: isSqlServer ? "nvarchar(100)" : "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CertificateValidityMonths",
                table: "Trainings",
                type: integerType,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumAverageScore",
                table: "Trainings",
                type: isSqlServer ? "decimal(5,2)" : "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireAllLessonsCompleted",
                table: "Trainings",
                type: booleanType,
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireAllMandatoryAssessmentsPassed",
                table: "Trainings",
                type: booleanType,
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatory",
                table: "Assessments",
                type: booleanType,
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: integerType, nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: integerType, nullable: false),
                    CertificateNumber = table.Column<string>(type: isSqlServer ? "nvarchar(100)" : "TEXT", maxLength: 100, nullable: false),
                    VerificationCode = table.Column<string>(type: isSqlServer ? "nvarchar(100)" : "TEXT", maxLength: 100, nullable: false),
                    LearnerFullNameSnapshot = table.Column<string>(type: isSqlServer ? "nvarchar(250)" : "TEXT", maxLength: 250, nullable: false),
                    TrainingTitleSnapshot = table.Column<string>(type: isSqlServer ? "nvarchar(250)" : "TEXT", maxLength: 250, nullable: false),
                    TrainerFullNameSnapshot = table.Column<string>(type: isSqlServer ? "nvarchar(250)" : "TEXT", maxLength: 250, nullable: true),
                    CompletionDate = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    Status = table.Column<int>(type: integerType, nullable: false),
                    RevokedAt = table.Column<DateTime>(type: dateTimeType, nullable: true),
                    RevokedByAdminId = table.Column<string>(type: isSqlServer ? "nvarchar(450)" : "TEXT", nullable: true),
                    RevocationReason = table.Column<string>(type: isSqlServer ? "nvarchar(1000)" : "TEXT", maxLength: 1000, nullable: true),
                    PdfFileName = table.Column<string>(type: isSqlServer ? "nvarchar(255)" : "TEXT", maxLength: 255, nullable: true),
                    PdfRelativePath = table.Column<string>(type: isSqlServer ? "nvarchar(500)" : "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: dateTimeType, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: dateTimeType, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_AspNetUsers_RevokedByAdminId",
                        column: x => x.RevokedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Training_CertificateValidityMonths",
                table: "Trainings",
                sql: "CertificateValidityMonths IS NULL OR CertificateValidityMonths > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Training_MinimumAverageScore",
                table: "Trainings",
                sql: "MinimumAverageScore IS NULL OR (MinimumAverageScore >= 0 AND MinimumAverageScore <= 100)");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LearnerId_TrainingId",
                table: "Enrollments",
                columns: new[] { "LearnerId", "TrainingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificateNumber",
                table: "Certificates",
                column: "CertificateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_EnrollmentId",
                table: "Certificates",
                column: "EnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_RevokedByAdminId",
                table: "Certificates",
                column: "RevokedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Status",
                table: "Certificates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_VerificationCode",
                table: "Certificates",
                column: "VerificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Training_CertificateValidityMonths",
                table: "Trainings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Training_MinimumAverageScore",
                table: "Trainings");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_LearnerId_TrainingId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "CertificateEnabled",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "CertificateTemplateName",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "CertificateValidityMonths",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "MinimumAverageScore",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "RequireAllLessonsCompleted",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "RequireAllMandatoryAssessmentsPassed",
                table: "Trainings");

            migrationBuilder.DropColumn(
                name: "IsMandatory",
                table: "Assessments");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_LearnerId_TrainingId",
                table: "Enrollments",
                columns: new[] { "LearnerId", "TrainingId" });
        }
    }
}
