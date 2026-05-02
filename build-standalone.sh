#!/usr/bin/env bash
set -euo pipefail

PROJECT="FormSqlTranslator/FormSqlTranslator.csproj"
CONFIGURATION="Release"
FRAMEWORK="net9.0"
OUTPUT_ROOT="publish"
RUNTIMES=("linux-x64" "linux-arm64" "win-x64" "win-arm64")

for rid in "${RUNTIMES[@]}"; do
  out_dir="$OUTPUT_ROOT/$rid"
  echo "Publishing $rid -> $out_dir"
  dotnet publish "$PROJECT" \
    -c "$CONFIGURATION" \
    -f "$FRAMEWORK" \
    -r "$rid" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -p:EnableCompressionInSingleFile=true \
    -o "$out_dir"
done

echo "Done. Standalone binaries are in ./$OUTPUT_ROOT"
