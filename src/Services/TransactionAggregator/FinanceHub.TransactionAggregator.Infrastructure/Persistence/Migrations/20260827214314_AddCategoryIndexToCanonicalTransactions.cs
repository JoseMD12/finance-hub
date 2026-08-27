using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryIndexToCanonicalTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O índice vem antes do backfill de propósito: sem ele, o UPDATE abaixo faz varredura
            // sequencial de canonical_transactions. Em base grande, criar o índice manualmente com
            // CREATE INDEX CONCURRENTLY antes de aplicar esta migração evita o lock de escrita.
            migrationBuilder.CreateIndex(
                name: "idx_canonical_transactions_category",
                table: "canonical_transactions",
                column: "CategoryId");

            // Backfill de neutralidade das transações já ingeridas antes de a natureza passar a
            // ser resolvida pela categoria.
            //
            // Vive aqui, e não no startup da API, por dois motivos. Executado a cada boot, o
            // predicado "ainda não é neutra" selecionava exatamente as transações que o usuário
            // tinha acabado de trazer de volta aos totais via ToggleNeutrality, revertendo a
            // decisão dele em silêncio a cada reinício. E, registrado no histórico de migração,
            // roda uma única vez em vez de varrer a tabela inteira a cada deploy.
            migrationBuilder.Sql("""
                UPDATE canonical_transactions
                   SET "is_ignored_in_totals" = true,
                       nature = c.nature
                  FROM categories c
                 WHERE c."Id" = canonical_transactions."CategoryId"
                   AND c.nature <> 0
                   AND canonical_transactions."is_ignored_in_totals" = false;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_canonical_transactions_category",
                table: "canonical_transactions");
        }
    }
}
