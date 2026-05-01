# FormSqlTranslator

CLI tool skeleton for parsing `.frm/.php/.xml/.inc` forms, extracting Oracle SQL blocks and sending them to SQL translator microservice.

## Implemented now

- file discovery for `.frm/.php/.xml/.inc`
- XML parser + fallback regex parser for malformed form files
- extraction from `component[cmptype=DataSet|SubSelect|Action|ActionRouter]`
- SQL classification (`PlainSql` / `AnonymousBlock`)
- translation client (`POST /sql`, `text/plain`, retry + timeout)
- anonymous block post-processing to `DO $$ ... $$`
- intermediate artifacts (`00-original`, `20-blocks`, `30-translation`, `40-postprocess`)

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
