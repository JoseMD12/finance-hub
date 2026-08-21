using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceHub.TransactionAggregator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalDescriptionColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "original_description",
                table: "canonical_transactions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "original_description",
                table: "canonical_transactions");
        }
    }
}
