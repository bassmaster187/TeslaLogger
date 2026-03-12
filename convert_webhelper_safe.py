#!/usr/bin/env python3
"""
Convert WebHelper.cs - ONLY simple Logfile.Log patterns
Using extreme caution to avoid broken syntax
"""
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/WebHelper.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

original = content

# ONLY convert simple, obvious patterns:
# Logfile.Log("text: " + ex.Message)
# Logfile.Log("text: " + variable)
# Do NOT try to handle complex cases

# Pattern 1: Logfile.Log("text" + ex.Message) or ex.ToString()
content = re.sub(
    r'Logfile\.Log\("([^"]+)"\s*\+\s*(ex\.(?:Message|ToString\(\)))\s*\)',
    lambda m: f'Logfile.Log($"{m.group(1)}{{{m.group(2)}}}")',
    content
)

# Pattern 2: Logfile.Log("text" + simpleVar)
# where simpleVar is a single identifier (no dots, no parens)
content = re.sub(
    r'Logfile\.Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9]*)\s*\)',
    lambda m: f'Logfile.Log($"{m.group(1)}{{{m.group(2)}}}")',
    content
)

# Pattern 3: Logfile.Log("text" + propertyAccess) where propertyAccess has dots
# But NOT if they have method calls or casts
content = re.sub(
    r'Logfile\.Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*)\s*\)',
    lambda m: f'Logfile.Log($"{m.group(1)}{{{m.group(2)}}}")',
    content
)

# Pattern 4: Logfile.Log("text1" + var + "text2" + var2) - but ONLY if simple
# Like: "Car with VIN: " + car.Vin + " not found! Display Name: " + car.DisplayName
content = re.sub(
    r'Logfile\.Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*)\s*\+\s*"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*)\s*\)',
    lambda m: f'Logfile.Log($"{m.group(1)}{{{m.group(2)}}}{m.group(3)}{{{m.group(4)}}}")',
    content
)

# Count changes
changes = 0
for old_line, new_line in zip(original.split('\n'), content.split('\n')):
    if old_line != new_line:
        changes += 1

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"WebHelper.cs: Converted {changes} lines (simple patterns only)")
