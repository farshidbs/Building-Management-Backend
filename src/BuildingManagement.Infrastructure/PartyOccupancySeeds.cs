using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class PartyTypeSeedConfiguration : IEntityTypeConfiguration<PartyType>
{
    public void Configure(EntityTypeBuilder<PartyType> builder) =>
        builder.HasData(
            Item(1, PartyReferenceKeys.PartyTypes.Person, "شخص", 10),
            Item(2, PartyReferenceKeys.PartyTypes.Organization, "سازمان", 20));

    private static object Item(long id, string key, string title, int sortOrder) => new
    {
        Id = id,
        Key = key,
        Title = title,
        Description = (string?)null,
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

internal sealed class PartyIdentifierTypeSeedConfiguration : IEntityTypeConfiguration<PartyIdentifierType>
{
    public void Configure(EntityTypeBuilder<PartyIdentifierType> builder) =>
        builder.HasData(
            Item(1, PartyReferenceKeys.IdentifierTypes.NationalId, "کد ملی", 10),
            Item(2, PartyReferenceKeys.IdentifierTypes.LegalEntityNationalId, "شناسه ملی شخص حقوقی", 20),
            Item(3, PartyReferenceKeys.IdentifierTypes.PassportNumber, "شماره گذرنامه", 30),
            Item(4, PartyReferenceKeys.IdentifierTypes.ResidenceIdentifier, "شناسه اقامت", 40),
            Item(5, PartyReferenceKeys.IdentifierTypes.Other, "سایر", 50));

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
            Item(1, PartyReferenceKeys.RelationTypes.Owner, "مالک", true, false, true, 10),
            Item(2, PartyReferenceKeys.RelationTypes.Tenant, "مستأجر", false, true, true, 20),
            Item(3, PartyReferenceKeys.RelationTypes.Resident, "ساکن", false, true, false, 30),
            Item(4, PartyReferenceKeys.RelationTypes.LegalRepresentative, "نماینده قانونی", false, false, true, 40),
            Item(5, PartyReferenceKeys.RelationTypes.ContactPerson, "شخص رابط", false, false, true, 50),
            Item(6, PartyReferenceKeys.RelationTypes.Other, "سایر", false, false, false, 60));

    private static object Item(long id, string key, string title, bool ownership, bool occupancy,
        bool paymentContact, int sortOrder) => new
        {
            Id = id,
            Key = key,
            Title = title,
            Description = (string?)null,
            IsOwnershipRelation = ownership,
            IsOccupancyRelation = occupancy,
            CanBePaymentContact = paymentContact,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc
        };
}
