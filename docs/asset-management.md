# Asset Management

Asset Management records equipment owned by exactly one complex or one building. Assets use a five-character public code. Child identifiers remain internal except for the explicitly nested `AssetEvent.Id` described below.

`AssetEvent.EventDate` is historical data, not identity. Multiple events may occur on the same date. An event is addressed as a nested child by `Asset.Code` plus `AssetEvent.Id`; the ID is exposed only on the AssetEvent response for details, corrections, and attachments.

Reference data is seeded in Persian for asset types and event types. Development seeding adds representative Persian assets and events when a physical structure exists and no assets have been created.

`LastEventDate` and `SuggestedNextReviewDate` are read-model values. The latter uses the latest active event's explicit `SuggestedNextDate`, otherwise the event date plus the asset's optional review interval. Neither value is persisted and no schedule, reminder, notification, or recurring job is created.

Files reuse `base.StoredFiles` and local storage:

- `assets/{assetCode}/gallery/{generatedFileName}`
- `assets/{assetCode}/documents/{generatedFileName}`
- `assets/{assetCode}/events/{eventId}/{generatedFileName}`

Only one active gallery image can be the cover for an asset. Cover switching is transactional and a filtered unique SQL Server index is the final consistency guard.

`AssetEvent.Cost` is optional informational metadata only. It does not create or update any expense, charge, payment, balance, or accounting entry.

Run integration tests explicitly with `RUN_SQLSERVER_INTEGRATION_TESTS=true dotnet test`; without that opt-in, SQL Server-dependent tests skip cleanly.
