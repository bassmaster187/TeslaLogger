#!/usr/bin/env python3
"""
Convert WebHelper.cs string concatenations carefully - line by line
"""
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/WebHelper.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    lines = f.readlines()

modified_lines = []
changes = 0

for i, line in enumerate(lines):
    original = line
    
    # Skip lines that shouldn't be touched
    if line.strip().startswith('//') or 'throw' in line or 'return' in line.split(';')[0]:
        modified_lines.append(line)
        continue
    
    # Skip lines with arithmetic or SQL
    if any(x in line for x in ['MAX(', '+1', '+=', '+17', '+32', '*9/5']):
        modified_lines.append(line)
        continue
    
    # Pattern 1: Logfile.Log("text" + var)
    if 'Logfile.Log(' in line and ' + ' in line:
        new_line = re.sub(
            r'Logfile\.Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\)',
            lambda m: f'Logfile.Log($"{m.group(1)}{{{m.group(2)}}}")',
            line
        )
        if new_line != line:
            modified_lines.append(new_line)
            changes += 1
            continue
    
        # Try pattern 2: Logfile.Log("text" + var1 + "text2" + var2)
        new_line = re.sub(
            r'Logfile\.Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\+\s*"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\)',
            lambda m: f'Logfile.Log($"{m.group(1)}{{{m.group(2)}}}{m.group(3)}{{{m.group(4)}}}")',
            line
        )
        if new_line != line:
            modified_lines.append(new_line)    
            changes += 1
            continue
    
    # Pattern 3: Other method("text" + var)
    if 'Logfile.Log(' not in line and ' + ' in line and '(' in line and '"' in line:
        # Simple replacement for "text" + var patterns
        new_line = re.sub(
            r'"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\)',
            lambda m: f'$"{m.group(1)}{{{m.group(2)}}}")',
            line
        )
        if new_line != line:
            modified_lines.append(new_line)
            changes += 1
            continue
    
    modified_lines.append(line)

with open(filepath, 'w', encoding='utf-8') as f:
    f.writelines(modified_lines)

print(f"WebHelper.cs: Converted {changes} lines to string interpolation")
if changes > 50:
    print(f"✓ Large conversion ({changes} instances) - now verify build!")
