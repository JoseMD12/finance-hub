using FinanceHub.PluggyIntegration.Domain.Constants;

namespace FinanceHub.PluggyIntegration.Domain.ValueObjects;

public sealed record AccountType
{
    public string RawType { get; }
    public string? RawSubtype { get; }
    public bool IsCreditCard { get; }

    public AccountType(string rawType, string? rawSubtype)
    {
        RawType = rawType ?? string.Empty;
        RawSubtype = rawSubtype;
        IsCreditCard = RawType == PluggyConstants.AccountTypes.Credit ||
                       RawSubtype == PluggyConstants.AccountSubtypes.CreditCard;
    }
}
