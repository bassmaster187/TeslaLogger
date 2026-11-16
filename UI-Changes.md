# UI-Changes – Fahrzeug-Cockpit (`/admin/index.php`)

Stand: 16.11.2026

## Ziel

Modernisierung der Teslalogger-Startseite (`TeslaLogger/www/admin/index.php`): weg vom Tabellen-Daten-Dump
im Stil einer WordPress-Theme-Kopie von 2014, hin zu einem fokussierten Fahrzeug-Cockpit.
Primäre Frage des Nutzers – *„Wie geht es meinem Auto gerade?"* – soll in wenigen Sekunden beantwortet sein.

Randbedingungen:

- Alle bestehenden Datenquellen und Endpunkte bleiben unverändert (`current_json.php`, `info.php`,
  `subscription-check.php`).
- Element-IDs bleiben erhalten, damit `info.php` und die Inline-Logik weiter funktionieren.
- **Polling bleibt bei 250 ms** (nur bei `document.hasFocus()`), wie ausdrücklich gefordert.
- Andere Admin-Seiten werden nicht verändert (neues CSS ist auf `body.tl-page` gescoped).
- `menu.php` bleibt unangetastet; die Navigation wird nur optisch über das neue Stylesheet angepasst.

---

## 1. Struktur / Layout

**Vorher**

- Layout aus `<table>`-Elementen, `float:left`, Fixbreiten (`130px`, `180px`, `680px`), `max-width:1036px`.
- „Car Info" und „Letzte Fahrt" teilten sich eine Tabelle, getrennt durch eine `<thead>`-Zeile mitten in der Tabelle.
- Karte fest `680 × 400 px` in einer eigenen Tabelle.
- Info-Box (`.HeaderT`) als versteckte Tabellenzeile mit „Yes/No".

**Nachher**

- Semantisches `<main class="tl-app">` mit CSS Grid und Flex, `max-width:1200px`, zentriert.
- Hero als zweispaltiges Grid: links Identität + KPIs, rechts Karte.
- KPI-Kacheln:
  - SOC (groß, primär eingefärbt)
  - Reichweite inkl. optionalem 100 %-Hinweis
  - Kilometerstand
  - Letztes Update
- Karten-Grid darunter: „Fahrzeuginformationen" (BMS + Versionen) und „Letzte Fahrt".
- Remote-Inhalt (teslalogger.de) in eigener Card, mit `overflow-x`-Schutz.
- Info-Banner als vollbreiter In-Flow-Banner mit Aktionen rechts.

## 2. Status & Handlungsaufforderungen

- Neuer **Status-Badge** mit Farbe, Punkt und Text: `online`, `driving`, `charging`, `sleep`, `offline`, `error`.
  Ladevorgang erhält einen dezenten Puls (respektiert `prefers-reduced-motion`).
- Detailzeilen nur dort, wo sie Mehrwert haben (Ladeleistung, Restzeit, Routenziel, Vorklimatisierung,
  Sentry, Battery Heater). Reine Zustände wie „Offline"/„Sleeping" werden nicht mehr doppelt angezeigt.
- **Fleet-API-/Subscription-Warnungen** stehen jetzt als farbige Alerts über dem Inhalt, jeweils mit
  klarer CTA (`Credentials` bzw. `Subscribe`). Die Subscription-Prüfung läuft weiterhin asynchron,
  die Zeile wird erst bei „No subscription" eingeblendet.
- Update-Hinweis der Teslalogger-Version als Chip statt roter `<font>`-Textzeile.
- Fehlerzustände (`FatalError`, `Inactive`) in Rot mit `is-error`-Klasse statt `<font color="red">`.

## 3. Visuelle Hierarchie

- Fahrzeugname (`h1`) ist wieder sichtbar; das alte `#displayname { display:none }` auf Desktop entfällt.
- SOC ist die größte Kennzahl der Seite; Sekundärwerte sind eindeutig kleiner.
- Tabellenkopf-Grau (`#d0d0d0`) und drei konkurrierende Header-Stile wurden durch ein einheitliches
  Card-/Typo-System ersetzt (Uppercase-Labels, ruhige Flächen, eine Akzentfarbe).
- Karte bleibt großes visuelles Element, ist aber nicht mehr die einzige Blickfang-Fläche.
- BMS-/Diagnosewerte sind standardmäßig eingeklappt (`<details>`), statt die Kernwerte nach unten zu drängen.

## 4. Responsive & Darstellung

- Breakpoints: 1100 px (KPIs 2×2), 1000 px (Hero einspaltig), 900 px (Karten einspaltig),
  760 px (KPIs 2 Spalten, Karte volle Breite), 460 px (Label/Wert untereinander).
- Geprüft bei 1440, 1024, 800 und 390 px: kein horizontaler Overflow.
- `prefers-color-scheme: dark` mit eigener Token-Palette inkl. Badges, Alerts und Chips.
- Karten-Höhe über `flex`/`min-height`, Karte füllt die Card zuverlässig (Leaflet-Container absolut positioniert).

## 5. Bedienung & Barrierearmut

- `<font>`-Tags, ungültige `<thead><td>`-Strukturen und `float`-Layouts entfernt.
- Status-Badge und Status-Detail mit `aria-live="polite"`.
- Icons (offene Fenster/Frunk/Trunk/Tür/Unlocked) mit `alt`-Texten, Status nicht mehr nur über Farbe.
- Buttons haben echte Hover-/Focus-Zustände und `:focus-visible`-Ringe.
- `#car_version_link` startet mit `href="#"`; der Reload beim Klick ohne vorhandene Version entfällt.
- Datumsausgabe korrigiert (`toLocaleString` statt `toLocaleTimeString` mit Datumsoptionen).
- Trip- und Range-Werte mit Guard: keine `NaN`-Anzeige bei `battery_level = 0` oder fehlenden Daten.

## 6. Technische Details

- **Polling unverändert:** `setInterval(..., 250)`, Aufruf nur bei fokussiertem Tab.
- Karte: Marker wird einmal erzeugt und danach aktualisiert (`setLatLng` + `setRotationAngle`);
  `panTo` nur bei Positionsänderung > 2 m. Dadurch bleibt die Karte bedienbar.
- Neue Helfer im Inline-JS: `setText`, `setHtml`, `setRow`, `setFlag`, `setStatus`, `fullRangeText`, `UpdateMap`.
- Sichtbarkeit über `hidden`-Attribut/Klassen statt jQuery `.show()/.hide()` (keine `display`-Konflikte).
- `checkForUpdates()` wurde durch `getTeslaloggerVersionInfo()` ersetzt (liefert Array statt Echo),
  inkl. Timeout-/Fehler-Absicherung der Remote-Abfragen und ohne `Undefined variable $branch`.
- Remote-Inhalt wird mit Fehlerbehandlung geladen (bei Fehler wird die Card nicht gerendert).
- Version-/Branch-Chips werden escaped ausgegeben.

---

## Dateien

| Datei | Änderung |
|---|---|
| `TeslaLogger/www/admin/index.php` | Neu aufgebaut (Markup, CSS-Klassen, Inline-JS), 832 Zeilen |
| `TeslaLogger/www/admin/static/cockpit.css` | Neu, 995 Zeilen, nur auf `body.tl-page` gescoped |
| `TeslaLogger/copy-web-to.net8docker.cmd` | `cockpit.css` ergänzt, Container-Name auf `teslalogger-webserver` vereinheitlicht |

Nicht geändert: `menu.php`, `info.php`, `current_json.php`, `language.php`, `tools.php`,
`static/teslalogger_style.css`.

---

## Deployment

```powershell
docker cp .\www\admin\index.php teslalogger-webserver:/var/www/html/admin/
docker cp .\www\admin\static\cockpit.css teslalogger-webserver:/var/www/html/admin/static/
```

Oder gesammelt über `TeslaLogger\copy-web-to.net8docker.cmd`.

Verifikation:

- `php -l` im Container: keine Syntaxfehler
- `GET http://localhost:8888/admin/index.php` → HTTP 200, keine PHP-Warnungen
- `GET .../static/cockpit.css` → HTTP 200
- Browser: nur die bereits vorher vorhandenen Matomo-Cookie-Fehler aus dem Remote-Content
- Sichtprüfung 1440 / 1024 / 800 / 390 px, Dropdown „Extras" und simulierter Ladezustand

---

## Hinweise / offene Punkte

- **Kein `www`-Volume:** Der Webserver-Container mountet nur `/tmp`. Die Änderungen überleben kein
  Neu-Erstellen des Containers (Watchtower / `docker compose up`). Danach `copy-web-to.net8docker.cmd`
  erneut ausführen.
- **Abweichende CSS im Container:** Die dortige `static/teslalogger_style.css` enthält eine zusätzliche
  Regel `#info { position: fixed; ... }`, die im Repo nicht existiert. `cockpit.css` setzt `#info`
  explizit auf `position: static; display: none`, sodass der Banner unabhängig davon korrekt funktioniert.
- **Karte bei 0,0:** Ist keine Position vorhanden, liefert die DB 0/0; die Karte zeigt dann Ozean.
  Das war vorher genauso und wurde bewusst nicht verändert.
- **Remote-Content:** Der Block von `teslalogger.de` bringt eigenes, sehr großes Bildmaterial mit.
  Inhaltlich unverändert; nur in eine Card mit horizontalem Scrollschutz gefasst.
- **Nicht umgesetzt (bewusst):** Umbau der Hauptnavigation in `menu.php`, Entfernen von jQuery UI /
  Genericons, Umstieg von AJAX-Polling auf SSE/WebSocket.
