---
name: translations-sync
description: "Sync translation keys across all language-*.txt files in TeslaLogger/bin/. USE FOR: update translations, sync language files, translate new keys, update language-*.txt, translation sync, i18n update, language file changes. DO NOT USE FOR: adding new languages, changing language file format."
license: MIT
metadata:
  author: TeslaLogger
  version: "1.1.0"
---

# Sync TeslaLogger Translations

When translation keys change in one language file (usually `language-de.txt`), propagate the updates to all other `language-*.txt` files in `TeslaLogger/bin/`.

**Rule: every key gets a real translation in every one of the 15 language files.** Never leave a value empty, never copy the English or German text into another language as a placeholder, and never skip a file because "it falls back to English". See the *Translations* section in `AGENTS.md`.

## File Location

All language files live in: `TeslaLogger/bin/language-*.txt`

The English file (`language-en.txt`) is the source of truth for keys and default values.

## Workflow

### 1. Identify changed keys

Read the modified language file (e.g., `language-de.txt`) to find the changed keys. Note the key names and new values.

### 2. Find all language files

```
glob: TeslaLogger/bin/language-*.txt
```

### 3. Check which files already have the keys

```
grep: KEY_NAME (in TeslaLogger/bin/, include: language-*.txt)
```

### 4. Classify each language file

| Status | Action |
|--------|--------|
| Key exists with translation | Update the translation to match the new meaning |
| Key exists but empty (`KEY=`) | Add the translation |
| Key does not exist | **Add it** in the right section — do not skip, do not leave it to the English fallback |

### 5. Update files

For every file:
- Read the surrounding lines (context around the match / around the section of the using page, e.g. `#geoadd.php`)
- Insert or update the value with a proper translation in that file's language
- Preserve exact formatting: quotes, line breaks, special characters like `"_QQ_"`
- Keep the file encoding and line endings: these files are UTF-8 **with BOM** and CRLF. If you script the edit, re-add the BOM and the CRLF, otherwise the diff shows the whole file as changed

### Translation guidelines

- Translate the **meaning**, not literally
- Keep the same language style and conventions as the existing file
- Preserve placeholders: `{LINK}`, `{LINK1}`, `{LINK2}`, `{id}`, `"_QQ_"`, browser names in `{...}`
- When the German source changes (e.g. "vor 2021" → "MCU1"), adapt the translation to reflect the new meaning, not the old wording
- For technical terms like "MCU1", keep them as-is (don't translate "MCU1")

### 6. Verify and report

Verify that all 15 files now contain the key and that no value is a copy of the English or German text:

```
grep: ^KEY_NAME= (in TeslaLogger/bin/, include: language-*.txt)   ->  15 hits expected
```

Return a table showing:

| File | BA_ALLCARS | BA_MODELSXOLD |
|------|-----------|---------------|
| language-de.txt | Changed by user | Changed by user |
| language-it.txt | Updated | Updated |
| language-es.txt | Updated | Updated |
| language-sv.txt | Added (was empty) | Added (was empty) |
| language-da.txt | Added (key was missing) | Added (key was missing) |
| ... | ... | ... |

## Example

User changes in `language-de.txt`:
```
BA_ALLCARS="Alle Fahrzeuge außer Model S/X mit MCU1"
BA_MODELSXOLD="Model S/X mit MCU1"
```

Update `language-es.txt` (old: "construido antes del 2021" → new: "con MCU1"):
```
BA_ALLCARS="Todos los vehículos excepto Model S/X con MCU1"
BA_MODELSXOLD="Model S/X con MCU1"
```

If `language-da.txt` has no `BA_ALLCARS` key yet, add the Danish translation to it — do not rely on the English fallback.
