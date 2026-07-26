using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainingManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNonPortableTrainingCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Training_CertificateValidityMonths",
                table: "Trainings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Training_MinimumAverageScore",
                table: "Trainings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Training_CertificateValidityMonths",
                table: "Trainings",
                sql: "CertificateValidityMonths IS NULL OR CertificateValidityMonths > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Training_MinimumAverageScore",
                table: "Trainings",
                sql: "MinimumAverageScore IS NULL OR (MinimumAverageScore >= 0 AND MinimumAverageScore <= 100)");
        }
    }
}
