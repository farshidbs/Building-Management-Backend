namespace BuildingManagement.Domain;

public sealed class UnitOccupancyHistory
{
    private UnitOccupancyHistory() { }
    public long Id { get; private set; }
    public long UnitId { get; private set; }
    public int OccupantsCount { get; private set; }
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public UnitOccupancyHistory(long unitId, int occupantsCount, DateTimeOffset? effectiveFrom,
        string? notes, DateTimeOffset now)
    {
        if (unitId <= 0) throw new DomainValidationException("unit", "Unit is required.");
        if (occupantsCount < 0) throw new DomainValidationException("occupantsCount", "Must not be negative.");
        UnitId = unitId;
        OccupantsCount = occupantsCount;
        EffectiveFrom = effectiveFrom?.ToUniversalTime();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = now;
    }

    public void Close(DateTimeOffset? effectiveTo, DateTimeOffset now)
    {
        if (effectiveTo.HasValue && EffectiveFrom.HasValue && effectiveTo < EffectiveFrom)
            throw new DomainValidationException("effectiveFrom", "Cannot be earlier than current occupancy effective date.");
        EffectiveTo = effectiveTo?.ToUniversalTime();
        IsActive = false;
        UpdatedAtUtc = now;
    }
}
