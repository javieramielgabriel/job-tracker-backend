using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusAndPhaseIndexesAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InterviewPhases_JobApplicationId",
                schema: "JobTracker",
                table: "InterviewPhases");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "JobTracker",
                table: "JobApplications",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "JobTracker",
                table: "InterviewPhases",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_Status_CreatedAtUtc",
                schema: "JobTracker",
                table: "JobApplications",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPhases_JobApplicationId_ScheduledAtUtc",
                schema: "JobTracker",
                table: "InterviewPhases",
                columns: new[] { "JobApplicationId", "ScheduledAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobApplications_Status_CreatedAtUtc",
                schema: "JobTracker",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_InterviewPhases_JobApplicationId_ScheduledAtUtc",
                schema: "JobTracker",
                table: "InterviewPhases");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "JobTracker",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "JobTracker",
                table: "InterviewPhases");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPhases_JobApplicationId",
                schema: "JobTracker",
                table: "InterviewPhases",
                column: "JobApplicationId");
        }
    }
}
