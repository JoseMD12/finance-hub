using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditInvoiceDataToTransactionsAndBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "current_installment",
                table: "canonical_transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "invoice_due_date_utc",
                table: "canonical_transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "total_installments",
                table: "canonical_transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "available_credit_limit",
                table: "account_balances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "credit_limit",
                table: "account_balances",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "invoice_closing_date_utc",
                table: "account_balances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "invoice_due_date_utc",
                table: "account_balances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_credit_card",
                table: "account_balances",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_installment",
                table: "canonical_transactions");

            migrationBuilder.DropColumn(
                name: "invoice_due_date_utc",
                table: "canonical_transactions");

            migrationBuilder.DropColumn(
                name: "total_installments",
                table: "canonical_transactions");

            migrationBuilder.DropColumn(
                name: "available_credit_limit",
                table: "account_balances");

            migrationBuilder.DropColumn(
                name: "credit_limit",
                table: "account_balances");

            migrationBuilder.DropColumn(
                name: "invoice_closing_date_utc",
                table: "account_balances");

            migrationBuilder.DropColumn(
                name: "invoice_due_date_utc",
                table: "account_balances");

            migrationBuilder.DropColumn(
                name: "is_credit_card",
                table: "account_balances");
        }
    }
}
