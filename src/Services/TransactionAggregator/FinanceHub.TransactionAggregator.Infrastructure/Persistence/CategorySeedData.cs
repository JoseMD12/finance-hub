using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FinanceHub.TransactionAggregator.Domain.Entities;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence;

public static class CategorySeedData
{
    private sealed record CategoryJsonModel(
        Guid Id,
        string Name,
        string Slug,
        string? Icon,
        string? Color,
        bool IsSystemDefault,
        Guid? ParentCategoryId,
        List<CategoryJsonModel>? Subcategories,
        string? Nature = null);

    /// <summary>
    /// Converte a natureza declarada no dataset. Ausência significa <c>Operating</c>: a esmagadora
    /// maioria das categorias é gasto ou receita de vida, e só as exceções se declaram.
    /// </summary>
    private static TransactionNature ParseNature(string? raw) =>
        Enum.TryParse<TransactionNature>(raw, ignoreCase: true, out var parsed)
            ? parsed
            : TransactionNature.Operating;

    public static List<Category> GetDefaultCategories()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("categories.default.json", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            return [];
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return [];
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rootCategories = JsonSerializer.Deserialize<List<CategoryJsonModel>>(json, options);

        if (rootCategories == null || rootCategories.Count == 0)
        {
            return [];
        }

        var result = new List<Category>();

        foreach (var parent in rootCategories)
        {
            var parentEntity = Category.Create(
                parent.Name,
                parent.Slug,
                parent.Icon ?? "tag",
                parent.Color ?? "gray",
                parent.IsSystemDefault,
                null,
                parent.Id,
                ParseNature(parent.Nature));

            result.Add(parentEntity);

            if (parent.Subcategories != null)
            {
                foreach (var sub in parent.Subcategories)
                {
                    var subEntity = Category.Create(
                        sub.Name,
                        sub.Slug,
                        sub.Icon ?? parent.Icon ?? "tag",
                        sub.Color ?? parent.Color ?? "gray",
                        sub.IsSystemDefault || parent.IsSystemDefault,
                        parent.Id,
                        sub.Id,
                        // Subcategoria sem natureza declarada herda a do pai.
                        ParseNature(sub.Nature ?? parent.Nature));

                    result.Add(subEntity);
                }
            }
        }

        return result;
    }
}
