# Perle Rare MariaDB — copy into Cursor Agent as your first message
# ---------------------------------------------------------------------------

You have access to the Perle Rare MariaDB database via the MCP server **perlerare-mariadb**.

## Connection context
- Database: `perle-rareinfo`
- Access: **READ-ONLY** (SELECT only — never try INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE)
- Engine: MariaDB on OVH (production data)

## How to work
1. Use the MySQL MCP tools to inspect schema and run SELECT queries.
2. Prefer safe queries: `SHOW TABLES`, `DESCRIBE table`, `SELECT ... LIMIT ...`
3. Always use `LIMIT` on large tables (default `LIMIT 50` unless I ask for more).
4. If a query might be heavy, explain briefly and use aggregates / limits.
5. Never invent table or column names — verify with the MCP first.
6. Answer in the same language I use (French or English).
7. When listing tables, group them if useful (contacts, biens, annonces, etc.).

## First task
List all tables in the `perle-rareinfo` database, then briefly describe what this CRM database seems to cover based on table names.

After that, wait for my next question.
