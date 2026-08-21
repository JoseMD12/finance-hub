using System;
using System.Collections.Generic;
using FinanceHub.TransactionAggregator.Domain.Entities;

namespace FinanceHub.TransactionAggregator.Infrastructure.Persistence;

public static class CategorySeedData
{
    public static readonly Guid FoodId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid TransportId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid HousingId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid HealthId = Guid.Parse("11111111-1111-1111-1111-111111111104");
    public static readonly Guid LeisureId = Guid.Parse("11111111-1111-1111-1111-111111111105");
    public static readonly Guid ShoppingId = Guid.Parse("11111111-1111-1111-1111-111111111106");
    public static readonly Guid EducationId = Guid.Parse("11111111-1111-1111-1111-111111111107");
    public static readonly Guid FinanceId = Guid.Parse("11111111-1111-1111-1111-111111111108");
    public static readonly Guid IncomeId = Guid.Parse("11111111-1111-1111-1111-111111111109");
    public static readonly Guid OthersId = Guid.Parse("11111111-1111-1111-1111-111111111110");

    private const string ColorEmerald = "emerald";
    private const string ColorSky = "sky";
    private const string ColorAmber = "amber";
    private const string ColorRose = "rose";
    private const string ColorPurple = "purple";
    private const string ColorIndigo = "indigo";
    private const string ColorTeal = "teal";
    private const string ColorBlue = "blue";
    private const string ColorGreen = "green";
    private const string ColorGray = "gray";

    public static List<Category> GetDefaultCategories()
    {
        var list = new List<Category>();

        // 1. Alimentação (food)
        list.Add(Category.Create("Alimentação", "food", "utensils", ColorEmerald, isSystemDefault: true, id: FoodId));
        list.Add(Category.Create("Supermercado", "food-supermarket", "shopping-cart", ColorEmerald, isSystemDefault: true, parentCategoryId: FoodId));
        list.Add(Category.Create("Restaurante", "food-restaurant", "utensils", ColorEmerald, isSystemDefault: true, parentCategoryId: FoodId));
        list.Add(Category.Create("Delivery", "food-delivery", "bike", ColorEmerald, isSystemDefault: true, parentCategoryId: FoodId));
        list.Add(Category.Create("Padaria", "food-bakery", "croissant", ColorEmerald, isSystemDefault: true, parentCategoryId: FoodId));

        // 2. Transporte (transport)
        list.Add(Category.Create("Transporte", "transport", "car", ColorSky, isSystemDefault: true, id: TransportId));
        list.Add(Category.Create("Aplicativos", "transport-rideshare", "car", ColorSky, isSystemDefault: true, parentCategoryId: TransportId));
        list.Add(Category.Create("Combustível", "transport-fuel", "fuel", ColorSky, isSystemDefault: true, parentCategoryId: TransportId));
        list.Add(Category.Create("Passagens", "transport-transit", "ticket", ColorSky, isSystemDefault: true, parentCategoryId: TransportId));
        list.Add(Category.Create("Estacionamento", "transport-parking", "circle-parking", ColorSky, isSystemDefault: true, parentCategoryId: TransportId));
        list.Add(Category.Create("Manutenção", "transport-maintenance", "wrench", ColorSky, isSystemDefault: true, parentCategoryId: TransportId));

        // 3. Moradia (housing)
        list.Add(Category.Create("Moradia", "housing", "home", ColorAmber, isSystemDefault: true, id: HousingId));
        list.Add(Category.Create("Aluguel", "housing-rent", "building", ColorAmber, isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Condomínio", "housing-condo", "building-2", ColorAmber, isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Energia", "housing-electricity", "zap", ColorAmber, isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Água", "housing-water", "droplet", ColorAmber, isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Gás", "housing-gas", "flame", ColorAmber, isSystemDefault: true, parentCategoryId: HousingId));
        list.Add(Category.Create("Internet", "housing-internet", "wifi", ColorAmber, isSystemDefault: true, parentCategoryId: HousingId));

        // 4. Saúde (health)
        list.Add(Category.Create("Saúde", "health", "heart-pulse", ColorRose, isSystemDefault: true, id: HealthId));
        list.Add(Category.Create("Farmácia", "health-pharmacy", "pill", ColorRose, isSystemDefault: true, parentCategoryId: HealthId));
        list.Add(Category.Create("Consultas", "health-doctor", "stethoscope", ColorRose, isSystemDefault: true, parentCategoryId: HealthId));
        list.Add(Category.Create("Plano", "health-insurance", "shield-plus", ColorRose, isSystemDefault: true, parentCategoryId: HealthId));
        list.Add(Category.Create("Academia", "health-gym", "dumbbell", ColorRose, isSystemDefault: true, parentCategoryId: HealthId));

        // 5. Lazer (leisure)
        list.Add(Category.Create("Lazer", "leisure", "tv", ColorPurple, isSystemDefault: true, id: LeisureId));
        list.Add(Category.Create("Streaming", "leisure-streaming", "play", ColorPurple, isSystemDefault: true, parentCategoryId: LeisureId));
        list.Add(Category.Create("Viagens", "leisure-travel", "plane", ColorPurple, isSystemDefault: true, parentCategoryId: LeisureId));
        list.Add(Category.Create("Eventos", "leisure-events", "ticket", ColorPurple, isSystemDefault: true, parentCategoryId: LeisureId));
        list.Add(Category.Create("Hobbies", "leisure-hobbies", "gamepad", ColorPurple, isSystemDefault: true, parentCategoryId: LeisureId));

        // 6. Compras (shopping)
        list.Add(Category.Create("Compras", "shopping", "shopping-bag", ColorIndigo, isSystemDefault: true, id: ShoppingId));
        list.Add(Category.Create("Vestuário", "shopping-clothing", "shirt", ColorIndigo, isSystemDefault: true, parentCategoryId: ShoppingId));
        list.Add(Category.Create("Eletrônicos", "shopping-electronics", "laptop", ColorIndigo, isSystemDefault: true, parentCategoryId: ShoppingId));
        list.Add(Category.Create("Cosméticos", "shopping-cosmetics", "sparkles", ColorIndigo, isSystemDefault: true, parentCategoryId: ShoppingId));
        list.Add(Category.Create("Decoração", "shopping-decor", "lamp", ColorIndigo, isSystemDefault: true, parentCategoryId: ShoppingId));

        // 7. Educação (education)
        list.Add(Category.Create("Educação", "education", "graduation-cap", ColorTeal, isSystemDefault: true, id: EducationId));
        list.Add(Category.Create("Cursos", "education-courses", "video", ColorTeal, isSystemDefault: true, parentCategoryId: EducationId));
        list.Add(Category.Create("Livros", "education-books", "book-open", ColorTeal, isSystemDefault: true, parentCategoryId: EducationId));
        list.Add(Category.Create("Mensalidades", "education-tuition", "school", ColorTeal, isSystemDefault: true, parentCategoryId: EducationId));

        // 8. Finanças (finance)
        list.Add(Category.Create("Finanças", "finance", "landmark", ColorBlue, isSystemDefault: true, id: FinanceId));
        list.Add(Category.Create("Tarifas", "finance-fees", "receipt", ColorBlue, isSystemDefault: true, parentCategoryId: FinanceId));
        list.Add(Category.Create("Impostos", "finance-taxes", "file-text", ColorBlue, isSystemDefault: true, parentCategoryId: FinanceId));
        list.Add(Category.Create("Juros", "finance-interest", "percent", ColorBlue, isSystemDefault: true, parentCategoryId: FinanceId));
        list.Add(Category.Create("Seguros", "finance-insurance", "shield", ColorBlue, isSystemDefault: true, parentCategoryId: FinanceId));

        // 9. Receitas (income)
        list.Add(Category.Create("Receitas", "income", "trending-up", ColorGreen, isSystemDefault: true, id: IncomeId));
        list.Add(Category.Create("Salário", "income-salary", "briefcase", ColorGreen, isSystemDefault: true, parentCategoryId: IncomeId));
        list.Add(Category.Create("Rendimentos", "income-yield", "coins", ColorGreen, isSystemDefault: true, parentCategoryId: IncomeId));
        list.Add(Category.Create("Reembolsos", "income-refund", "rotate-ccw", ColorGreen, isSystemDefault: true, parentCategoryId: IncomeId));
        list.Add(Category.Create("Freelance", "income-freelance", "laptop", ColorGreen, isSystemDefault: true, parentCategoryId: IncomeId));

        // 10. Outros (others)
        list.Add(Category.Create("Outros", "others", "tag", ColorGray, isSystemDefault: true, id: OthersId));
        list.Add(Category.Create("Ajustes", "others-adjustments", "sliders", ColorGray, isSystemDefault: true, parentCategoryId: OthersId));
        list.Add(Category.Create("Transferências", "others-transfers", "arrow-left-right", ColorGray, isSystemDefault: true, parentCategoryId: OthersId));
        list.Add(Category.Create("Diversos", "others-general", "tag", ColorGray, isSystemDefault: true, parentCategoryId: OthersId));

        return list;
    }
}
