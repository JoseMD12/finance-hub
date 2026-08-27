using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesColumnToCanonicalTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "canonical_transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notes",
                table: "canonical_transactions");
        }
    }
}
