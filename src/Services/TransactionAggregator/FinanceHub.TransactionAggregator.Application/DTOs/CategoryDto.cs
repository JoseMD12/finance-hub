using System;
using System.Collections.Generic;

namespace FinanceHub.TransactionAggregator.Application.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentCategoryId,
    string IconKey,
    string ColorToken,
    bool IsSystemDefault,
    bool IsActive,
    List<CategoryDto>? Subcategories = null);
