using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260818120000_AddFinancialTransactionCore")]
public partial class AddFinancialTransactionCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FinancialTransactions",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                TransactionType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ReferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                table.ForeignKey("FK_FinancialTransactions_Users_CreatedByUserId", x => x.CreatedByUserId,
                    "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Wallets",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Wallets", x => x.Id);
                table.ForeignKey("FK_Wallets_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "WalletTransactions",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                WalletId = table.Column<long>(type: "bigint", nullable: false),
                FinancialTransactionId = table.Column<long>(type: "bigint", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                table.ForeignKey("FK_WalletTransactions_FinancialTransactions_FinancialTransactionId",
                    x => x.FinancialTransactionId, "FinancialTransactions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WalletTransactions_Wallets_WalletId", x => x.WalletId,
                    "Wallets", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_FinancialTransactions_CreatedAt", "FinancialTransactions", "CreatedAt");
        migrationBuilder.CreateIndex("IX_FinancialTransactions_CreatedByUserId", "FinancialTransactions", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_FinancialTransactions_ReferenceType_ReferenceId", "FinancialTransactions", new[] { "ReferenceType", "ReferenceId" });
        migrationBuilder.CreateIndex("IX_Wallets_UserId", "Wallets", "UserId", unique: true);
        migrationBuilder.CreateIndex("IX_WalletTransactions_FinancialTransactionId", "WalletTransactions", "FinancialTransactionId");
        migrationBuilder.CreateIndex("IX_WalletTransactions_WalletId_CreatedAt", "WalletTransactions", new[] { "WalletId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("WalletTransactions");
        migrationBuilder.DropTable("FinancialTransactions");
        migrationBuilder.DropTable("Wallets");
    }
}
