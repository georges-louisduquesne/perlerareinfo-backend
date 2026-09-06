# Quick check that this Windows PC can run the Cursor MariaDB MCP
Write-Host "=== Perle Rare MCP setup check ===" -ForegroundColor Cyan

$node = Get-Command node -ErrorAction SilentlyContinue
$npx = Get-Command npx -ErrorAction SilentlyContinue

if (-not $node) {
  Write-Host "FAIL: Node.js not found. Install LTS from https://nodejs.org/ then reopen PowerShell." -ForegroundColor Red
  exit 1
}

Write-Host "OK: node $($node.Source)"
Write-Host "    version: $(node -v)"

if (-not $npx) {
  Write-Host "FAIL: npx not found (should come with Node.js)." -ForegroundColor Red
  exit 1
}

Write-Host "OK: npx available"
Write-Host ""
Write-Host "Next:" -ForegroundColor Yellow
Write-Host "  1. Copy mcp.json to YOUR_PROJECT\.cursor\mcp.json"
Write-Host "  2. Restart Cursor"
Write-Host "  3. Paste CURSOR-PROMPT.md into Agent chat"
Write-Host ""
Write-Host "Optional: warm up the MCP package (first download)..." -ForegroundColor DarkGray
npx -y @benborla29/mcp-server-mysql --help 2>$null
Write-Host "Done." -ForegroundColor Green
