using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260819120000_AddSecretaryPermissionsAndIdentitySchedule")]
public partial class AddSecretaryPermissionsAndIdentitySchedule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // SQL Server cannot ALTER a Guid key into an identity int. Rebuild the table in one
        // transaction and copy every schedule row so existing access configuration is retained.
        migrationBuilder.DropForeignKey("FK_SecretaryAccessSchedules_Users_UserId", "SecretaryAccessSchedules");
        migrationBuilder.RenameTable("SecretaryAccessSchedules", newName: "SecretaryAccessSchedules_Legacy");
        migrationBuilder.CreateTable(
            name: "SecretaryAccessSchedules",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DayOfWeek = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecretaryAccessSchedules", x => x.Id);
                table.ForeignKey("FK_SecretaryAccessSchedules_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.Sql("INSERT INTO SecretaryAccessSchedules (UserId, DayOfWeek, IsActive, CreatedAt, UpdatedAt, IsDeleted, DeletedAt) SELECT UserId, DayOfWeek, IsActive, CreatedAt, UpdatedAt, IsDeleted, DeletedAt FROM SecretaryAccessSchedules_Legacy");
        migrationBuilder.DropTable("SecretaryAccessSchedules_Legacy");
        migrationBuilder.CreateIndex("IX_SecretaryAccessSchedules_UserId_DayOfWeek", "SecretaryAccessSchedules", new[] { "UserId", "DayOfWeek" }, unique: true);

        migrationBuilder.CreateTable(
            name: "SecretaryAccessPermissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DayOfWeek = table.Column<int>(type: "int", nullable: false),
                PermissionType = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecretaryAccessPermissions", x => x.Id);
                table.ForeignKey("FK_SecretaryAccessPermissions_Users_SecretaryUserId", x => x.SecretaryUserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_SecretaryAccessPermissions_SecretaryUserId_DayOfWeek_PermissionType", "SecretaryAccessPermissions", new[] { "SecretaryUserId", "DayOfWeek", "PermissionType" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SecretaryAccessPermissions");
        migrationBuilder.DropTable("SecretaryAccessSchedules");
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
        migrationBuilder.CreateIndex("IX_SecretaryAccessSchedules_UserId_DayOfWeek", "SecretaryAccessSchedules", new[] { "UserId", "DayOfWeek" }, unique: true);
    }
}
