# Cursor → MariaDB (read-only) for Windows

Gives the client a **read-only** MariaDB connection inside Cursor, so they can ask things like:

> list me the tables in the database

## Connection details

| Field | Value |
|-------|--------|
| Host | `142.4.216.57` |
| Port | `17075` |
| Database | `perle-rareinfo` |
| User | `cursor_client` |
| Privileges | **SELECT only** on `perle-rareinfo` |

Password: see `infra/ovh/secrets/cursor-client.env` (local, not in git).  
Share it with the client out-of-band (password manager / secure chat), never commit it.

---

## Windows setup (≈ 5 minutes)

### 1. Install Node.js 20+

Download: https://nodejs.org/  
Check in PowerShell:

```powershell
node -v
npx -v
```

### 2. Add MCP config in Cursor

**Option A — project file (recommended)**

In the project root, create `.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "perlerare-mariadb": {
      "command": "npx",
      "args": ["-y", "@benborla29/mcp-server-mysql"],
      "env": {
        "MYSQL_HOST": "142.4.216.57",
        "MYSQL_PORT": "17075",
        "MYSQL_USER": "cursor_client",
        "MYSQL_PASS": "PASTE_PASSWORD_HERE",
        "MYSQL_DB": "perle-rareinfo"
      }
    }
  }
}
```

**Option B — global Cursor settings**

Cursor → **Settings** → **MCP** → add the same `perlerare-mariadb` block.

A copy of the template is in [`mcp.json.example`](./mcp.json.example).

### 3. Restart Cursor

Fully quit and reopen Cursor so the MCP server starts.

### 4. Test

In Agent chat:

```
List the tables in the perle-rareinfo database
```

or

```
How many rows are in the biens table?
```

Writes are disabled by default in this MCP server (no INSERT/UPDATE/DELETE).

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| MCP red / failed | Need Node 20+; run `npx -y @benborla29/mcp-server-mysql` once in PowerShell |
| Connection timeout | Check firewall/VPN; port **17075** must be reachable |
| Access denied | Confirm password from `cursor-client.env` |
| Wrong database | Keep `MYSQL_DB=perle-rareinfo` |

### Optional: SSH tunnel (safer)

If you prefer not to expose DB over the public internet:

```powershell
ssh -i git.key -p 2477 -L 17075:127.0.0.1:17075 administrateur@142.4.216.57
```

Then set in MCP:

```text
MYSQL_HOST=127.0.0.1
MYSQL_PORT=17075
```

(Keep the SSH window open while using Cursor.)

---

## Security notes

- User is **SELECT-only** on `perle-rareinfo` (no writes).
- Do **not** commit `.cursor/mcp.json` if it contains the password.
- Prefer rotating the password if it was shared in plain text.
