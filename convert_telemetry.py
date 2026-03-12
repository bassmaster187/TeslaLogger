#!/usr/bin/env python3
"""
Convert string concatenations to string interpolation in TelemetryParser.cs
Patterns to convert:
- Log("string" + var) -> Log($"string {var}")
- Log("string" + var + "string2") -> Log($"string {var} string2")
- Log(ex.ToString() + "\n" + content) -> Log($"{ex}\n{content}")
"""
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/TelemetryParser.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# Pattern 1: Simple Log calls with string + variable
# Log("text: " + var)
pattern1 = r'Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\)'
def replace1(match):
    text, var = match.groups()
    return f'Log($"{text}{{{var}}}")'

# Pattern 2: Chained concatenations - need recursive/careful handling
# For now, let's do simpler cases first and handle complex ones manually

# Simple replacements for known patterns from the analysis
replacements = [
    # Line 89: Log("Driving after last charge: " + drivenAfterLastCharge + " km -> charge_energy_added = 0");
    (r'Log\("Driving after last charge: "\s*\+\s*drivenAfterLastCharge\s*\+\s*" km -> charge_energy_added = 0"\)',
     'Log($"Driving after last charge: {drivenAfterLastCharge} km -> charge_energy_added = 0")'),
    
    # Line 94: Log("charge_energy_added from DB: " + charge_energy_added);
    (r'Log\("charge_energy_added from DB: "\s*\+\s*charge_energy_added\)',
     'Log($"charge_energy_added from DB: {charge_energy_added}")'),
    
    # Line 134: Log("Parking time: " + lastDriving.ToString());
    (r'Log\("Parking time: "\s*\+\s*lastDriving\.ToString\(\)\)',
     'Log($"Parking time: {lastDriving}")'),
    
    # Line 162: Log("ACCharging = " + value);
    (r'Log\("ACCharging = "\s*\+\s*value\)',
     'Log($"ACCharging = {value}")'),
]

count = 0
for pattern, replacement in replacements:
    new_content = re.sub(pattern, replacement, content)
    if new_content != content:
        count += 1
        content = new_content
        print(f"✓ Applied replacement pattern {count}")

# General pattern for Log("string" + var)
original_len = len(re.findall(r'Log\("([^"]+)"\s*\+', content))
content = re.sub(r'Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\s*\)', 
                 r'Log($"\1{\2}")', content)
new_len = len(re.findall(r'Log\("([^"]+)"\s*\+', content))
if original_len > new_len:
    print(f"✓ Applied {original_len - new_len} simple pattern conversions")

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"\nConversion complete!")
