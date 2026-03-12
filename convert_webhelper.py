#!/usr/bin/env python3
"""
Convert all string concatenations in WebHelper.cs to string interpolation
"""
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/WebHelper.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

original_content = content

# Pattern 1: Simple "string" + variable 
# Match: "text" + var) or "text" + var,
def convert_pattern1(text):
    # "string" + var[.method()] at end of expression
    text = re.sub(
        r'"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*(?=[\),;])',
        lambda m: f'$"{m.group(1)}{{{m.group(2)}}}"',
        text
    )
    return text

# Pattern 2: variable + "string" 
def convert_pattern2(text):
    # var + "string" - need to create interpolated string
    text = re.sub(
        r'([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\+\s*"([^"]+)"(?=[\),;])',
        lambda m: f'$"{{{m.group(1)}}}{m.group(2)}"',
        text
    )
    return text

# Pattern 3: "str" + var + "str" chains
def convert_pattern3(text):
    # "str1" + var1 + "str2" [+ var2...]
    text = re.sub(
        r'"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\+\s*"([^"]+)"',
        lambda m: f'$"{m.group(1)}{{{m.group(2)}}}{m.group(3)}"',
        text
    )
    return text

# Apply conversions - order matters to avoid conflicts
content = convert_pattern3(content)  # Complex patterns first
content = convert_pattern1(content)  # Simple patterns
content = convert_pattern2(content)  # Reverse patterns

# If we still have unclosed interpolations, wrap them
content = re.sub(r'\$"([^"]*)"', lambda m: f'$"{m.group(1)}"', content)

# Count changes
original_lines = original_content.split('\n')
new_lines = content.split('\n')
changes = sum(1 for a, b in zip(original_lines, new_lines) if a != b)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"WebHelper.cs: Converted {changes} lines with string interpolation")
if changes > 100:
    print(f"This was a large conversion - estimated ~{changes} instances modernized!")
