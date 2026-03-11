#!/usr/bin/env python3
"""Convert string concatenations to string interpolation in Tools.cs"""

import re
import sys

# Read the file
with open('TeslaLogger/Tools.cs', 'r') as f:
    content = f.read()
    lines = content.split('\n')

# Conversion tracker
conversions = []
errors = []

# Process each line
new_lines = []
for i, line in enumerate(lines, 1):
    original = line
    
    if '" + ' not in line:
        new_lines.append(line)
        continue
    
    # Pattern 1: Simple Logfile.Log("literal " + var)
    # Logfile.Log("Copy '" + file.FullName + "' to '" + p + "'");
    if 'Logfile.Log(' in line and 'ToString()' not in line:
        # Parse the Logfile.Log call
        match = re.search(r'Logfile\.Log\((.*)\);?\s*$', line)
        if match:
            args = match.group(1)
            # Try to convert simple concatenations to interpolation
            # This is complex - we need to parse " + " separated parts
            
            # Simple approach: look for patterns like ("text " + var + " text")
            # and convert to $"text {var} text"
            
            # Check if this is a simple convertible pattern
            # Count the depth of concatenations
            parts = args.split('" + "')
            if len(parts) > 1:
                # We have multiple string parts - this can be converted
                new_arg = convert_concat_to_interp(args)
                new_line = line[:match.start(1)] + new_arg + line[match.end(1):]
                new_lines.append(new_line)
                conversions.append((i, 'Logfile.Log', original[:50], new_line[:50]))
                continue
    
    # Pattern 2: DebugLog that already has $ but has + concatenation
    if 'DebugLog($' in line and '" + ' in line:
        # This is already partially modernized, convert the rest
        new_line = line  # Keep as-is for now, already has interpolation
        new_lines.append(new_line)
        continue
    
    # Pattern 3: Return statements
    if 'return' in line and 'DateTime.Now.ToString' in line:
        # This has format specifier - keep for manual review
        new_lines.append(line)
        continue
    
    # Default: keep as-is
    new_lines.append(line)

def convert_concat_to_interp(concat_str):
    """Convert concatenation to string interpolation"""
    # This is complex - for now, return as-is for manual handling
    return concat_str

# Print summary
print(f"Analyzed {len(lines)} lines")
print(f"Found {len(conversions)} conversions")
print(f"Errors: {len(errors)}")

for line_num, cat, orig, new in conversions[:5]:
    print(f"Line {line_num} ({cat}): {orig} -> {new}")

if len(conversions) > 5:
    print(f"... and {len(conversions) - 5} more")

# Write back
if len(conversions) > 0:
    with open('TeslaLogger/Tools.cs', 'w') as f:
        f.write('\n'.join(new_lines))
    print(f"\n✅ Wrote {len(new_lines)} lines back to file")
else:
    print("\n⚠️ No conversions made - using manual sed approach instead")
