# Current scope reviewed

The current implementation scope includes:

1. Discovery of input files: `.frm`, `.php`, `.xml`, `.inc`.
2. SQL extraction from components/tags:
   - `component[cmptype=DataSet|SubSelect|Action|ActionRouter|Script]`
   - `cmpAction`, `cmpDataSet` (legacy tags mapped to Action/DataSet).
3. SQL block classification:
   - `PlainSql`
   - `AnonymousBlock`
4. Translation via microservice (`POST /sql`, `text/plain`, retry + timeout).
5. Anonymous block post-processing to remove procedure wrapper and `call` artifacts.
6. Intermediate artifacts persistence (`00-original`, `20-blocks`, `30-translation`, `40-postprocess`).
7. Rewrite of XML components with PostgreSQL branches (`tmis|nmis|both`).
8. Output path strategy:
   - preserve relative directory structure under `--input` root.
9. Tests:
   - classifier, parser, postprocessor, conditions, output path helper.
10. CI:
   - GitHub Actions workflow for .NET 9 restore/build/test.

Open items (next scope):
- robust AST-level SQL extraction from Script blocks (currently regex heuristics)
- stronger XML rewrite matching for duplicate component names
- integration tests with fixture forms and mocked translator responses
