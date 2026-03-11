#!/usr/bin/env python3
"""Analyze string concatenation patterns in Tools.cs for Phase 8 conversion"""

import re

# Read the file
with open('TeslaLogger/Tools.cs', 'r') as f:
    lines = f.readlines()

# Find all concatenation patterns
patterns = []
for i, line in enumerate(lines, 1):
    if '" + ' in line:
        # Clean line for display
        display = line.strip()[:100]
        
        # Categorize
        if 'Logfile.Log' in line:
            cat = 'Logfile.Log'
        elif 'DebugLog' in line:
            cat = 'DebugLog'
        elif 'return' in line:
            cat = 'Return'
        elif 'string' in line or 'msg' in line or 'temp' in line or 'exmsg' in line:
            cat = 'Variable'
        else:
            cat = 'Other'
        
        patterns.append({
            'line': i,
            'category': cat,
            'display': display,
            'full_line': line.rstrip()
        })

# Print summary
print("=== Tools.cs String Concatenation Analysis ===\n")
print(f"Total concatenations: {len(patterns)}\n")

# Group by category
from collections import defaultdict
by_cat = defaultdict(list)
for p in patterns:
    by_cat[p['category']].append(p)

print("Breakdown by category:")
for cat in sorted(by_cat.keys()):
    items = by_cat[cat]
    print(f"  {cat}: {len(items)}")

print("\n=== Detailed Breakdown ===\n")
for cat in sorted(by_cat.keys()):
    items = by_cat[cat]
    print(f"\n{cat} ({len(items)} instances):")
    for p in items[:5]:
        print(f"  Line {p['line']}: {p['display']}")
    if len(items) > 5:
        print(f"  ... and {len(items) - 5} more")
