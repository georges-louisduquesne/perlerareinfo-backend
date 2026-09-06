# Setup on this Windows PC (5 minutes)

## 1. Install Node.js 20+

1. Open https://nodejs.org/
2. Download the **LTS** installer (Windows)
3. Install with default options
4. **Close and reopen** PowerShell, then run:

```powershell
node -v
npx -v
```

Both commands should print a version. If not, restart the PC and try again.

Optional check from this folder:

```powershell
cd path\to\windows-kit
.\check-setup.ps1
```

## 2. Install / open Cursor

https://cursor.com/

## 3. Add the MariaDB MCP

### Recommended: project `.cursor/mcp.json`

1. Open your project folder in Cursor (or create an empty folder for DB questions).
2. In that project, create a folder named `.cursor` (with the leading dot).
3. Copy **`mcp.json`** from this kit into:

```text
YOUR_PROJECT\.cursor\mcp.json
```

Example:

```text
C:\Users\You\Documents\perlerare-db\.cursor\mcp.json
```

### Alternative: Cursor settings UI

1. Cursor → **Settings** → **MCP**
2. Add a new server and paste the contents of `mcp.json` (the `perlerare-mariadb` block under `mcpServers`).

## 4. Restart Cursor

Fully quit Cursor (tray icon too), then reopen the project.

In **Settings → MCP**, `perlerare-mariadb` should show as connected (green).

First start may take 30–60s while `npx` downloads the package.

## 5. Use it

1. Open **Agent** chat (not only Composer if MCP tools are disabled there).
2. Open **`CURSOR-PROMPT.md`** and copy the whole prompt.
3. Paste it as your first message.
4. Then ask e.g. *“List all tables”*.

---

## If MCP fails

| Problem | Fix |
|---------|-----|
| Node not found | Reinstall Node LTS, restart Cursor |
| Red / error MCP | In PowerShell: `npx -y @benborla29/mcp-server-mysql` once, then restart Cursor |
| Timeout | Check internet; port **17075** must not be blocked by firewall/VPN |
| Access denied | Open `credentials.env` and confirm password matches `mcp.json` |
