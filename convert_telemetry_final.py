#!/usr/bin/env python3
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/TelemetryParser.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Store original for comparison
original_content = content

# Pattern 1: Simple case - Log("text" + var)
content = re.sub(
    r'Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*(?:\(\))?)\s*\)',
    lambda m: f'Log($"{m.group(1)}{{{m.group(2)}}}")',
    content
)

# Pattern 2: Two concatenations - Log("text" + var + "text2")
content = re.sub(
    r'Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*(?:\(\))?)\s*\+\s*"([^"]+)"\s*\)',
    lambda m: f'Log( $"{m.group(1)}{{{m.group(2)}}}{m.group(3)}")',
    content
)

# Pattern 3: car.Log with simple case
content = re.sub(
    r'car\.Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*(?:\(\))?)\s*\)',
    lambda m: f'car.Log($"{m.group(1)}{{{m.group(2)}}}")',
    content
)

# Pattern 4: Three concatenations - Log("text" + var1 + "text2" + var2)
content = re.sub(
    r'Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*)\s*\+\s*"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*)\s*\)',
    lambda m: f'Log($"{m.group(1)}{{{m.group(2)}}}{m.group(3)}{{{m.group(4)}}}")',
    content
)

# Pattern 5: Ternary operator case - Log("text" + (cond ? "a" : "b"))
content = re.sub(
    r'Log\("([^"]+)"\s*\+\s*\(([^\)]+)\s*\?\s*"([^"]+)"\s*:\s*"([^"]+)"\s*\)\s*\)',
    lambda m: f'Log($"{m.group(1)}{{{m.group(2)}}}")',
    content
)

# Count changes
import difflib
original_lines = original_content.split('\n')
new_lines = content.split('\n')
changes = sum(1 for a, b in zip(original_lines, new_lines) if a != b)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"Successfully converted {changes} lines in TelemetryParser.cs")
