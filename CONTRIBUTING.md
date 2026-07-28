# Contributing

1. Open an issue or RFC for semantic, schema, security, or architecture changes.
2. Keep pull requests focused; do not mix a framework/package upgrade with a
   feature or migration redesign.
3. Add or update requirements, contracts, ADRs/RFCs, and tests where applicable.
4. Run `./scripts/verify.sh`; for persistence changes also run with
   `STOCKVERITY_TEST_POSTGRES` set.
5. Run `./scripts/smoke-compose.sh` for deployment-facing changes.
6. Never commit `.env`, logs, build outputs, data-protection keys, database
   dumps, tokens, or credentials.

PR descriptions must state behavior changed, migration impact, security impact,
evidence run, and remaining limitations.
