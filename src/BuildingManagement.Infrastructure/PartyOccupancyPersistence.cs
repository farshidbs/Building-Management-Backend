using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class PartyTypeConfiguration : IEntityTypeConfiguration<PartyType>
{
    public void Configure(EntityTypeBuilder<PartyType> builder)
    {
        ConfigurationHelpers.Reference(builder, "PartyTypes", "base");
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}

internal sealed class PartyContactTypeConfiguration : IEntityTypeConfiguration<PartyContactType>
{
    public void Configure(EntityTypeBuilder<PartyContactType> builder) =>
        ConfigurationHelpers.Reference(builder, "PartyContactTypes", "base");
}

internal sealed class PartyIdentifierTypeConfiguration : IEntityTypeConfiguration<PartyIdentifierType>
{
    public void Configure(EntityTypeBuilder<PartyIdentifierType> builder) =>
        ConfigurationHelpers.Reference(builder, "PartyIdentifierTypes", "base");
}

internal sealed class UnitPartyRelationTypeConfiguration : IEntityTypeConfiguration<UnitPartyRelationType>
{
    public void Configure(EntityTypeBuilder<UnitPartyRelationType> builder)
    {
        ConfigurationHelpers.Reference(builder, "UnitPartyRelationTypes");
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}

internal sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        ConfigurationHelpers.Entity(builder, "Parties", "base");
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.OrganizationName).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasOne<PartyType>().WithMany().HasForeignKey(x => x.PartyTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PartyTypeId);
        builder.HasIndex(x => x.NormalizedDisplayName);
    }
}

internal sealed class PartyContactConfiguration : IEntityTypeConfiguration<PartyContact>
{
    public void Configure(EntityTypeBuilder<PartyContact> builder)
    {
        ConfigurationHelpers.Entity(builder, "PartyContacts", "base");
        builder.Property(x => x.Value).HasMaxLength(320).IsRequired();
        builder.Property(x => x.NormalizedValue).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(100);
        builder.Property(x => x.VerifiedAtUtc).HasPrecision(0);
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartyContactType>().WithMany().HasForeignKey(x => x.PartyContactTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PartyId);
        builder.HasIndex(x => x.NormalizedValue);
        builder.HasIndex(x => new { x.PartyId, x.PartyContactTypeId }).IsUnique()
            .HasFilter("[IsActive] = CAST(1 AS bit) AND [IsPrimary] = CAST(1 AS bit)");
    }
}

internal sealed class PartyIdentifierConfiguration : IEntityTypeConfiguration<PartyIdentifier>
{
    public void Configure(EntityTypeBuilder<PartyIdentifier> builder)
    {
        ConfigurationHelpers.Entity(builder, "PartyIdentifiers", "base");
        builder.Property(x => x.CountryCode).HasColumnType("char(2)").IsRequired();
        builder.Property(x => x.Value).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedValue).HasMaxLength(200).IsRequired();
        builder.Property(x => x.VerifiedAtUtc).HasPrecision(0);
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartyIdentifierType>().WithMany().HasForeignKey(x => x.PartyIdentifierTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PartyId);
        builder.HasIndex(x => new { x.CountryCode, x.PartyIdentifierTypeId, x.NormalizedValue });
    }
}

internal sealed class UnitPartyRelationConfiguration : IEntityTypeConfiguration<UnitPartyRelation>
{
    public void Configure(EntityTypeBuilder<UnitPartyRelation> builder)
    {
        ConfigurationHelpers.Entity(builder, "UnitPartyRelations");
        builder.Property(x => x.StartDate).HasPrecision(0);
        builder.Property(x => x.EndDate).HasPrecision(0);
        builder.Property(x => x.OwnershipShare).HasPrecision(5, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitPartyRelationType>().WithMany().HasForeignKey(x => x.UnitPartyRelationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.UnitId, x.IsActive, x.EndDate });
        builder.HasIndex(x => new { x.UnitId, x.PartyId, x.UnitPartyRelationTypeId, x.EndDate });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_UnitPartyRelations_Dates",
                "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]");
            table.HasCheckConstraint("CK_UnitPartyRelations_OwnershipShare",
                "[OwnershipShare] IS NULL OR ([OwnershipShare] > 0 AND [OwnershipShare] <= 100)");
        });
    }
}

internal sealed class UnitOccupancyHistoryConfiguration : IEntityTypeConfiguration<UnitOccupancyHistory>
{
    public void Configure(EntityTypeBuilder<UnitOccupancyHistory> builder)
    {
        builder.ToTable("UnitOccupancyHistories", ConfigurationHelpers.Schema, table =>
        {
            table.HasCheckConstraint("CK_UnitOccupancyHistories_Count", "[OccupantsCount] >= 0");
            table.HasCheckConstraint("CK_UnitOccupancyHistories_Dates",
                "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.EffectiveFrom).HasPrecision(0).IsRequired();
        builder.Property(x => x.EffectiveTo).HasPrecision(0);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.UnitId, x.EffectiveFrom });
        builder.HasIndex(x => x.UnitId).IsUnique()
            .HasFilter("[IsActive] = CAST(1 AS bit) AND [EffectiveTo] IS NULL");
    }
}
