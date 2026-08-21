using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FinanceHub.TransactionAggregator.Application.Interfaces;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence.Datasets;

public class MerchantDatasetProvider : IMerchantDatasetProvider
{
    private readonly List<MerchantDefinition> _merchants = [];

    public MerchantDatasetProvider()
    {
        LoadMerchants();
    }

    private void LoadMerchants()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("merchants.brazil.json", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return;
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loaded = JsonSerializer.Deserialize<List<MerchantDefinition>>(json, options);

        if (loaded != null)
        {
            _merchants.AddRange(loaded);
        }
    }

    public MerchantDefinition? Match(string cleanText)
    {
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            return null;
        }

        var normalizedInput = cleanText.ToUpperInvariant().Trim();

        foreach (var merchant in _merchants)
        {
            foreach (var pattern in merchant.Patterns)
            {
                var normalizedPattern = pattern.ToUpperInvariant().Trim();
                if (normalizedInput.Contains(normalizedPattern))
                {
                    return merchant;
                }
            }
        }

        return null;
    }

    public IReadOnlyList<MerchantDefinition> GetAllMerchants()
    {
        return _merchants.AsReadOnly();
    }
}
