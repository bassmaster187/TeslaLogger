# Convert lock(obj) statements in .cs files to SemaphoreSlim

$teslaLoggerDir = Join-Path $PSScriptRoot "TeslaLogger"
$files = Get-ChildItem -Path $teslaLoggerDir -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\bin\\" }

Write-Host "Found $($files.Count) .cs files to process`n"

function ConvertLockToSemaphoreSlim {
    param($file)
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # === Special lock handling (add semaphore declarations) ===

    # TelemetryConnectionKafka.cs - lock(typeof(TelemetryConnectionKafka)) -> kafkaLock
    if ($file.Name -eq "TelemetryConnectionKafka.cs" -and $content -match "lock\s*\(\s*typeof\(TelemetryConnectionKafka\)") {
        Write-Host "  Converting: lock(typeof(TelemetryConnectionKafka)) -> kafkaLock"
        $classMatch = [regex]::Match($content, "(\s*internal class TelemetryConnectionKafka : TelemetryConnection\s*\{)")
        if ($classMatch.Success) {
            $content = $content.Insert($classMatch.Index + $classMatch.Length, "`n`n`tstatic readonly SemaphoreSlim kafkaLock = new SemaphoreSlim(1, 1);")
        }
        $content = [regex]::Replace($content, "lock\s*\(\s*typeof\(TelemetryConnectionKafka\)\)", "kafkaLock.Wait()", [System.Text.RegularExpressions.RegexOptions]::Multiline)
    }

    # TeslaAuth.cs - lock(Random) -> randomLock
    if ($file.Name -eq "TeslaAuth.cs" -and $content -match "lock\s*\(\s*Random\s*\)") {
        Write-Host "  Converting: lock(Random) -> randomLock"
        $content = $content -replace "(static readonly Random Random = new Random\(\);)", "`$1`n`n`tstatic readonly SemaphoreSlim randomLock = new SemaphoreSlim(1, 1);"
        $content = [regex]::Replace($content, "lock\s*\(\s*Random\s*\)", "randomLock.Wait()", [System.Text.RegularExpressions.RegexOptions]::Multiline)
    }

    # WebServer.cs - lock(Car.Allcars) -> carListLock
    if ($file.Name -eq "WebServer.cs" -and $content -match "lock\s*\(\s*Car\.Allcars\s*\)") {
        Write-Host "  Converting: lock(Car.Allcars) -> carListLock"
        $content = $content -replace "(namespace TeslaLoggerNET8\s*\{)\s*\n\s*(public class WebServer)", "`$1`n`n`n`n`tstatic readonly SemaphoreSlim carListLock = new SemaphoreSlim(1, 1);`n`2"
        $content = [regex]::Replace($content, "lock\s*\(\s*Car\.Allcars\s*\)", "carListLock.Wait()", [System.Text.RegularExpressions.RegexOptions]::Multiline)
    }

    # Geofence.cs - geofenceList and geofencePrivateList
    if ($file.Name -eq "Geofence.cs" -and ($content -match "lock\s*\(\s*geofencePrivateList\s*\)" -or $content -match "lock\s*\(\s*geofenceList\s*\)")) {
        Write-Host "  Converting: geofence locks -> geofenceListLock / geofencePrivateListLock"
        $content = $content -replace "(internal SortedSet<Address> geofenceList = new SortedSet<Address>\(new AddressByLatLng\(\);)", "`$1`n`n`tstatic readonly SemaphoreSlim geofenceListLock = new SemaphoreSlim(1, 1);"
        $content = $content -replace "(internal SortedSet<Address> geofencePrivateList = new SortedSet<Address>\(new AddressByLatLng\(\);)", "`$1`n`n`tstatic readonly SemaphoreSlim geofencePrivateListLock = new SemaphoreSlim(1, 1);"
        $content = [regex]::Replace($content, "lock\s*\(\s*geofencePrivateList\s*\)", "geofencePrivateListLock.Wait()", [System.Text.RegularExpressions.RegexOptions]::Multiline)
        $content = [regex]::Replace($content, "lock\s*\(\s*geofenceList\s*\)", "geofenceListLock.Wait()", [System.Text.RegularExpressions.RegexOptions]::Multiline)
    }

    # === Convert object declarations to SemaphoreSlim ===
    $content = $content -replace "private static object (\w+) = new object\(\);", "private static readonly SemaphoreSlim `$1 = new SemaphoreSlim(1, 1);"
    $content = $content -replace "private static Object (\w+) = new object\(\);", "private static readonly SemaphoreSlim `$1 = new SemaphoreSlim(1, 1);"
    $content = $content -replace "private readonly object (\w+) = new object\(\);", "private readonly SemaphoreSlim `$1 = new SemaphoreSlim(1, 1);"
    $content = $content -replace "object (\w+) = new object\(\);", "SemaphoreSlim `$1 = new SemaphoreSlim(1, 1);"

    # === Convert lock blocks ===
    # Pass 1: Replace lock (X) with X.Wait(); using multiline mode
    $lockPattern = "^\s+lock\s*\((\w+)\)\s*$"
    $content = [regex]::Replace($content, $lockPattern, "`$1.Wait();", [System.Text.RegularExpressions.RegexOptions]::Multiline)

    # Pass 2: Convert { to try where preceded by X.Wait(); and add finally blocks
    $lines = $content -split "`r?`n"
    $newLines = @()
    $i = 0

    # Stack for tracking nested try blocks: each element is [lockObj, braceDepth]
    $tryStack = @()

    while ($i -lt $lines.Length) {
        $line = $lines[$i]

        # Check if we should start a new try block
        $startTry = $false
        $lockObj = ""
        if ($newLines.Count -gt 0) {
            $prevLine = $newLines[$newLines.Count - 1]
            if ($prevLine -match "^\s+(\w+)\.Wait\(\);") {
                $lockObj = $Matches[1].Value
                if ($line.Trim() -eq '{') {
                    $startTry = $true
                }
            }
        }

        if ($startTry) {
            $newLines += "try"
            $tryStack += [PSCustomObject]@{ LockObj = $lockObj; BraceDepth = 1 }
            $i++
            continue
        }

        # Check if we're inside a try block
        if ($tryStack.Count -gt 0) {
            $topTry = $tryStack[$tryStack.Count - 1]
            $openCount = ([regex]::Matches($line, '\{').Count)
            $closeCount = ([regex]::Matches($line, '\}').Count)

            if ($closeCount -gt 0 -and ($topTry.BraceDepth - $closeCount) -le 0) {
                # Found the matching closing brace
                $depth = 0
                $closePos = -1
                for ($j = 0; $j -lt $line.Length; $j++) {
                    if ($line[$j] -eq '{') { $depth++ }
                    if ($line[$j] -eq '}') { $depth-- }
                    if ($depth -le 0) { $closePos = $j; break }
                }

                if ($closePos -ge 0) {
                    $afterBrace = if ($closePos + 1 -lt $line.Length) { $line.Substring($closePos + 1) } else { "" }
                    $closingIndent = $line.Substring(0, $line.Length - $line.TrimStart().Length)
                    $newLines += "$topTry.LockObj finally { $topTry.LockObj.Release(); }$closingIndent}"
                    if ($afterBrace) {
                        $newLines += $afterBrace
                    }
                } else {
                    $newLines += "finally { $topTry.LockObj.Release(); }`n$indent}"
                }

                $tryStack = $tryStack[0..($tryStack.Count - 2)]
                $i++
                continue
            }

            # Check for nested try blocks inside try
            if ($line -match "^\s+(\w+)\.Wait\(\);") {
                $nestedLockObj = $Matches[1].Value
                if ($i + 1 -lt $lines.Length -and $lines[$i + 1].Trim() -eq '{') {
                    $newLines += $line
                    $newLines += "try"
                    $tryStack += [PSCustomObject]@{ LockObj = $nestedLockObj; BraceDepth = 1 }
                    $i++
                    continue
                }
            }

            $newLines += $line
            $tryStack[$tryStack.Count - 1].BraceDepth += ($openCount - $closeCount)
            $i++
            continue
        }

        # Normal line processing
        $newLines += $line
        $i++
    }

    $newContent = ($newLines -join "`n")

    # Check results
    if ($newContent -match "lock\s*\(\s*typeof\(.*\)\s*\)" -or ($newContent -match "lock\s*\(\s*Random\s*\)" -and $file.Name -eq "TeslaAuth.cs")) {
        Write-Host "  WARNING: Special lock statements still present" -ForegroundColor Yellow
    } elseif ($newContent -match "lock\s*\(\w+\)" -and $newContent -notmatch "lock\s*\(\s*typeof") {
        Write-Host "  WARNING: Lock statements still present" -ForegroundColor Yellow
        $contentMatches = [regex]::Matches($newContent, "lock\s*\(\s*(\w+)\s*\)")
        foreach ($m in $contentMatches) {
            Write-Host "    - $($m.Value)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  OK: All lock statements converted" -ForegroundColor Green
    }

    # Write back only if content changed
    if ($newContent -ne $originalContent) {
        $newContent | Set-Content -Path $file.FullName -Encoding UTF8
    }
}

# Process each file
foreach ($file in $files) {
    Write-Host "Processing: $($file.Name)"
    ConvertLockToSemaphoreSlim $file
}

# === Final Summary ===
Write-Host "`n=== Final Summary ==="
$remainingFiles = @()
foreach ($file in (Get-ChildItem -Path $teslaLoggerDir -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\bin\\" })) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "lock\s*\(\s*(\w+)\s*\)") {
        $lockObj = ([regex]::Match($content, "lock\s*\(\s*(\w+)\s*\)")).Groups[1].Value
        if ($lockObj -ne "FlushFinalBlock" -and $lockObj -ne "Lock" -and $lockObj -ne "Unlock") {
            $remainingFiles += [PSCustomObject]@{ File = $file.Name; Lock = $lockObj }
        }
    }
}

if ($remainingFiles.Count -eq 0) {
    Write-Host "`nSUCCESS: All lock statements have been converted!" -ForegroundColor Green
} else {
    Write-Host "`nFILES WITH REMAINING LOCK STATEMENTS:" -ForegroundColor Red
    foreach ($item in $remainingFiles) {
        Write-Host "  $($item.File) - lock($($item.Lock))" -ForegroundColor Red
    }
}
