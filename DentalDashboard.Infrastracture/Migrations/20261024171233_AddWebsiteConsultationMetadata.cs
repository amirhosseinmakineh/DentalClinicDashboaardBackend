using Microsoft.EntityFrameworkCore.Migrations;

namespace DentalDashboard.Infrastracture.Migrations;

// Retain this migration ID because some installations may already have applied it.
// Retired: Website intake uses only the existing lead schema. Do not create these
// optional columns on pending installations, or delete data on applied ones.
public partial class AddWebsiteConsultationMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally no schema changes. Existing columns, if any, are untouched.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Never drop previously populated columns during rollback.
    }
}
