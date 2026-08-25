using System;
using FinanceHub.TransactionAggregator.Domain.Entities;

namespace FinanceHub.TransactionAggregator.Domain.Services;

public static class TransactionChannelDetector
{
    private static readonly (string Keyword, TransactionChannel Channel)[] ChannelRules =
    [
        ("PIX", TransactionChannel.Pix),
        ("TED", TransactionChannel.Ted),
        ("DOC", TransactionChannel.Doc),
        ("CREDIT", TransactionChannel.CreditCard),
        ("CREDITO", TransactionChannel.CreditCard),
        ("DEBIT", TransactionChannel.DebitCard),
        ("DEBITO", TransactionChannel.DebitCard)
    ];

    public static TransactionChannel ResolveChannel(TransactionChannel providedChannel, string rawDescription)
    {
        if (providedChannel != TransactionChannel.Other)
        {
            return providedChannel;
        }

        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return TransactionChannel.Other;
        }

        var descriptionUpper = rawDescription.ToUpperInvariant();

        foreach (var (keyword, channel) in ChannelRules)
        {
            if (descriptionUpper.Contains(keyword))
            {
                return channel;
            }
        }

        return TransactionChannel.Other;
    }
}
