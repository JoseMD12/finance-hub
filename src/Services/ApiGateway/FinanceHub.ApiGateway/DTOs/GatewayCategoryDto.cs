namespace FinanceHub.ApiGateway.DTOs;

public record GatewayCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentCategoryId,
    string IconKey,
    string ColorToken,
    bool IsSystemDefault,
    bool IsActive,
    List<GatewayCategoryDto>? Subcategories = null);
