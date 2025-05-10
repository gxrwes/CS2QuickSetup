# -----------------------------------------------------------------------------
# kill-unwanted-apps.ps1
# Run this as Administrator before launching Counter-Strike 2 to maximize FPS.
# -----------------------------------------------------------------------------

# 1) Ensure we’re running elevated
if (-not ([Security.Principal.WindowsPrincipal] `
         [Security.Principal.WindowsIdentity]::GetCurrent()) `
         .IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning "Please re-launch PowerShell as Administrator and rerun this script."
    exit 1
}

# 2) Define process names (without “.exe”) you want to keep running
$keep = @(
    'steam',           # Steam client
    'cs2',             # Counter-Strike 2 executable (adjust if name differs)
    'discord',         # Discord
    'powershell',      # This host (so you don’t kill the script)
    'pwsh',            # PowerShell Core, if used
    'explorer',        # Windows shell
    'dwm',             # Desktop Window Manager
    'taskhostw',       # Task Host for Windows
    'conhost',         # Console window host
    'taskmgr',         # In case you have Task Manager open
    'applicationframehost',
    'shellexperiencehost'
)

# 3) Grab current session so we only target your interactive apps
$mySession = (Get-Process -Id $PID).SessionId

# 4) Stop all processes in this session not in $keep
Get-Process |
  Where-Object {
    $_.SessionId -eq $mySession -and
    ($keep -notcontains $_.ProcessName)
  } |
  ForEach-Object {
    try {
      Stop-Process -Id $_.Id -Force -ErrorAction Stop
      Write-Host "Stopped $($_.ProcessName) (PID $($_.Id))"
    }
    catch {
      Write-Warning "Could not stop $($_.ProcessName): $_"
    }
  }

# 5) Optionally, stop common background services (MySQL, PostgreSQL, Docker)
#    Uncomment and edit service names as needed:
# $servicesToStop = @('mysql','mysql57','postgresql-x64-13','com.docker.service')
# foreach ($svc in $servicesToStop) {
#   if (Get-Service -Name $svc -ErrorAction SilentlyContinue) {
#     Stop-Service -Name $svc -Force -ErrorAction SilentlyContinue
#     Write-Host "Stopped service $svc"
#   }
# }
