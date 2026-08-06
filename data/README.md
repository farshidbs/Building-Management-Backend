# Iran location dataset

`iran-locations.json` is the repository-local bootstrap dataset for the supported hierarchy:

`ایران → استان → شهر`

It contains 31 provinces and 1,119 cities. The source identifiers were intentionally omitted because they are not stable domain identifiers in this application. Parentage and Persian names are preserved.

## Provenance

- Source repository: `mahdi-eth/Iran-Cities-Data`
- Source commit: `2aa79daec4829c44c1da0e0855bb0afc022076cb`
- Source commit date: `2024-01-06T04:45:50Z`
- License: MIT
- Imported source files: `JSON/provinces.json` and `JSON/cities.json`

The exact source URLs and commit are also embedded in the JSON file so database restoration does not require reading a mutable external repository.

## Import behavior

The initial database load performed for this project:

- reuses an existing active `ایران` country row when present;
- reuses an existing province or city under the same parent when present;
- creates missing rows with the existing five-character public-code policy;
- does not delete, rename, or deactivate existing locations;
- uses the existing `ParentId` hierarchy and does not add a redundant hierarchy column.

Future re-imports should remain idempotent by matching normalized name, parent, and location type.
