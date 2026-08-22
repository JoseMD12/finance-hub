using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTransactionAggregatorSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_ignored_in_totals",
                table: "canonical_transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "nature",
                table: "canonical_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "paired_transaction_id",
                table: "canonical_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_canonical_transactions_operating_totals",
                table: "canonical_transactions",
                columns: new[] { "UserId", "TransactionDateUtc", "Type" },
                filter: "\"is_ignored_in_totals\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_canonical_transactions_operating_totals",
                table: "canonical_transactions");

            migrationBuilder.DropColumn(
                name: "is_ignored_in_totals",
                table: "canonical_transactions");

            migrationBuilder.DropColumn(
                name: "nature",
                table: "canonical_transactions");

            migrationBuilder.DropColumn(
                name: "paired_transaction_id",
                table: "canonical_transactions");
        }
    }
}
