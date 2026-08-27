using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNatureToCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "nature",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // O seed de categorias só roda em base vazia, então bases já existentes ficariam com
            // todas as categorias como Operating — e transferências e investimentos voltariam a
            // contar como gasto de vida. Estes UPDATEs corrigem o catálogo já semeado.
            // Valores do enum TransactionNature: Transfer = 1, Investment = 3, Adjustment = 4.
            migrationBuilder.Sql("""
                UPDATE categories SET nature = 1
                 WHERE "Id" = '11111111-1111-1111-1111-111111111002';
            """);

            migrationBuilder.Sql("""
                UPDATE categories SET nature = 3
                 WHERE "Id" = '11111111-1111-1111-1111-111111110805';
            """);

            migrationBuilder.Sql("""
                UPDATE categories SET nature = 4
                 WHERE "Id" = '11111111-1111-1111-1111-111111111001';
            """);

            // Correção de dado: resgate de cofrinho estava catalogado como Receitas > Rendimentos,
            // o que contava dinheiro voltando de aplicação como receita nova e inflava a renda.
            // Passa a apontar para Finanças > Investimentos, que é neutro por natureza.
            migrationBuilder.Sql("""
                UPDATE canonical_transactions
                   SET "CategoryId" = '11111111-1111-1111-1111-111111110805'
                 WHERE "CategoryId" = '11111111-1111-1111-1111-111111110902'
                   AND "is_ignored_in_totals" = true;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "nature",
                table: "categories");
        }
    }
}
