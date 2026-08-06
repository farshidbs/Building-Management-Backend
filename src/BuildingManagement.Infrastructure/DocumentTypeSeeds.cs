using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class DocumentTypeSeedConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder) =>
        builder.HasData(
            Item(1, DocumentTypeKeys.OwnershipDocument, "سند مالکیت", 10, true),
            Item(2, DocumentTypeKeys.Contract, "قرارداد", 20, true),
            Item(3, DocumentTypeKeys.InsurancePolicy, "بیمه‌نامه", 30, true, true),
            Item(4, DocumentTypeKeys.Permit, "مجوز", 40, true, true),
            Item(5, DocumentTypeKeys.BuildingPlan, "نقشه", 50),
            Item(6, DocumentTypeKeys.Invoice, "صورتحساب", 60, true),
            Item(7, DocumentTypeKeys.ManagementApproval, "رسید", 70, true),
            Item(8, DocumentTypeKeys.BoardMeetingMinutes, "گزارش", 80, true),
            Item(9, DocumentTypeKeys.OfficialLetter, "گواهی", 90, true, true),
            Item(10, DocumentTypeKeys.Other, "سایر", 100));

    private static object Item(long id, string key, string title, int sortOrder,
        bool requiresDocumentDate = false, bool supportsExpiration = false) =>
        new
        {
            Id = id,
            Key = key,
            Title = title,
            Description = (string?)null,
            RequiresDocumentDate = requiresDocumentDate,
            SupportsExpiration = supportsExpiration,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc
        };
}
