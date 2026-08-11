# Asset Management

Asset Management records equipment owned by exactly one complex or one building. Assets use a five-character public code; internal child/history identifiers are not returned by the API.

Reference data is seeded in Persian for asset types and event types. Development seeding adds representative Persian assets and events when a physical structure exists and no assets have been created.

`LastEventDate` and `SuggestedNextReviewDate` are read-model values. The latter uses the latest active event's explicit `SuggestedNextDate`, otherwise the event date plus the asset's optional review interval. Neither value is persisted and no schedule, reminder, notification, or recurring job is created.

Files reuse `base.StoredFiles` and local storage:

- `assets/{assetCode}/gallery/{generatedFileName}`
- `assets/{assetCode}/documents/{generatedFileName}`
- `assets/{assetCode}/events/{generatedFileName}`

Only one active gallery image can be the cover for an asset. Cover switching is transactional and a filtered unique SQL Server index is the final consistency guard.

Run integration tests explicitly with `RUN_SQLSERVER_INTEGRATION_TESTS=true dotnet test`; without that opt-in, SQL Server-dependent tests skip cleanly.
