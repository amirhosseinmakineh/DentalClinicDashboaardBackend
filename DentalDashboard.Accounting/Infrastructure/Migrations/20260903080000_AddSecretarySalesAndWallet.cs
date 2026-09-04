using System;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260903080000_AddSecretarySalesAndWallet")]
public partial class AddSecretarySalesAndWallet : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SecretarySaleServices",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                SecretaryReward = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_SecretarySaleServices", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SecretarySales",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ServiceId = table.Column<long>(type: "bigint", nullable: false),
                SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                SecretaryReward = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                ReviewedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecretarySales", x => x.Id);
                table.CheckConstraint("CK_SecretarySales_SalePrice", "[SalePrice] > 0");
                table.CheckConstraint("CK_SecretarySales_SecretaryReward", "[SecretaryReward] > 0");
                table.ForeignKey("FK_SecretarySales_SecretarySaleServices_ServiceId", x => x.ServiceId, "SecretarySaleServices", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SecretarySales_Users_PatientUserId", x => x.PatientUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SecretarySales_Users_ReviewedByAdminId", x => x.ReviewedByAdminId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SecretarySales_Users_SecretaryUserId", x => x.SecretaryUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SecretaryWallets",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecretaryWallets", x => x.Id);
                table.CheckConstraint("CK_SecretaryWallets_Balance", "[Balance] >= 0");
                table.ForeignKey("FK_SecretaryWallets_Users_SecretaryUserId", x => x.SecretaryUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SecretaryWalletTransactions",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                WalletId = table.Column<long>(type: "bigint", nullable: false),
                SecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SecretarySaleId = table.Column<long>(type: "bigint", nullable: true),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TransactionType = table.Column<int>(type: "int", nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecretaryWalletTransactions", x => x.Id);
                table.CheckConstraint("CK_SecretaryWalletTransactions_Amount", "[Amount] <> 0");
                table.ForeignKey("FK_SecretaryWalletTransactions_SecretarySales_SecretarySaleId", x => x.SecretarySaleId, "SecretarySales", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SecretaryWalletTransactions_SecretaryWallets_WalletId", x => x.WalletId, "SecretaryWallets", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SecretaryWalletTransactions_Users_SecretaryUserId", x => x.SecretaryUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_SecretarySaleServices_Title", "SecretarySaleServices", "Title", unique: true, filter: "[IsDeleted] = 0");
        migrationBuilder.CreateIndex("IX_SecretarySales_PatientUserId", "SecretarySales", "PatientUserId");
        migrationBuilder.CreateIndex("IX_SecretarySales_ReviewedByAdminId", "SecretarySales", "ReviewedByAdminId");
        migrationBuilder.CreateIndex("IX_SecretarySales_SecretaryUserId_CreatedAt", "SecretarySales", new[] { "SecretaryUserId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_SecretarySales_ServiceId", "SecretarySales", "ServiceId");
        migrationBuilder.CreateIndex("IX_SecretarySales_Status_CreatedAt", "SecretarySales", new[] { "Status", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_SecretaryWallets_SecretaryUserId", "SecretaryWallets", "SecretaryUserId", unique: true, filter: "[IsDeleted] = 0");
        migrationBuilder.CreateIndex("IX_SecretaryWalletTransactions_SecretarySaleId_TransactionType", "SecretaryWalletTransactions", new[] { "SecretarySaleId", "TransactionType" }, unique: true, filter: "[SecretarySaleId] IS NOT NULL AND [TransactionType] = 1");
        migrationBuilder.CreateIndex("IX_SecretaryWalletTransactions_SecretaryUserId_CreatedAt", "SecretaryWalletTransactions", new[] { "SecretaryUserId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_SecretaryWalletTransactions_WalletId", "SecretaryWalletTransactions", "WalletId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SecretaryWalletTransactions");
        migrationBuilder.DropTable("SecretarySales");
        migrationBuilder.DropTable("SecretaryWallets");
        migrationBuilder.DropTable("SecretarySaleServices");
    }
}
