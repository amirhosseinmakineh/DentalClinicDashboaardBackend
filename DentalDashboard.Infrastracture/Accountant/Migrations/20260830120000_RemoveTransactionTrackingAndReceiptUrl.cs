using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Accountant.Migrations;

public partial class RemoveTransactionTrackingAndReceiptUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReceiptUrl",
            table: "FinancialTransactions");

        migrationBuilder.DropColumn(
            name: "TrackingNumber",
            table: "FinancialTransactions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ReceiptUrl",
            table: "FinancialTransactions",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TrackingNumber",
            table: "FinancialTransactions",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);
    }
}
