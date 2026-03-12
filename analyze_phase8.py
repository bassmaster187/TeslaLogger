#!/usr/bin/env python3
import re

files = [
    '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/DBHelper.cs',
    '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/TelemetryParser.cs',
    '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/UpdateTeslalogger.cs',
    '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/Car.cs'
]

for filepath in files:
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        concat_count = 0
        concat_lines = []
        
        for i, line in enumerate(lines, 1):
            # Skip comments
            if line.strip().startswith('//'):
                continue
            # Skip arithmetic/SQL
            if any(x in line for x in ['MAX(', 'TLstartline', '+1', '+17']):
                continue
            # Look for string concatenation patterns
            # Pattern 1: "string" + variable or method
            if re.search(r'"\s*\+\s*[a-zA-Z_]', line):
                concat_count += 1
                concat_lines.append((i, line.strip()[:90]))
            # Pattern 2: variable/method + "string"
            elif re.search(r'[a-zA-Z_0-9)\]]\s*\+\s*"', line):
                concat_count += 1
                concat_lines.append((i, line.strip()[:90]))
        
        if concat_count > 0:
            filename = filepath.split('/')[-1]
            print(f"\n{filename}: {concat_count} instances")
            for lineno, line_text in concat_lines[:10]:  # Show first 10
                print(f"  Line {lineno}: {line_text}...")
            if len(concat_lines) > 10:
                print(f"  ... and {len(concat_lines)-10} more")
    except Exception as e:
        print(f"Error processing {filepath}: {e}")

print("\nDone!")
