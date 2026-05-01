# FormSqlTranslator

CLI tool skeleton for parsing `.frm/.php/.xml/.inc` forms, extracting Oracle SQL blocks and sending them to SQL translator microservice.

## Implemented now

- file discovery for `.frm/.php/.xml/.inc`
- XML parser + fallback regex parser for malformed form files
- extraction from `component[cmptype=DataSet|SubSelect|Action|ActionRouter|Script]` (including SQL-like calls in Script CDATA)
- SQL classification (`PlainSql` / `AnonymousBlock`)
- translation client (`POST /sql`, `text/plain`, retry + timeout)
- anonymous block post-processing to `DO $$ ... $$`
- anonymous wrapper `CREATE OR REPLACE PROCEDURE/FUNCTION pg_temp.func_* ... CALL/SELECT pg_temp.func_*` is converted to `DO $$ ... $$`
- intermediate artifacts (`00-original`, `20-blocks`, `30-translation`, `40-postprocess`)
- XML rewrite step that injects/updates POSTGRE ActionRouter/SubSelect branches (`mode=tmis|nmis|both`)
- out/ output preserves relative directory structure of files under the input root

## Usage

```bash
dotnet run --project FormSqlTranslator -- \
  --input ./forms \
  --output ./out \
  --translator-url http://192.168.241.141:8081/sql \
  --mode both \
  --recursive true \
  --dry-run false \
  --save-intermediate true \
  --max-degree 4
```

## Tests

```bash
dotnet test FormSqlTranslator.sln
```

Added tests:
- `ConditionTemplateServiceTests`
- `SqlBlockClassifierTests`
- `AnonymousBlockPostProcessorTests`
