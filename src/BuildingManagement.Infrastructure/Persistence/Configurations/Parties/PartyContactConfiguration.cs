using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class PartyContactConfiguration : IEntityTypeConfiguration<PartyContact>
{
    public void Configure(EntityTypeBuilder<PartyContact> builder)
    {
        builder.ToTable("PartyContacts", ConfigurationHelpers.Schema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.Value).HasMaxLength(320).IsRequired();
        builder.Property(x => x.NormalizedValue).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(100);
        builder.Property(x => x.VerifiedAtUtc).HasPrecision(0);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartyContactType>().WithMany().HasForeignKey(x => x.PartyContactTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PartyId);
        builder.HasIndex(x => x.NormalizedValue);
        builder.HasIndex(x => new { x.PartyId, x.PartyContactTypeId }).IsUnique()
            .HasFilter("[IsActive] = CAST(1 AS bit) AND [IsPrimary] = CAST(1 AS bit)");
    }
}
