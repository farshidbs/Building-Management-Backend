using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class UnitPartyRelationConfiguration : IEntityTypeConfiguration<UnitPartyRelation>
{
    public void Configure(EntityTypeBuilder<UnitPartyRelation> builder)
    {
        builder.ToTable("UnitPartyRelations", ConfigurationHelpers.Schema, table =>
            table.HasCheckConstraint("CK_UnitPartyRelations_Dates",
                "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.StartDate).HasPrecision(0);
        builder.Property(x => x.EndDate).HasPrecision(0);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitPartyRelationType>().WithMany().HasForeignKey(x => x.UnitPartyRelationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.UnitId, x.IsActive, x.EndDate });
        builder.HasIndex(x => new { x.UnitId, x.PartyId, x.UnitPartyRelationTypeId, x.EndDate });
    }
}
