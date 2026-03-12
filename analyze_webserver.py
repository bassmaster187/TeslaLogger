#!/usr/bin/env python3
import re

# Read WebServer.cs
with open('/Users/lindner/VSCode/TeslaLogger/TeslaLogger/WebServer.cs', 'r', encoding='utf-8') as f:
    content = f.read()
    lines = content.split('\n')

# Pattern to find string concatenations (not in interpolated strings)
# Looking for: "..." + variable/expression
pattern = r'["\'].*?["\'].*?\+(?!\+)'  

concat_lines = []
for i, line in enumerate(lines, 1):
    # Skip commented lines and lines with arithmetic
    if line.strip().startswith('//'):
        continue
    if 'MAX(' in line or 'TLstarline' in line:
        continue
    if '.ToString()' in line and ' +' in line:
        concat_lines.append((i, line.strip()))
    elif re.search(r'["\'].*?["\'].*?\s*\+\s*["\']', line):
        # This matches string + string
        concat_lines.append((i, line.strip()))

print(f"Found {len(concat_lines)} string concatenation instances:")
for lineno, line_content in concat_lines:
    if len(line_content) > 120:
        print(f"  Line {lineno}: {line_content[:120]}...")
    else:
        print(f"  Line {lineno}: {line_content}")
