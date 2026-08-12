namespace BuildingManagement.Domain;

public sealed class UnitPartyRelation
{
    private UnitPartyRelation() { }
    public long Id { get; private set; }
    public long UnitId { get; private set; }
    public long PartyId { get; private set; }
    public long UnitPartyRelationTypeId { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public UnitPartyRelation(long unitId, long partyId, long relationTypeId,
        DateTimeOffset? startDate, DateTimeOffset? endDate, string? notes, DateTimeOffset now)
    {
        if (unitId <= 0) throw new DomainValidationException("unit", "Unit is required.");
        if (partyId <= 0) throw new DomainValidationException("party", "Party is required.");
        if (relationTypeId <= 0)
            throw new DomainValidationException("relationType", "Relation type is required.");
        UnitId = unitId;
        PartyId = partyId;
        UnitPartyRelationTypeId = relationTypeId;
        ValidateDates(startDate, endDate);
        StartDate = startDate?.ToUniversalTime();
        EndDate = endDate?.ToUniversalTime();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = now;
    }

    public void End(DateTimeOffset? endDate, DateTimeOffset now)
    {
        var terminationDate = (endDate ?? now).ToUniversalTime();
        ValidateDates(StartDate, terminationDate);
        EndDate = terminationDate;
        UpdatedAtUtc = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }

    private static void ValidateDates(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (startDate.HasValue && endDate < startDate)
            throw new DomainValidationException("endDate", "Cannot be earlier than start date.");
    }

}
