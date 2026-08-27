using System;

namespace FinanceHub.TransactionAggregator.Domain.Constants;

/// <summary>
/// Identificadores das categorias de sistema semeadas em <c>categories.default.json</c>.
/// Existem para eliminar <c>Guid.Parse</c> inline espalhado pelo código (Regra 10 — zero magic
/// strings e zero magic numbers).
///
/// Estes são identificadores de <b>catálogo genérico</b>, válidos para qualquer usuário. Nada
/// aqui pode depender dos dados de um usuário específico.
/// </summary>
public static class SystemCategoryIds
{
    public static readonly Guid Transferencias = new("11111111-1111-1111-1111-111111111002");
    public static readonly Guid Ajustes = new("11111111-1111-1111-1111-111111111001");
    public static readonly Guid Investimentos = new("11111111-1111-1111-1111-111111110805");
    public static readonly Guid Tarifas = new("11111111-1111-1111-1111-111111110801");
    public static readonly Guid Salario = new("11111111-1111-1111-1111-111111110901");
    public static readonly Guid Rendimentos = new("11111111-1111-1111-1111-111111110902");
}
