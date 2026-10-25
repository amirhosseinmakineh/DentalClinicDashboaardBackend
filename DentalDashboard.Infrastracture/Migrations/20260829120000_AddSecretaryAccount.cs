using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DentalDashboard.Infrastracture.Context;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260829120000_AddSecretaryAccount")]
public partial class AddSecretaryAccount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExpenseCategories",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FinancialTransactions",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Type = table.Column<int>(type: "int", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CounterpartyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                PaymentMethod = table.Column<int>(type: "int", nullable: false),
                TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                ReceiptUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                ExpenseCategoryId = table.Column<long>(type: "bigint", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                table.ForeignKey("FK_FinancialTransactions_ExpenseCategories_ExpenseCategoryId", x => x.ExpenseCategoryId, "ExpenseCategories", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_FinancialTransactions_Users_CreatedByUserId", x => x.CreatedByUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_ExpenseCategories_Title", "ExpenseCategories", "Title", unique: true);
        migrationBuilder.CreateIndex("IX_FinancialTransactions_CreatedByUserId", "FinancialTransactions", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_FinancialTransactions_ExpenseCategoryId", "FinancialTransactions", "ExpenseCategoryId");
        migrationBuilder.CreateIndex("IX_FinancialTransactions_TransactionDate", "FinancialTransactions", "TransactionDate");
        migrationBuilder.CreateIndex("IX_FinancialTransactions_Type_ExpenseCategoryId", "FinancialTransactions", new[] { "Type", "ExpenseCategoryId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FinancialTransactions");
        migrationBuilder.DropTable(name: "ExpenseCategories");
    }
}
