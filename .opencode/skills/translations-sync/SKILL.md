---
name: translations-sync
description: "Sync translation keys across all language-*.txt files in TeslaLogger/bin/. USE FOR: update translations, sync language files, translate new keys, update language-*.txt, translation sync, i18n update, language file changes. DO NOT USE FOR: adding new languages, changing language file format."
license: MIT
metadata:
  author: TeslaLogger
  version: "1.0.0"
---

# Sync TeslaLogger Translations

When translation keys change in one language file (usually `language-de.txt`), propagate the updates to all other `language-*.txt` files in `TeslaLogger/bin/`.

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
| Key does not exist | Skip — file will fall back to English automatically |

### 5. Update files

For each file that has the key:
- Read the surrounding lines (context around the match)
- Update the value, adapting the translation to the new meaning while keeping the same language
- Preserve exact formatting: quotes, line breaks, special characters like `"_QQ_"`

### Translation guidelines

- Translate the **meaning**, not literally
- Keep the same language style and conventions as the existing file
- Preserve placeholders: `{LINK}`, `{LINK1}`, `{LINK2}`, `{id}`, `"_QQ_"`, browser names in `{...}`
- When the German source changes (e.g. "vor 2021" → "MCU1"), adapt the translation to reflect the new meaning, not the old wording
- For technical terms like "MCU1", keep them as-is (don't translate "MCU1")

### 6. Report summary

Return a table showing:

| File | BA_ALLCARS | BA_MODELSXOLD |
|------|-----------|---------------|
| language-de.txt | Changed by user | Changed by user |
| language-it.txt | Updated | Updated |
| language-es.txt | Updated | Updated |
| language-sv.txt | Added (was empty) | Added (was empty) |
| language-da.txt | Falls back to EN | Falls back to EN |
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

Skip `language-da.txt` if it has no `BA_ALLCARS` key — it will automatically use the English fallback.
