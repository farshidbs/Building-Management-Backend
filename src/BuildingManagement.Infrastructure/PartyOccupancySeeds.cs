using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class PartyTypeSeedConfiguration : IEntityTypeConfiguration<PartyType>
{
    public void Configure(EntityTypeBuilder<PartyType> builder) =>
        builder.HasData(
            Item(1, PartyReferenceKeys.PartyTypes.IranianPerson, "شخص حقیقی ایرانی", 10),
            Item(2, PartyReferenceKeys.PartyTypes.IranianOrganization, "شخص حقوقی ایرانی", 20),
            Item(3, PartyReferenceKeys.PartyTypes.ForeignPerson, "شخص حقیقی خارجی", 30),
            Item(4, PartyReferenceKeys.PartyTypes.ForeignOrganization, "شخص حقوقی خارجی", 40));

    private static object Item(long id, string key, string title, int sortOrder) => new
    {
        Id = id,
        Key = key,
        Title = title,
        SortOrder = sortOrder,
        IsActive = true,
        CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc
    };
}

internal sealed class PartyContactTypeSeedConfiguration : IEntityTypeConfiguration<PartyContactType>
{
    public void Configure(EntityTypeBuilder<PartyContactType> builder) =>
        builder.HasData(
            Item(1, PartyReferenceKeys.ContactTypes.Mobile, "موبایل", 10),
            Item(2, PartyReferenceKeys.ContactTypes.Phone, "تلفن", 20),
            Item(3, PartyReferenceKeys.ContactTypes.Email, "ایمیل", 30));

    private static object Item(long id, string key, string title, int sortOrder) => new
    {
        Id = id,
        Key = key,
        Title = title,
        SortOrder = sortOrder,
        IsActive = true,
        CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc
    };
}

internal sealed class UnitPartyRelationTypeSeedConfiguration : IEntityTypeConfiguration<UnitPartyRelationType>
{
    public void Configure(EntityTypeBuilder<UnitPartyRelationType> builder) =>
        builder.HasData(
            Item(1, PartyReferenceKeys.RelationTypes.Owner, "مالک", true, false, 10),
            Item(2, PartyReferenceKeys.RelationTypes.Tenant, "مستأجر", false, true, 20),
            Item(3, PartyReferenceKeys.RelationTypes.Resident, "ساکن", false, true, 30),
            Item(4, PartyReferenceKeys.RelationTypes.LegalRepresentative, "نماینده قانونی", false, false, 40),
            Item(5, PartyReferenceKeys.RelationTypes.ContactPerson, "شخص رابط", false, false, 50),
            Item(6, PartyReferenceKeys.RelationTypes.Other, "سایر", false, false, 60));

    private static object Item(long id, string key, string title, bool ownership, bool occupancy,
        int sortOrder) => new
        {
            Id = id,
            Key = key,
            Title = title,
            Description = (string?)null,
            IsOwnershipRelation = ownership,
            IsOccupancyRelation = occupancy,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc
        };
}
