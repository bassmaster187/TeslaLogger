#!/usr/bin/env python3
"""Convert Program.cs string concatenations to interpolation"""

import re

# Read file
with open('TeslaLogger/Program.cs', 'r') as f:
    content = f.read()

# Simple sed-like replacements for common patterns
replacements = [
    # Logfile.Log patterns
    (r'Logfile\.Log\("([^"]+)" \+ ([^;]+);', r'Logfile.Log($"$1 {$2};'),
    (r'Logfile\.Log\("([^"]+)" \+ (\w+\.GetFilePath.*?)\);', r'Logfile.Log($"$1 {$2}");'),
]

# Count concatenations
matches = re.findall(r'"\s*\+\s*', content)
print(f"Total concatenations found: {len(matches)}")

# Show sample patterns
lines = content.split('\n')
concat_lines = []
for i, line in enumerate(lines, 1):
    if '" + ' in line:
        concat_lines.append((i, line.strip()[:100]))

print(f"\nSample patterns (first 15):")
for line_num, content_preview in concat_lines[:15]:
    print(f"  Line {line_num}: {content_preview}")

print(f"\nShowing automated conversion capability - will perform manual replacements instead.")
