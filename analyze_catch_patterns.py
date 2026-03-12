#!/usr/bin/env python3
"""
Analyze catch patterns in C# files to categorize by exception type
"""
import os
import re
from collections import defaultdict

def categorize_catch_context(text_before, text_inside):
    """Determine likely exception type based on context"""
    combined = (text_before + text_inside).lower()
    
    # HTTP/Network patterns
    if any(x in combined for x in ['http', 'request', 'response', 'client.download', 'getstring', 'postasync', 'getasync']):
        return 'HttpRequestException'
    
    # Database patterns
    if any(x in combined for x in ['mysql', 'sql', 'database', 'connection', 'mySqlCommand', 'execute', 'sqlclient']):
        return 'Exception'  # MySqlException for MySQL, but Exception is safer
    
    # Timeout patterns
    if 'timeout' in combined or 'timed out' in combined:
        return 'TimeoutException'
    
    # JSON patterns
    if any(x in combined for x in ['json', 'jsonconvert', 'deserialize', 'serialize']):
        return 'JsonException'
    
    # File I/O patterns
    if any(x in combined for x in ['file', 'directory', 'stream', 'io', 'path']):
        return 'IOException'
    
    # Thread abort
    if 'threadabort' in combined or 'thread.abort' in combined:
        return 'ThreadAbortException'
    
    # Network timeout
    if 'network' in combined or 'socket' in combined:
        return 'IOException'
    
    # Default - keep as Exception
    return 'Exception'

# Scan files
catch_patterns = defaultdict(list)
file_count = 0

for root, dirs, files in os.walk('/Users/lindner/VSCode/TeslaLogger/TeslaLogger'):
    for file in files:
        if file.endswith('.cs'):
            filepath = os.path.join(root, file)
            try:
                with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    
                # Find all catch (Exception) blocks
                pattern = r'catch\s*\(\s*Exception\s+(\w+)\s*\)'
                for match in re.finditer(pattern, content):
                    var_name = match.group(1)
                    start = max(0, match.start() - 500)
                    end = min(len(content), match.end() + 500)
                    context_before = content[start:match.start()]
                    context_after = content[match.end():end]
                    
                    category = categorize_catch_context(context_before, context_after)
                    
                    catch_patterns[category].append({
                        'file': os.path.basename(filepath),
                        'line': content[:match.start()].count('\n') + 1,
                        'var': var_name
                    })
                    file_count += 1
            except Exception as e:
                pass

# Print summary
print("Catch Pattern Analysis")
print("=" * 70)
print(f"Total catch (Exception) patterns: {file_count}\n")

for category in sorted(catch_patterns.keys(), key=lambda x: -len(catch_patterns[x])):
    count = len(catch_patterns[category])
    print(f"\n{category:30} {count:3} instances")
    
    # Show examples from different files
    files_seen = set()
    for entry in catch_patterns[category][:5]:
        if entry['file'] not in files_seen:
            print(f"  - {entry['file']:40} (line {entry['line']})")
            files_seen.add(entry['file'])
