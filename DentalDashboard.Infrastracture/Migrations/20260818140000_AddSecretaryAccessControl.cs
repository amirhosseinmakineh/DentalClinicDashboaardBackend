using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260818140000_AddSecretaryAccessControl")]
public partial class AddSecretaryAccessControl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "SecretaryType", table: "Users", type: "int", nullable: true);
        migrationBuilder.CreateTable(
            name: "SecretaryAccessScheduleAudits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OldDays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                NewDays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            }, constraints: table => table.PrimaryKey("PK_SecretaryAccessScheduleAudits", x => x.Id));
        migrationBuilder.CreateTable(
            name: "SecretaryAccessSchedules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DayOfWeek = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_SecretaryAccessSchedules", x => x.Id);
                table.ForeignKey("FK_SecretaryAccessSchedules_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_SecretaryAccessScheduleAudits_SecretaryUserId", "SecretaryAccessScheduleAudits", "SecretaryUserId");
        migrationBuilder.CreateIndex("IX_SecretaryAccessSchedules_UserId_DayOfWeek", "SecretaryAccessSchedules", new[] { "UserId", "DayOfWeek" }, unique: true);
        migrationBuilder.Sql("UPDATE u SET SecretaryType = 1 FROM Users u INNER JOIN UserRoles ur ON ur.UserId = u.Id INNER JOIN Roles r ON r.Id = ur.RoleId WHERE LOWER(r.RoleName) = 'secretary'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SecretaryAccessScheduleAudits");
        migrationBuilder.DropTable("SecretaryAccessSchedules");
        migrationBuilder.DropColumn("SecretaryType", "Users");
    }
}
