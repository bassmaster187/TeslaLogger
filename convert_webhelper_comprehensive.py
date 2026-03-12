#!/usr/bin/env python3
"""
Safe conversion of WebHelper.cs string concatenations
Use simple, tested patterns only
"""
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/WebHelper.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

original = content

# Simple Pattern 1: "text" + property
# Examples: "LastTaskerToken: " + reply, "Valid: " + x.ToString()
content = re.sub(
    r'"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*(?:\(\))?)',
    r'$"\1{\2}"',
    content
)

# Count successful conversions
changes = content.count('$"') - original.count('$"')

# Write back
with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"WebHelper.cs: Converted {changes} string concatenations to interpolation")
# Pattern 5: "text" + var simple
content = re.sub(
    r'"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\)',
    lambda m: f'$"{m.group(1)}{{{m.group(2)}}}")',
    content
)

# Count changes
changes = 0
for old_line, new_line in zip(original.split('\n'), content.split('\n')):
    if old_line != new_line:
        changes += 1

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"WebHelper.cs: Converted {changes} lines to string interpolation")
