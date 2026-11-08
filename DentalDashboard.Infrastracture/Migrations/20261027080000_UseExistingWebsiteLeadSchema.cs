using Microsoft.EntityFrameworkCore.Migrations;

namespace DentalDashboard.Infrastracture.Migrations;

// Records the final EF model without changing the physical lead table.
public partial class UseExistingWebsiteLeadSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No column creation or deletion, regardless of the old migration state.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // A rollback must not recreate the removed EF dependency or delete data.
    }
}
