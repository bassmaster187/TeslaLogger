# Test multiline regex replacement
$content = Get-Content 'TeslaLogger/Car.cs' -Raw
Write-Host "Original line 239:"
$lines = $content -split "`n"
Write-Host $lines[238]

# Without multiline mode
$pattern1 = "^\s+lock\s*\((\w+)\)\s*$"
$result1 = $content -replace $pattern1, "`$1.Wait();"
$resultLines1 = $result1 -split "`n"
Write-Host "Without multiline - line 239:"
Write-Host $resultLines1[238]

# With multiline mode using [regex]::Replace
$result2 = [regex]::Replace($content, $pattern1, "`$1.Wait();", [System.Text.RegularExpressions.RegexOptions]::Multiline)
$resultLines2 = $result2 -split "`n"
Write-Host "With multiline - line 239:"
Write-Host $resultLines2[238]

# Count matches with multiline
$matches1 = [regex]::Matches($content, $pattern1).Count
$matches2 = [regex]::Matches($content, $pattern1, [System.Text.RegularExpressions.RegexOptions]::Multiline).Count
Write-Host "`nMatches without multiline: $matches1"
Write-Host "Matches with multiline: $matches2"
