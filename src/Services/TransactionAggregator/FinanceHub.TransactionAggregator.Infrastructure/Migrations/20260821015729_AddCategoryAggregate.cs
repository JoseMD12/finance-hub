using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceHub.TransactionAggregator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "user_category_rules",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "canonical_transactions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IconKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ColorToken = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categories_categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inbox_processed_messages",
                columns: table => new
                {
                    message_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    processed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_processed_messages", x => x.message_hash);
                });

            migrationBuilder.CreateTable(
                name: "user_consolidated_balance_read_model",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    total_checking_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_credit_card_spent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_consolidated_balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    last_calculated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_consolidated_balance_read_model", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_categories_slug",
                table: "categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_ParentCategoryId",
                table: "categories",
                column: "ParentCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "inbox_processed_messages");

            migrationBuilder.DropTable(
                name: "user_consolidated_balance_read_model");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "user_category_rules");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "canonical_transactions");
        }
    }
}
