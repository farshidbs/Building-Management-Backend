namespace BuildingManagement.Domain;

internal static class DocumentRules
{
    internal static void Validate(DateTimeOffset? documentDate, DateTimeOffset? effectiveFrom,
        DateTimeOffset? expiresAt, bool requiresDocumentDate, bool supportsExpiration)
    {
        if (requiresDocumentDate && !documentDate.HasValue)
            throw new DomainValidationException("documentDate", "Document date is required for this document type.");
        if (expiresAt.HasValue && !supportsExpiration)
            throw new DomainValidationException("expiresAt", "This document type does not support expiration.");
        if (effectiveFrom.HasValue && expiresAt < effectiveFrom)
            throw new DomainValidationException("expiresAt", "Expiration cannot be earlier than effective date.");
    }
}
