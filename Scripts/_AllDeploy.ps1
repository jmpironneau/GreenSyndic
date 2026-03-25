#Requires -RunAsAdministrator
# ════════════════════════════════════════════════════
#   GREENSYNDIC - Full Deployment (IIS + SSL)
#   5 apps + Lets Encrypt certificates
# ════════════════════════════════════════════════════

$ErrorActionPreference = "Stop"
trap {
    Write-Host ""
    Write-Host "*** ERREUR ***" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "Fichier: $($_.InvocationInfo.ScriptName)" -ForegroundColor Yellow
    Write-Host "Ligne:   $($_.InvocationInfo.ScriptLineNumber)" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Appuyez sur Entree pour fermer"
    exit 1
}
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = Split-Path -Parent $ScriptDir
$Dest = "C:\inetpub\greensyndic"
$Psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$Wacs = "C:\tools\win-acme\wacs.exe"

$BackendDomain   = "backendgreensyndic.entreezen.com"
$DeskDomain      = "gestiongreensyndic.entreezen.com"
$MobSyndicDomain = "gestionmobgreensyndic.entreezen.com"
$MobProprioDomain = "proprietairegreensyndic.entreezen.com"
$MobLocDomain    = "locatairegreensyndic.entreezen.com"

$Sites = @(
    @{ Name="GS-Backend";    Csproj="Backend\GreenSyndic.Api\GreenSyndic.Api.csproj";       Folder="backend";    Domain=$BackendDomain },
    @{ Name="GS-DeskSyndic"; Csproj="Frontend\DeskSyndic\DeskSyndic.csproj";                Folder="desksyndic"; Domain=$DeskDomain },
    @{ Name="GS-MobSyndic";  Csproj="Frontend\MobSyndic\MobSyndic.csproj";                  Folder="mobsyndic";  Domain=$MobSyndicDomain },
    @{ Name="GS-MobProprio"; Csproj="Frontend\MobProprio\MobProprio.csproj";                Folder="mobproprio"; Domain=$MobProprioDomain },
    @{ Name="GS-MobLoc";     Csproj="Frontend\MobLoc\MobLoc.csproj";                        Folder="mobloc";     Domain=$MobLocDomain }
)

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  GREENSYNDIC - Full Deployment (IIS + SSL)" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# ════════════════════════════════════════════════════
# STEP 1 : Stop IIS + Kill processes
# ════════════════════════════════════════════════════
Write-Host "(1/7) Stopping IIS and killing processes..." -ForegroundColor Yellow

iisreset /stop 2>$null | Out-Null
# Kill old GreenSyndic processes + wacs
foreach ($proc in @("GreenSyndic.Api", "DeskSyndic", "MobSyndic", "MobProprio", "MobLoc", "Seed", "wacs")) {
    Get-Process -Name $proc -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}
# Kill old cmd windows with GreenSyndic in title (previous deploy runs)
Get-Process cmd -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -match "GreenSyndic|AllDeploy" } | Stop-Process -Force -ErrorAction SilentlyContinue
foreach ($port in 5050..5055) {
    Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | ForEach-Object {
        Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "  OK - Cleanup complete" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════
# STEP 2 : Publish 5 apps
# ════════════════════════════════════════════════════
Write-Host "(2/7) Publishing 5 apps (Release)..." -ForegroundColor Yellow

if (-not (Test-Path $Dest)) { New-Item -ItemType Directory -Path $Dest | Out-Null }

foreach ($site in $Sites) {
    Write-Host "       $($site.Name)..."
    $csproj = Join-Path $Root $site.Csproj
    $output = Join-Path $Dest $site.Folder

    if (Test-Path $output) { Remove-Item $output -Recurse -Force }

    dotnet publish $csproj -c Release -o $output --nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERREUR: Failed to publish $($site.Name)" -ForegroundColor Red
        Read-Host "Appuyez sur Entree"
        exit 1
    }
}

Write-Host "  OK - 5 apps published to $Dest" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════
# STEP 3 : Patch API URLs for production
# ════════════════════════════════════════════════════
Write-Host "(3/7) Patching API URLs for production..." -ForegroundColor Yellow

$backendUrl = "https://$BackendDomain"
foreach ($folder in @("desksyndic", "mobsyndic", "mobproprio", "mobloc")) {
    $jsDir = Join-Path $Dest "$folder\wwwroot\js"
    foreach ($jsFile in @("api.js", "app.js")) {
        $jsPath = Join-Path $jsDir $jsFile
        if (Test-Path $jsPath) {
            (Get-Content $jsPath -Raw) -replace 'http://localhost:5050', $backendUrl | Set-Content $jsPath -NoNewline
            Write-Host "       Patched $folder\$jsFile"
        }
    }
}

Write-Host "  OK - URLs patched" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════
# STEP 4 : Set permissions
# ════════════════════════════════════════════════════
Write-Host "(4/7) Setting permissions..." -ForegroundColor Yellow

icacls $Dest /grant "IIS_IUSRS:(OI)(CI)RX" /T /Q | Out-Null
icacls $Dest /grant "IUSR:(OI)(CI)RX" /T /Q | Out-Null

Write-Host "  OK - Permissions set" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════
# STEP 5 : Configure IIS sites + AppPools
# ════════════════════════════════════════════════════
Write-Host "(5/7) Configuring IIS sites..." -ForegroundColor Yellow

Import-Module WebAdministration

foreach ($site in $Sites) {
    $poolName = $site.Name
    $sitePath = Join-Path $Dest $site.Folder

    # AppPool
    if (-not (Test-Path "IIS:\AppPools\$poolName")) {
        New-WebAppPool -Name $poolName | Out-Null
    }
    # No Managed Code + LocalSystem (access to PostgreSQL via localhost)
    Set-ItemProperty "IIS:\AppPools\$poolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty "IIS:\AppPools\$poolName" -Name processModel.identityType -Value 0

    # Remove existing site
    if (Test-Path "IIS:\Sites\$($site.Name)") {
        Remove-Website -Name $site.Name
    }

    # Create site with HTTP binding (port 80)
    New-Website -Name $site.Name -PhysicalPath $sitePath -ApplicationPool $poolName -HostHeader $site.Domain -Port 80 | Out-Null
    Write-Host "       $($site.Name) -> $($site.Domain)"
}

Write-Host "  OK - 5 IIS sites configured" -ForegroundColor Green
Write-Host ""

# ════════════════════════════════════════════════════
# STEP 6 : Start IIS
# ════════════════════════════════════════════════════
Write-Host "(6/7) Starting IIS..." -ForegroundColor Yellow
iisreset /start 2>$null | Out-Null

Write-Host "       Waiting for Backend..."
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $httpCode = & curl.exe -k -s -o NUL -w "%{http_code}" "http://$BackendDomain/api/version" 2>$null
        Write-Host "       Attempt $($i+1)/30 - HTTP $httpCode" -ForegroundColor Gray
        if ($httpCode -eq "200") { $ready = $true; break }
    } catch {}
    Start-Sleep -Seconds 3
}
if (-not $ready) {
    Write-Host "  WARN - Backend not responding after 90s - check IIS logs" -ForegroundColor Yellow
} else {
    Write-Host "  OK - Backend ready" -ForegroundColor Green
}
Write-Host ""

# ════════════════════════════════════════════════════
# STEP 7 : SSL Certificates (Lets Encrypt via win-acme)
# ════════════════════════════════════════════════════
Write-Host "(7/7) Creating SSL certificates (Lets Encrypt)..." -ForegroundColor Yellow

if (-not (Test-Path $Wacs)) {
    Write-Host "  WARN - win-acme not found at $Wacs" -ForegroundColor Yellow
    Write-Host "       Download from: https://www.win-acme.com/" -ForegroundColor Yellow
    Write-Host "       SSL certificates skipped" -ForegroundColor Yellow
} else {
    # Stop only GreenSyndic sites to free port 80 bindings for ACME
    Write-Host "       Stopping GreenSyndic sites for ACME validation..." -ForegroundColor Gray
    foreach ($site in $Sites) {
        Stop-Website -Name $site.Name -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2

    foreach ($site in $Sites) {
        Write-Host "       Requesting certificate for $($site.Domain)..."
        & $Wacs --target manual --host $site.Domain --validation selfhosting --store certificatestore --certificatestore My --accepttos --emailaddress "admin@greensyndic.ci" --closeonfinish --nocache --force 2>&1 | ForEach-Object {
            $line = $_.ToString()
            Write-Host "       $line" -ForegroundColor Gray
        }
    }

    # Bind certificates to sites manually
    Write-Host "       Binding certificates to IIS sites..." -ForegroundColor Gray
    foreach ($site in $Sites) {
        $cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -match $site.Domain } | Sort-Object NotAfter -Descending | Select-Object -First 1
        if ($cert) {
            # Add HTTPS binding
            $existing = Get-WebBinding -Name $site.Name -Protocol https -ErrorAction SilentlyContinue
            if (-not $existing) {
                New-WebBinding -Name $site.Name -Protocol https -Port 443 -HostHeader $site.Domain -SslFlags 1
            }
            $binding = Get-WebBinding -Name $site.Name -Protocol https
            $binding.AddSslCertificate($cert.Thumbprint, "My")
            Write-Host "       Bound certificate to $($site.Name)" -ForegroundColor Green
        } else {
            Write-Host "       WARN - No certificate found for $($site.Domain)" -ForegroundColor Yellow
        }
    }

    # Restart GreenSyndic sites
    Write-Host "       Starting GreenSyndic sites..." -ForegroundColor Gray
    foreach ($site in $Sites) {
        Start-Website -Name $site.Name -ErrorAction SilentlyContinue
    }
    Write-Host "  OK - SSL certificates created and bound" -ForegroundColor Green
}

Write-Host ""

# ════════════════════════════════════════════════════
# Summary
# ════════════════════════════════════════════════════
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Deployment complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Backend   : https://$BackendDomain"
Write-Host "  Gestion   : https://$DeskDomain"
Write-Host "  Mob Syndic: https://$MobSyndicDomain"
Write-Host "  Proprio   : https://$MobProprioDomain"
Write-Host "  Locataire : https://$MobLocDomain"
Write-Host ""
Write-Host "  Swagger   : https://$BackendDomain/swagger"
Write-Host ""
Read-Host "Appuyez sur Entree"
