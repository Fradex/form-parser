# FormSqlTranslator

CLI skeleton for parsing `.frm/.php/.xml/.inc` forms, extracting Oracle SQL blocks and sending them to SQL translator microservice.

## Usage

```bash
dotnet run --project FormSqlTranslator -- \
  --input ./forms \
  --output ./out \
  --translator-url http://192.168.241.141:8081/sql \
  --mode both \
  --recursive true \
  --dry-run false \
  --save-intermediate true
```
