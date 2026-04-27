$ErrorActionPreference = "Stop"

Write-Host "=== Lock to SemaphoreSlim Conversion Script ===" -ForegroundColor Cyan
Write-Host "Working directory: $pwd" -ForegroundColor Gray
Write-Host ""

function ConvertLockToSemaphore {
    param(
        [string]$FilePath,
        [string]$LockObject,
        [string]$FieldOld,
        [string]$FieldNew,
        [string]$SemVarName,
        [bool]$AddNewFieldBefore = $false,
        [string]$NewFieldName = "",
        [bool]$AddStaticField = $false,
        [string]$StaticFieldText = ""
    )
    
    Write-Host "Processing: $FilePath" -ForegroundColor Cyan
    Write-Host "  Lock object: $LockObject" -ForegroundColor Gray
    Write-Host "  Semaphore var: $SemVarName" -ForegroundColor Gray
    
    # Read file content
    $content = Get-Content -Path $FilePath -Raw -Encoding UTF8
    
    # Step 1: Replace field declaration if needed
    if ($FieldOld -and $FieldNew) {
        Write-Host "  Replacing field declaration..." -ForegroundColor Gray
        $content = $content.Replace($FieldOld, $FieldNew)
    }
    
    # Step 2: Add static field if needed (WebServer.cs case)
    if ($AddStaticField -and $StaticFieldText) {
        $lines = $content.Split([char]10)
        $newLines = @()
        $added = $false
        foreach ($line in $lines) {
            $newLines += $line
            if (!$added -and $line -match "^    private static ") {
                $newLines += $StaticFieldText
                $added = $true
            }
        }
        $content = ($newLines -join "`n")
        Write-Host "  Added static field..." -ForegroundColor Gray
    }
    
    # Step 3: Convert lock statements
    Write-Host "  Converting lock statements..." -ForegroundColor Gray
    $lines = $content.Split([char]10)
    $result = @()
    $i = 0
    
    while ($i -lt $lines.Count) {
        $line = $lines[$i]
        
        # Check if this line contains the lock statement
        if ($line -match "^\s+lock\s*\(\s*[\"$LockObject\s]+\)\s*\{?") {
            # Extract the indentation
            $match = $line -match "^(\s*)lock"
            $indent = $matches[1]
            
            # Find the matching closing brace
            $braceCount = 1
            $lockEnd = -1
            for ($j = $i + 1; $j -lt $lines.Count; $j++) {
                $countOpen = ([regex]::Matches($lines[$j], '\{')).Count
                $countClose = ([regex]::Matches($lines[$j], '\}')).Count
                $braceCount += $countOpen - $countClose
                if ($braceCount -le 0) {
                    $lockEnd = $j
                    break
                }
            }
            
            if ($lockEnd -eq -1) {
                Write-Host "  ERROR: Could not find closing brace for lock at line $($i + 1)" -ForegroundColor Red
                $result += $line
                $i++
                continue
            }
            
            # Replace lock line with semaphore.Wait(); try {
            $result += "${indent}${SemVarName}.Wait(); try {"
            
            # Add inner lines (between lock open and close)
            for ($j = $i + 1; $j -lt $lockEnd; $j++) {
                $result += $lines[$j]
            }
            
            # Add finally block
            $result += "${indent}finally { ${SemVarName}.Release(); }"
            
            $i = $lockEnd + 1
        } else {
            $result += $line
            $i++
        }
    }
    
    # Step 4: Add new semaphore field if needed (for locks on existing collections)
    if ($AddNewFieldBefore -and $NewFieldName) {
        $newLines = @()
        $added = $false
        foreach ($line in $result) {
            $newLines += $line
            # Check if this is the line where we should add the field
            if (!$added -and $line -match "internal SortedSet<Address> $NewFieldName.Replace('Lock','')") {
                $newLines += "        private readonly SemaphoreSlim $(($NewFieldName).Replace('Lock','Lock')) = new SemaphoreSlim(1, 1);"
                $added = $true
            }
        }
        $result = $newLines
        Write-Host "  Added new semaphore field..." -ForegroundColor Gray
    }
    
    # Write back
    $output = ($result -join "`n").TrimEnd()
    Set-Content -Path $FilePath -Value $output -Encoding UTF8
    Write-Host "  Done." -ForegroundColor Green
}

# ============================================================
# File 1: TeslaAPIState.cs
# ============================================================
Write-Host ""
ConvertLockToSemaphore `
    -FilePath "TeslaLogger\TeslaAPIState.cs" `
    -LockObject "TeslaAPIStateLock" `
    -FieldOld "private readonly object TeslaAPIStateLock = new object();" `
    -FieldNew "private readonly SemaphoreSlim TeslaAPIStateLock = new SemaphoreSlim(1, 1);" `
    -SemVarName "TeslaAPIStateLock"

# ============================================================
# File 2: FileManager.cs
# ============================================================
ConvertLockToSemaphore `
    -FilePath "TeslaLogger\\FileManager.cs" `
    -LockObject "SyncLock_WriteCurrentJsonFile" `
    -FieldOld "private static object SyncLock_WriteCurrentJsonFile = new object();" `
    -FieldNew "private static readonly SemaphoreSlim SyncLock_WriteCurrentJsonFile = new SemaphoreSlim(1, 1);" `
    -SemVarName "SyncLock_WriteCurrentJsonFile"

# ============================================================
# File 3: Car.cs
# ============================================================
ConvertLockToSemaphore `
    -FilePath "TeslaLogger\\Car.cs" `
    -LockObject "_syncRoot" `
    -FieldOld "private static object _syncRoot = new object();" `
    -FieldNew "private static readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);" `
    -SemVarName "_syncRoot"

# ============================================================
# File 4: UpdateTeslalogger.cs
# ============================================================
ConvertLockToSemaphore `
    -FilePath "TeslaLogger\\UpdateTeslalogger.cs" `
    -LockObject "lastTeslaLoggerVersionCheckObj" `
    -FieldOld "private static Object lastTeslaLoggerVersionCheckObj = new object();" `
    -FieldNew "private static readonly SemaphoreSlim lastTeslaLoggerVersionCheckObj = new SemaphoreSlim(1, 1);" `
    -SemVarName "lastTeslaLoggerVersionCheckObj"

# ============================================================
# File 5: TelemetryConnectionKafka.cs - typeof lock
# ============================================================
Write-Host ""
Write-Host "Processing: TelemetryConnectionKafka.cs (typeof lock - special handling)" -ForegroundColor Cyan

$content = Get-Content -Path "TeslaLogger\\TelemetryConnectionKafka.cs" -Raw -Encoding UTF8

# Add the semaphore field after other static fields
$content = $content -replace "(\s+static TelemetryConnectionKafka instance = null;)", "`$1`r`n        static readonly SemaphoreSlim kafkaLock = new SemaphoreSlim(1, 1);"

# Replace lock(typeof(...)) with kafkaLock.Wait(); try { ... finally { kafkaLock.Release(); }
$lines = $content.Split([char]10)
$result = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match "^\s+lock\s*\(\s*typeof\(TelemetryConnectionKafka\)\s*\)") {
        $indent = "            "
        # Find matching brace
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}kafkaLock.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { kafkaLock.Release(); }"
        $i = $lockEnd
    } else {
        $result += $line
    }
}
$content = ($result -join "`n")
Set-Content -Path "TeslaLogger\\TelemetryConnectionKafka.cs" -Value $content -Encoding UTF8
Write-Host "  Done." -ForegroundColor Green

# ============================================================
# File 6: TeslaAuth.cs - Random lock
# ============================================================
Write-Host ""
Write-Host "Processing: TeslaAuth.cs (Random lock - special handling)" -ForegroundColor Cyan

$content = Get-Content -Path "TeslaLogger\\TeslaAuth.cs" -Raw -Encoding UTF8

# Add randomLock field after Random declaration
$content = $content -replace "(\s+static readonly Random Random = new Random\(\);)", "`$1`r`n        static readonly SemaphoreSlim randomLock = new SemaphoreSlim(1, 1);"

# Replace lock (Random) with randomLock.Wait(); try { ... finally { randomLock.Release(); }
$lines = $content.Split([char]10)
$result = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match "^\s+lock\s*\(\s*Random\s*\)") {
        $indent = "                "
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}randomLock.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { randomLock.Release(); }"
        $i = $lockEnd
    } else {
        $result += $line
    }
}
$content = ($result -join "`n")
Set-Content -Path "TeslaLogger\\TeslaAuth.cs" -Value $content -Encoding UTF8
Write-Host "  Done." -ForegroundColor Green

# ============================================================
# File 7: MapQuestMapProvider.cs
# ============================================================
ConvertLockToSemaphore `
    -FilePath "TeslaLogger\\MapQuestMapProvider.cs" `
    -LockObject "_webClientLock" `
    -FieldOld "object _webClientLock = new object();" `
    -FieldNew "SemaphoreSlim _webClientLock = new SemaphoreSlim(1, 1);" `
    -SemVarName "_webClientLock"

# ============================================================
# File 8: Geofence.cs
# ============================================================
Write-Host ""
Write-Host "Processing: Geofence.cs (multiple locks - special handling)" -ForegroundColor Cyan

$content = Get-Content -Path "TeslaLogger\\Geofence.cs" -Raw -Encoding UTF8

# Replace lockObj field
$content = $content -replace "(\s+private static Object lockObj = new object\(\);)", "        private static readonly SemaphoreSlim lockObj = new SemaphoreSlim(1, 1);"

# Add new semaphore fields for geofence lists after the field declarations
$content = $content -replace "(\s+internal SortedSet<Address> geofenceList = new SortedSet<Address>\(new AddressByLatLng\(\)\);)", "`$1`r`n        private readonly SemaphoreSlim geofenceListLock = new SemaphoreSlim(1, 1);"

$content = $content -replace "(\s+internal SortedSet<Address> geofencePrivateList = new SortedSet<Address>\(new AddressByLatLng\(\)\);)", "`$1`r`n        private readonly SemaphoreSlim geofencePrivateListLock = new SemaphoreSlim(1, 1);"

# Convert lock statements
$lines = $content.Split([char]10)
$result = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    
    # lock (lockObj)
    if ($line -match "^\s+lock\s*\(\s*lockObj\s*\)") {
        $indent = "            "
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}lockObj.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { lockObj.Release(); }"
        $i = $lockEnd
        continue
    }
    
    # lock (geofencePrivateList)
    if ($line -match "^\s+lock\s*\(\s*geofencePrivateList\s*\)") {
        $indent = "            "
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}geofencePrivateListLock.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { geofencePrivateListLock.Release(); }"
        $i = $lockEnd
        continue
    }
    
    # lock (geofenceList)
    if ($line -match "^\s+lock\s*\(\s*geofenceList\s*\)") {
        $indent = "            "
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}geofenceListLock.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { geofenceListLock.Release(); }"
        $i = $lockEnd
        continue
    }
    
    $result += $line
}
$content = ($result -join "`n")
Set-Content -Path "TeslaLogger\\Geofence.cs" -Value $content -Encoding UTF8
Write-Host "  Done." -ForegroundColor Green

# ============================================================
# File 9: WebServer.cs
# ============================================================
Write-Host ""
Write-Host "Processing: WebServer.cs (Car.Allcars lock - special handling)" -ForegroundColor Cyan

$content = Get-Content -Path "TeslaLogger\\WebServer.cs" -Raw -Encoding UTF8

# Add carListLock field after first private static field
$content = $content -replace "(\s+private static HttpListener webServer;)", "`$1`r`n        static readonly SemaphoreSlim carListLock = new SemaphoreSlim(1, 1);"

# Replace lock (Car.Allcars) with carListLock.Wait(); try { ... finally { carListLock.Release(); }
$lines = $content.Split([char]10)
$result = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match "^\s+lock\s*\(\s*Car\.Allcars\s*\)") {
        $indent = "                "
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}carListLock.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { carListLock.Release(); }"
        $i = $lockEnd
    } else {
        $result += $line
    }
}
$content = ($result -join "`n")
Set-Content -Path "TeslaLogger\\WebServer.cs" -Value $content -Encoding UTF8
Write-Host "  Done." -ForegroundColor Green

# ============================================================
# File 10: WebHelper.cs - multiple lock types
# ============================================================
Write-Host ""
Write-Host "Processing: WebHelper.cs (multiple locks - special handling)" -ForegroundColor Cyan

$content = Get-Content -Path "TeslaLogger\\WebHelper.cs" -Raw -Encoding UTF8

# Replace httpClientLock field
$content = $content -replace "(\s+private static object httpClientLock = new object\(\);)", "        private static readonly SemaphoreSlim httpClientLock = new SemaphoreSlim(1, 1);"

# Add vehicles2AccountLock field after vehicles2Account declaration
$content = $content -replace "(\s+static Dictionary<string, Account> vehicles2Account = new Dictionary<string, Account>\(\);)", "`$1`r`n        private static readonly SemaphoreSlim vehicles2AccountLock = new SemaphoreSlim(1, 1);"

# Replace getAllVehiclesLock field declaration
$content = $content -replace "(\s+)object getAllVehiclesLock = new object\(\);", "`$1private readonly SemaphoreSlim getAllVehiclesLock = new SemaphoreSlim(1, 1);"

# Convert lock statements
$lines = $content.Split([char]10)
$result = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    
    # lock (httpClientLock) - use httpClientLock directly
    if ($line -match "^\s+lock\s*\(\s*httpClientLock\s*\)") {
        $indent = $matches[1]
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}httpClientLock.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { httpClientLock.Release(); }"
        $i = $lockEnd
        continue
    }
    
    # lock (getAllVehiclesLock) - use getAllVehiclesLock directly
    if ($line -match "^\s+lock\s*\(\s*getAllVehiclesLock\s*\)") {
        $indent = $matches[1]
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}getAllVehiclesLock.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { getAllVehiclesLock.Release(); }"
        $i = $lockEnd
        continue
    }
    
    # lock (vehicles2Account) - use vehicles2AccountLock
    if ($line -match "^\s+lock\s*\(\s*vehicles2Account\s*\)") {
        $indent = $matches[1]
        $braceCount = 1
        $lockEnd = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $braceCount += ([regex]::Matches($lines[$j], '\{')).Count - ([regex]::Matches($lines[$j], '\}')).Count
            if ($braceCount -le 0) { $lockEnd = $j; break }
        }
        $result += "${indent}vehicles2AccountLock.Wait(); try {"
        for ($j = $i + 1; $j -lt $lockEnd; $j++) { $result += $lines[$j] }
        $result += "${indent}finally { vehicles2AccountLock.Release(); }"
        $i = $lockEnd
        continue
    }
    
    $result += $line
}
$content = ($result -join "`n")
Set-Content -Path "TeslaLogger\\WebHelper.cs" -Value $content -Encoding UTF8
Write-Host "  Done." -ForegroundColor Green

# ============================================================
# Summary
# ============================================================
Write-Host ""
Write-Host "=== Conversion Complete ===" -ForegroundColor Green
Write-Host ""

# Verify no lock statements remain
$remainingLocks = @()
$filesToCheck = @(
    "TeslaLogger\\TeslaAPIState.cs",
    "TeslaLogger\\FileManager.cs",
    "TeslaLogger\\Car.cs",
    "TeslaLogger\\UpdateTeslalogger.cs",
    "TeslaLogger\\TelemetryConnectionKafka.cs",
    "TeslaLogger\\TeslaAuth.cs",
    "TeslaLogger\\MapQuestMapProvider.cs",
    "TeslaLogger\\Geofence.cs",
    "TeslaLogger\\WebServer.cs",
    "TeslaLogger\\WebHelper.cs"
)

foreach ($file in $filesToCheck) {
    $locks = Select-String -Path $file -Pattern "^\s+lock\s*\(" -ErrorAction SilentlyContinue
    if ($locks) {
        $remainingLocks += "$file: $($locks.Count) remaining lock statements"
    }
}

if ($remainingLocks.Count -gt 0) {
    Write-Host "WARNING: Some lock statements remain:" -ForegroundColor Yellow
    $remainingLocks | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
} else {
    Write-Host "SUCCESS: All lock statements have been converted!" -ForegroundColor Green
}
