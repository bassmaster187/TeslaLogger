#!/usr/bin/env python3
"""
Carefully convert MQTT.cs string concatenations to interpolation
Using targeted replacements for specific patterns
"""
import re

filepath = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger/MQTT.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

original = content

# Pattern 1: Fix interpolated URLs that were partially concatenated
# $"http://.../" + carId) → $"http://.../{carId}"
content = re.sub(
    r'\$"(http://[^"]*)/"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*)\)',
    lambda m: f'$"{m.group(1)}/{{{m.group(2)}}}")',
    content
)

# Pattern 2: Simple "text" + ex.Message/Property pattern
content = re.sub(
    r'"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*(?:\.[a-zA-Z_][a-zA-Z0-9_.]*)*)\)(?=[,;])',
    lambda m: f'$"{m.group(1)}{{{m.group(2)}}}")',
    content
)

# Pattern 3: carTopic + "/" + keyvalue.Key pattern
content = re.sub(
    r'([a-zA-Z_][a-zA-Z0-9_.]*)\s*\+\s*"/"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)',
    lambda m: f'${{{m.group(1)}}}/{{{m.group(2)}}}',
    content
)

# Pattern 4: Complex multi-part like: "text: " + host + ":" + port + " id: " + id
# Split by + and rebuild as interpolation
lines = content.split('\n')
new_lines = []
for line in lines:
    # Check if this is the connecting log line
    if 'Connecting with' in line and ' + ' in line:
        # Pattern: Logfile.Log("MQTT: Connecting ... " + host + ":" + port + " with ClientID: " + newClientId);
        match = re.search(
            r'Logfile\.Log\("([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*)\s*\+\s*"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.]*)\s*\+\s*"([^"]+)"\s*\+\s*([a-zA-Z_][a-zA-Z0-9_.()]*)\)',
            line
        )
        if match:
            new_line = f'Logfile.Log($"{match.group(1)}{{{match.group(2)}}}{match.group(3)}{{{match.group(4)}}}{match.group(5)}{{{match.group(6)}}}");'
            new_lines.append(new_line)
            continue
    
    new_lines.append(line)

content = '\n'.join(new_lines)

# Count changes
changes = sum(1 for a, b in zip(original.split('\n'), content.split('\n')) if a != b)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"MQTT.cs: Updated {changes} lines with string interpolation")
