#!/usr/bin/env python3
import re
import os

codebase_path = '/Users/lindner/VSCode/TeslaLogger/TeslaLogger'

# Files to check
files_to_check = [
    'UpdateTeslalogger.cs',
    'Car.cs', 
    'WebHelper.cs',
    'WebServer.cs',
    'Tools.cs',
    'MQTT.cs'
]

total_concatenations = 0
file_results = {}

for filename in files_to_check:
    filepath = os.path.join(codebase_path, filename)
    if not os.path.exists(filepath):
        continue
    
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Count patterns like: "string" + var or var + "string"
        # Skip comments and arithmetic
        lines = content.split('\n')
        concat_count = 0
        
        for line in lines:
            if line.strip().startswith('//'):
                continue
            if any(x in line for x in ['MAX(', '+1', '+17', '+= ', 'ToString()']):
                continue
            # Pattern: "string" + identifier or identifier + "string"
            if re.search(r'"\s*\+\s*[a-zA-Z_]', line) or re.search(r'[a-zA-Z_0-9)\]]\s*\+\s*"', line):
                # But skip arithmetic operations
                if not re.search(r'\d\s*\+\s*\d', line) and not re.search(r'\)\s*\+\s*\d', line):
                    concat_count += 1
        
        if concat_count > 0:
            file_results[filename] = concat_count
            total_concatenations += concat_count
    except Exception as e:
        print(f"Error processing {filename}: {e}")

print("=== Remaining String Concatenations in Key Files ===\n")
for filename in sorted(file_results.keys(), key=lambda x: file_results[x], reverse=True):
    count = file_results[filename]
    print(f"{filename:30} {count:3} instances")

print(f"\nTotal remaining: {total_concatenations} instances")
