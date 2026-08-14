using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        ConfigurationHelpers.Entity(builder, "Parties");
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.OrganizationName).HasMaxLength(200);
        builder.Property(x => x.IdentityNumber).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.BirthDate).HasColumnType("date");
        builder.HasOne<PartyType>().WithMany().HasForeignKey(x => x.PartyTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PartyTypeId);
        builder.HasIndex(x => x.NormalizedDisplayName);
    }
}
