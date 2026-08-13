using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class DemandTypeSeedConfiguration : IEntityTypeConfiguration<DemandType>
{
    public void Configure(EntityTypeBuilder<DemandType> builder) => builder.HasData(
        Item(1, "monthly_charge", "شارژ ماهانه", 10), Item(2, "utility_contribution", "سهم قبوض", 20),
        Item(3, "special_assessment", "مشارکت ویژه", 30), Item(4, "maintenance_contribution", "مشارکت نگهداری", 40),
        Item(5, "fund_shortage", "جبران کسری صندوق", 50));
    private static object Item(long id, string key, string title, int order) => new { Id = id, Key = key, Title = title, SortOrder = order, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc };
}

internal sealed class PaymentMethodSeedConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder) => builder.HasData(
        Item(1, "online_gateway", "درگاه آنلاین", false, 10), Item(2, "card_to_card", "کارت به کارت", true, 20),
        Item(3, "bank_transfer", "انتقال بانکی", true, 30), Item(4, "cash", "نقدی", true, 40), Item(5, "manual", "ثبت دستی", true, 50));
    private static object Item(long id, string key, string title, bool approval, int order) => new { Id = id, Key = key, Title = title, RequiresManagerApproval = approval, SortOrder = order, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc };
}

internal sealed class ExpenseTypeSeedConfiguration : IEntityTypeConfiguration<ExpenseType>
{
    public void Configure(EntityTypeBuilder<ExpenseType> builder) => builder.HasData(
        Item(1, "water", "آب", 10), Item(2, "electricity", "برق", 20), Item(3, "cleaning", "نظافت", 30),
        Item(4, "caretaker", "حقوق سرایدار", 40), Item(5, "equipment_repair", "تعمیر تجهیزات", 50), Item(6, "insurance", "بیمه", 60));
    private static object Item(long id, string key, string title, int order) => new { Id = id, Key = key, Title = title, ComplexId = (long?)null, BuildingId = (long?)null, SortOrder = order, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc };
}
