using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddCallInitiatedAtToLeadAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NULL
                    ALTER TABLE LeadAssignments ADD CallInitiatedAt datetime2 NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NOT NULL
                    ALTER TABLE LeadAssignments DROP COLUMN CallInitiatedAt;
                """);
        }
    }
}
