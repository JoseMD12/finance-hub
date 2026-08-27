using System;
using FinanceHub.TransactionAggregator.Domain.Exceptions;

namespace FinanceHub.TransactionAggregator.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public string IconKey { get; private set; }
    public string ColorToken { get; private set; }
    public bool IsSystemDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Natureza econômica das transações desta categoria — ortogonal ao propósito, conforme
    /// `.agents/specs/transit-transfers-and-neutrality-engine-spec.md` §2.1.
    ///
    /// É o que torna a neutralidade genérica: em vez de casar texto de descrição no handler de
    /// ingestão, a categoria resolvida pelo pipeline já diz se aquilo é gasto de vida
    /// (<c>Operating</c>) ou apenas dinheiro mudando de lugar (<c>Transfer</c>,
    /// <c>Investment</c>, <c>Adjustment</c>).
    /// </summary>
    public TransactionNature Nature { get; private set; }

    // EF Navigation property
    public Category? ParentCategory { get; private set; }
    public ICollection<Category> Subcategories { get; private set; } = new List<Category>();

    private Category()
    {
        Name = string.Empty;
        Slug = string.Empty;
        IconKey = string.Empty;
        ColorToken = string.Empty;
        Nature = TransactionNature.Operating;
    }

    private Category(
        Guid id,
        string name,
        string slug,
        Guid? parentCategoryId,
        string iconKey,
        string colorToken,
        bool isSystemDefault,
        TransactionNature nature)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidCategoryNameDomainException();
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new InvalidCategorySlugDomainException();
        }

        Id = id;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ParentCategoryId = parentCategoryId;
        IconKey = string.IsNullOrWhiteSpace(iconKey) ? "tag" : iconKey.Trim();
        ColorToken = string.IsNullOrWhiteSpace(colorToken) ? "gray" : colorToken.Trim();
        IsSystemDefault = isSystemDefault;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        Nature = nature;
    }

    public static Category Create(
        string name,
        string slug,
        string iconKey,
        string colorToken,
        bool isSystemDefault = false,
        Guid? parentCategoryId = null,
        Guid? id = null,
        TransactionNature nature = TransactionNature.Operating)
    {
        return new Category(
            id ?? Guid.NewGuid(),
            name,
            slug,
            parentCategoryId,
            iconKey,
            colorToken,
            isSystemDefault,
            nature);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void UpdateDetails(string name, string iconKey, string colorToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidCategoryNameDomainException();
        }

        Name = name.Trim();
        IconKey = string.IsNullOrWhiteSpace(iconKey) ? "tag" : iconKey.Trim();
        ColorToken = string.IsNullOrWhiteSpace(colorToken) ? "gray" : colorToken.Trim();
    }
}
