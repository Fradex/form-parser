# Verification runbook

This repository requires .NET SDK 9.0.x.

## Install

Follow official instructions:
https://dotnet.microsoft.com/en-us/download/dotnet/9.0

## Verify SDK

```bash
dotnet --info
```

## Build and test

```bash
dotnet restore FormSqlTranslator.sln
dotnet build FormSqlTranslator.sln -c Release
dotnet test FormSqlTranslator.sln -c Release
```

## Run translator

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
