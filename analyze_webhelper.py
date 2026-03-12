#!/usr/bin/env python3
"""
Analyze WebHelper.cs for string concatenation patterns
"""
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/WebHelper.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    lines = f.readlines()

concat_patterns = {}
total_concat = 0

for i, line in enumerate(lines, 1):
    if line.strip().startswith('//'):
        continue
    if any(x in line for x in ['MAX(', '+1', '+=', '+17']):
        continue
    
    # Pattern 1: "string" + var
    if match := re.search(r'"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*(?:;|$|\))', line):
        pattern = 'simple: "str" + var'
        concat_patterns[pattern] = concat_patterns.get(pattern, 0) + 1
        total_concat += 1
    
    # Pattern 2: var + "string"  
    elif match := re.search(r'([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\+\s*"([^"]+)"', line):
        pattern = 'simple: var + "str"'
        concat_patterns[pattern] = concat_patterns.get(pattern, 0) + 1
        total_concat += 1
    
    # Pattern 3: multiple concatenations
    elif re.search(r'"[^"]*"\s*\+\s*[a-zA-Z_][a-zA-Z0-9_.()]*\s*\+', line):
        pattern = 'multiple: "str" + var + ...'
        concat_patterns[pattern] = concat_patterns.get(pattern, 0) + 1
        total_concat += 1

print(f"WebHelper.cs Analysis:")
print(f"Total concatenations found: {total_concat}")
print("\nBreakdown by pattern:")
for pattern, count in sorted(concat_patterns.items(), key=lambda x: -x[1]):
    print(f"  {pattern:30} {count:3} instances")
