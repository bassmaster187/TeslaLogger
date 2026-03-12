#!/usr/bin/env python3
"""
Find all string concatenations in WebHelper.cs that haven't been converted yet
"""
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/WebHelper.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    lines = f.readlines()

patterns = {}
examples = {}

for i, line in enumerate(lines, 1):
    if line.strip().startswith('//') or 'using' in line:
        continue
    
    if ' + ' not in line or '"' not in line:
        continue
    
    # Skip arithmetic operations
    if any(x in line for x in ['MAX(', '+1', '+=', '+17', '+32', '*9/5', ') + ']):
        continue
    
    # Skip already interpolated strings
    if '$"' in line or '$@"' in line:
        continue
    
    # Find concatenation patterns
    if re.search(r'"[^"]*"\s*\+\s*[a-zA-Z_(]', line):
        # Extract a simplified pattern description
        if 'ex.' in line or 'Exception' in line:
            ptype = 'exception_msg'
        elif 'Logfile.Log' in line:
            ptype = 'logfile_pattern'
        elif 'return' in line:
            ptype = 'return_concat'
        elif '.ToString()' in line or '(int)' in line or '(double)' in line:
            ptype = 'cast_or_method'
        else:
            ptype = 'other'
        
        patterns[ptype] = patterns.get(ptype, 0) + 1
        if ptype not in examples:
            examples[ptype] = (i, line.strip()[:100])

print("Remaining String Concatenation Patterns in WebHelper.cs:")
print("=" * 60)
for ptype in sorted(patterns.keys(), key=lambda x: -patterns[x]):
    count = patterns[ptype]
    lineno, example = examples[ptype]
    print(f"\n{ptype:20} {count:3} instances")
    print(f"  Line {lineno}: {example}...")

print(f"\nTotal remaining: {sum(patterns.values())} instances")
