# UI-Änderungen an der Admin-Startseite (Fable 5.2)

Die Startseite des Admin-Panels (`TeslaLogger/www/admin/index.php`) wurde von einem Tabellenlayout auf ein Karten-Dashboard umgebaut. Der Ladezustand und der Fahrzeugstatus stehen jetzt groß oben, die Karte lässt sich frei verschieben, und ein Verbindungsabbruch wird angezeigt. Die Seite folgt außerdem der Hell-/Dunkel-Einstellung des Browsers.

Das Protokoll `logfile.php` wurde im selben Umfang umgebaut: Seitenkopf, Filterformular und Protokoll-Box in Karten, gleiche Schrift und gleiche Farben. Die gemeinsame Gestaltung (Tokens, Karten, Überschriften, Alerts) steht in `teslalogger_ui.css`, die beide Seiten laden.

Stand: 16.11.2026, erstellt mit OpenCode (Modell `anthropic/claude-fable-5.2`). Es ist ein Prototyp für den ersten von drei Redesign-Vorschlägen; Navigation und Hinweis-System sind unverändert. Alle anderen Admin-Seiten sind unverändert und bleiben hell, nur `logfile.php` folgt ebenfalls der Hell-/Dunkel-Einstellung.

Inhalt:

- [Geänderte Dateien](#geänderte-dateien)
- [Was sich für den Nutzer ändert](#was-sich-für-den-nutzer-ändert)
- [Technische Details](#technische-details)
- [Dark Mode](#dark-mode)
- [Leitfaden: weitere Seiten auf Dark Mode umstellen](#leitfaden-weitere-seiten-auf-dark-mode-umstellen)
- [Logfile: erste weitere Seite](#logfile-erste-weitere-seite)
- [Stylesheet-Einteilung](#stylesheet-einteilung-entscheidung-17112026)
- [Übersetzungen](#übersetzungen)
- [Deployment in den Docker-Container](#deployment-in-den-docker-container)
- [Test](#test)
- [Bekannte Einschränkungen](#bekannte-einschränkungen)
- [Nicht umgesetzt](#nicht-umgesetzt)

## Geänderte Dateien

| Datei | Änderung |
|---|---|
| `TeslaLogger/www/admin/index.php` | Markup auf Karten umgebaut, JavaScript erweitert |
| `TeslaLogger/www/admin/static/teslalogger_ui.css` | gemeinsame Design-Tokens, Basis-Komponenten (Karten, Überschriften, Alerts, Formular) **und** die seiten-spezifischen Regeln der umgestylten Seiten (DataTables, `tl-form`), wird von allen umgestylten Seiten geladen (siehe [Stylesheet-Einteilung](#stylesheet-einteilung-entscheidung-17112026)) |
| `TeslaLogger/www/admin/static/teslalogger_index.css` | nur noch Startseiten-spezifische Styles (Hero, Akkubalken, Karte, Switch), Tokens und Basis-Komponenten stehen jetzt in `teslalogger_ui.css` |
| `TeslaLogger/bin/language-en.txt` | zwei neue Texte |
| `TeslaLogger/bin/language-de.txt` | zwei neue Texte |
| `TeslaLogger/www/admin/logfile.php` | Markup wie die Startseite (Karten, `tl-card-title`), Formular in einem Grid, `ui.css` geladen |
| `TeslaLogger/www/admin/static/teslalogger_logfile.css` | wird nur von `logfile.php` geladen: Seitenkopf, Log-Box, Fehler-Highlight über die Tokens |
| `TeslaLogger/www/admin/journeys.php` | auf das Karten-Design umgebaut (Seitenkopf, Tabelle- und Formular-Karte, DataTables, nur `ui.css`), Hilfe-Icon |
| `TeslaLogger/www/admin/static/teslalogger_journeys.css` | entfernt, die Regeln stehen jetzt in `teslalogger_ui.css` |

`menu.php`, `info.php`, `tools.php` und `static/teslalogger_style.css` sind unverändert.

## Was sich für den Nutzer ändert

### Layout

- **Statuskarte oben:** Fahrzeugname, Status-Badge, Ladezustand als große Zahl, Reichweite und ein Akkubalken über die ganze Breite.
- **Fahrzeuginformationen:** Kilometerstand, Fahrzeug-Version, Software-Update, letztes Update, Teslalogger-Version und die ScanMyTesla-Werte als Liste.
- **Letzte Fahrt:** Distanz, Dauer, Verbrauch, Ø Verbrauch und Höchstgeschwindigkeit als Kennzahlen, der Startzeitpunkt steht in der Kopfzeile.
- **Aktuelle Position:** Karte in einer eigenen Karte mit Schalter „Fahrzeug folgen".
- **Block von teslalogger.de:** steht in einer eigenen Karte, inhaltlich unverändert.

Ab 900 px Breite liegen Fahrzeuginformationen und letzte Fahrt links, die Karte rechts daneben. Darunter stehen alle Karten untereinander in der Reihenfolge Status, Fahrzeuginformationen, letzte Fahrt, Karte.

### Hell und dunkel

- Die Startseite folgt dem Farbschema, das Browser oder Betriebssystem vorgeben. Einen Schalter auf der Seite gibt es nicht.
- Im hellen Schema sieht die Seite aus wie vor dem Einbau des Dark Mode.
- Im dunklen Schema sind Hintergrund und Karten dunkel, Text und Icons hell, die Signalfarben etwas heller.
- Die Landkarte bleibt in beiden Schemata hell.
- Die Kopfzeile mit dem Menü ist in beiden Schemata schwarz.
- Alle anderen Admin-Seiten bleiben hell, auch wenn der Browser ein dunkles Schema verlangt.

### Status-Badge

Der Status steht als farbiges Badge neben den Details, statt als „Status: …" in einer Tabellenzeile.

| Zustand | Badge-Text | Farbe |
|---|---|---|
| Laden | Laden | grün |
| Online | Online | grün |
| Fahren | Fahren | blau |
| Schlafen, Einschlafen | Schläft, Einschlafen | grau |
| Offline | Offline | grau |
| Inaktiv | Inaktiv | rot |
| Fehler | Fatal Error | rot |

Die Details daneben sind dieselben wie bisher: Ladeleistung und Restzeit beim Laden, Geschwindigkeit und Ziel beim Fahren, Vorklimatisierung, Wächter-Modus und Batterie-Heizung bei Online.

### Akkubalken

- Der Balken zeigt den Ladezustand in Prozent.
- Bis 20 % ist er orange, bis 10 % rot, darüber grün.
- Während des Ladens markiert ein Strich das Ladelimit.

### Karte

- Der Schalter „Fahrzeug folgen" ist beim Laden der Seite an. Die Karte zentriert dann wie bisher bei jeder Abfrage auf das Fahrzeug.
- Wer die Karte verschiebt, schaltet das Folgen ab. Die Karte bleibt stehen, der Fahrzeug-Pfeil bewegt sich weiter.
- Einschalten zentriert die Karte sofort wieder auf das Fahrzeug.
- Zoomen schaltet das Folgen nicht ab.

### Alter der Daten und Verbindungsfehler

- Hinter „Letztes Update" steht das Alter der Daten, zum Beispiel „(vor 33 Minuten)". Die Formulierung kommt vom Browser und richtet sich nach der Browsersprache, nicht nach der Teslalogger-Spracheinstellung.
- Schlagen die Abfragen von `current_json.php` mindestens 3 Sekunden lang fehl, erscheint oben der Hinweis „Keine Verbindung zum Teslalogger", und alle Karten werden abgeblendet. Die erste erfolgreiche Antwort blendet den Hinweis wieder aus.

## Technische Details

### Polling

Unverändert: `GetCurrentData()` läuft alle 250 ms, solange das Fenster den Fokus hat.

### Markup und IDs

Alle IDs, in die `GetCurrentData()` schreibt, sind geblieben. Auch die Info-Box funktioniert mit dem unveränderten `info.php` weiter: `.HeaderT`, `#InfoText`, `#PositiveButton` und `#NegativeButton` existieren weiterhin, `.HeaderT` und `#InfoText` sind jetzt `div`-Elemente statt Tabelle und `span`.

Neue IDs:

| ID | Zweck |
|---|---|
| `conn_error` | Hinweis bei Verbindungsfehler |
| `tl_grid` | Container der vier Karten |
| `battery_bar`, `battery_fill` | Akkubalken und Füllung |
| `charge_limit_marker` | Strich für das Ladelimit |
| `last_update_age` | Alter der Daten |
| `follow_toggle` | Schalter „Fahrzeug folgen" |

Entfallen ist `#displayname`; der Fahrzeugname steht jetzt immer in der Statuskarte und wird mit `htmlspecialchars()` ausgegeben.

### JavaScript

Neue Funktionen in `index.php`:

| Funktion | Aufgabe |
|---|---|
| `setCarStatus(state, label, detailsHtml)` | setzt Badge-Text, `data-state` und Details |
| `updateBattery(jsonData, isCharging)` | Breite und Farbe des Akkubalkens, Ladelimit |
| `setFollowCar(follow)` | Folgen an oder aus, beim Einschalten zentrieren |
| `setConnectionLost(lost)` | Hinweis und Abblendung ein- oder ausschalten |
| `formatAge(date)` | Alter der Daten über `Intl.RelativeTimeFormat` |

Weitere Änderungen:

- `GetCurrentData()` hat einen `.fail()`-Handler bekommen.
- Die Karte ruft `panTo()` nur noch auf, wenn das Folgen an ist. Das Leaflet-Ereignis `dragstart` schaltet es ab.
- Ein `ResizeObserver` ruft `map.invalidateSize()` auf, weil die Kartenhöhe mit den Nachbarkarten wächst.
- Die Temperatur der Vorklimatisierung erscheint bei der Einstellung Fahrenheit in °F. Diese Korrektur war im Docker-Image bereits enthalten und fehlte in der lokalen Arbeitskopie.
- Die `<font>`-Tags im Statusbereich sind entfernt, die Farbe kommt aus dem Stylesheet.
- Der Block von teslalogger.de wird nur ausgegeben, wenn der Abruf Inhalt liefert. Ein führendes UTF-8-BOM wird entfernt, weil es in der Karte als Leerzeile erschien.

Für den Dark Mode war keine Änderung am JavaScript nötig.

### Stylesheet

`static/teslalogger_ui.css` (gemeinsame Tokens und Basis-Komponenten) und `static/teslalogger_index.css` (Startseiten-Details) werden zusätzlich zu `teslalogger_style.css` geladen. Das Theme-Stylesheet bleibt, weil das Menü und alle anderen Admin-Seiten davon abhängen. `logfile.php` lädt `teslalogger_ui.css` und `teslalogger_logfile.css` statt `teslalogger_index.css`.

- Alle Klassen beginnen mit `tl-`.
- Klassennamen dürfen weder `content` noch `site` enthalten. Das Theme hängt an `[class*="content"]` und `[class*="site"]` Clearfix-Pseudoelemente, die im Grid als zusätzliche Elemente auftauchen würden.
- Farben, Abstände und Radius sind CSS-Variablen an `:root`, siehe [Dark Mode](#dark-mode).
- Umbrüche: zweispaltig ab 900 px, kompaktere Abstände bis 480 px.
- Der Link in `index.php` und `logfile.php` trägt eine Versionsnummer (`teslalogger_index.css?v=2`, `teslalogger_logfile.css?v=2`). Sie muss bei jeder Änderung am Stylesheet hochgezählt werden, sonst zeigen Browser die alte Datei aus dem Cache.

## Dark Mode

### Funktionsweise

Am Anfang von `teslalogger_ui.css` stehen zwei Blöcke:

```css
:root {
	--tl-bg: #f5f5f5;
	--tl-card: #ffffff;
	/* ... helle Werte ... */
}

@media (prefers-color-scheme: dark) {
	:root {
		color-scheme: dark;
		--tl-bg: #111315;
		--tl-card: #1c1f24;
		/* ... dunkle Werte ... */
	}
}
```

- Alle Regeln darunter verwenden nur `var(--tl-…)`. Eine eigene dunkle Variante einzelner Regeln gibt es nicht, bis auf die [Sonderfälle](#sonderfälle).
- `body { background: var(--tl-bg); }` und `.tl-page` gestalten die Seite außerhalb der eigenen Karten. Das ist möglich, weil nur die umgestylten Seiten (`index.php`, `logfile.php`) die Datei laden.
- `color-scheme: dark` sorgt für dunkle Scrollbalken und dunkle Standard-Formularelemente.
- Der helle Wert von `--tl-bg`, `--tl-link` und `--tl-link-hover` ist derselbe wie im Theme. Dadurch ändert sich im hellen Schema nichts.

### Farbvariablen

| Variable | Hell | Dunkel | Verwendung |
|---|---|---|---|
| `--tl-bg` | `#f5f5f5` | `#111315` | Seitenhintergrund |
| `--tl-card` | `#ffffff` | `#1c1f24` | Kartenfläche |
| `--tl-border` | `#e3e5e8` | `#2e333a` | Kartenrand, Trennlinien |
| `--tl-text` | `#1f2328` | `#e6e8eb` | Text, Strich für das Ladelimit |
| `--tl-muted` | `#636b74` | `#9aa3ad` | Überschriften der Karten, Beschriftungen, Einheiten |
| `--tl-track` | `#e6e8eb` | `#30353c` | leerer Teil des Akkubalkens |
| `--tl-link` | `#24890d` | `#6fcf58` | Links |
| `--tl-link-hover` | `#41a62a` | `#9be58a` | Links unter dem Mauszeiger |
| `--tl-accent` | `#24890d` | `#3fae24` | Akkubalken, Schalter an, Fokusrahmen |
| `--tl-accent-strong` | `#1a6b09` | `#7fd76a` | Text grüner Badges |
| `--tl-accent-soft` | `#e6f4e2` | `#1d3a17` | Fläche grüner Badges |
| `--tl-info-strong` | `#0d4f9e` | `#8ec2ff` | Text des blauen Badges |
| `--tl-info-soft` | `#e3f0fc` | `#16304f` | Fläche des blauen Badges |
| `--tl-warn` | `#e08a00` | `#f0a020` | Akkubalken bis 20 % |
| `--tl-danger` | `#c62828` | `#e5534b` | Akkubalken bis 10 % |
| `--tl-danger-strong` | `#b3261e` | `#ff938c` | Text roter Badges, Hinweise und Fehlertexte |
| `--tl-danger-soft` | `#fdeaea` | `#45201f` | Fläche roter Badges und Hinweise |
| `--tl-neutral-strong` | `#4a525a` | `#c3cad2` | Text grauer Badges |
| `--tl-neutral-soft` | `#eceef1` | `#2a2f36` | Fläche grauer Badges |
| `--tl-switch-off` | `#8b939c` | `#6b747e` | Schalter aus |
| `--tl-icon-filter` | `none` | `invert(1)` | Filter für schwarze PNG-Icons |
| `--tl-shadow` | weicher Schatten | `none` | Schatten der Karten |

`--tl-radius` und `--tl-gap` sind in beiden Schemata gleich.

Das Muster der Namen: `-soft` ist eine getönte Fläche, `-strong` der Text darauf, ohne Zusatz die kräftige Farbe für Balken und Bedienelemente.

### Sonderfälle

- **Fahrzeug-Icons:** Die Icons für Tür, Fenster, Kofferraum und Verriegelung sind schwarze PNG-Dateien. Im dunklen Schema kehrt `filter: var(--tl-icon-filter)` sie um.
- **Links:** Die Linkfarbe des Themes ist auf dunklem Grund zu dunkel. Innerhalb von `.tl-page` setzt die Regel `a:not(.leaflet-container a):not(.tl-alert a)` die Farbe aus den Variablen. Links in der Landkarte und in Hinweisen behalten ihre eigene Farbe.
- **Landkarte:** Die Kacheln sind helle Bilder. Deshalb bleiben auch die Bedienelemente der Karte hell: `.leaflet-container { color-scheme: light; }`.
- **YouTube-Logo:** Das Logo im Block von teslalogger.de hat dunkle Schrift und bekommt im dunklen Schema einen weißen Hintergrund.
- **Buttons der Info-Box:** Sie kommen aus dem Theme (grün mit weißer Schrift) und sind in beiden Schemata lesbar.
- **Feste Farben in `index.php`:** Geblieben sind der rote Zusatz „MONO" im Seitentitel und der rote Branch-Name in der Zeile „Teslalogger" (`checkForUpdates()`). Beide sind in beiden Schemata rot.

### Gemessene Kontraste

Berechnet nach WCAG. Für Text gelten 4,5:1 als Mindestwert, für Grafiken und Bedienelemente 3:1.

| Paar | Hell | Dunkel |
|---|---|---|
| Text auf Karte | 15,8 | 13,5 |
| Beschriftung auf Karte | 5,4 | 6,5 |
| Link auf Karte | 4,5 | 8,5 |
| Link unter dem Mauszeiger | 3,1 | 11,0 |
| Badge grün | 5,8 | 7,1 |
| Badge blau | 6,9 | 7,2 |
| Badge und Hinweis rot | 5,6 | 6,6 |
| Badge grau | 6,8 | 8,2 |
| Fehlertext auf Karte | 6,5 | 7,7 |
| Akkubalken grün auf leerem Balken | 3,7 | 4,3 |
| Akkubalken orange auf leerem Balken | 2,2 | 5,7 |
| Akkubalken rot auf leerem Balken | 4,6 | 3,3 |
| Schalter aus auf Karte | 3,1 | 3,5 |
| Schalter an auf Karte | 4,5 | 5,7 |

Zwei helle Werte liegen unter dem Mindestwert: die Linkfarbe unter dem Mauszeiger (aus dem Theme übernommen) und der orange Akkubalken. Beim Balken steht der Ladezustand zusätzlich als Zahl darüber.

### Dunkles Schema ausprobieren

- **Chrome und Edge:** Entwicklertools öffnen (F12), über das Menü mit den drei Punkten unter „Weitere Tools" den Reiter „Rendering" einblenden und dort die Emulation von `prefers-color-scheme` umstellen.
- **Firefox:** Entwicklertools öffnen (F12), im Inspektor über den Regeln die Schaltflächen mit Sonne und Mond benutzen.
- **Ohne Entwicklertools:** das Farbschema des Betriebssystems oder des Browsers umstellen.

## Leitfaden: weitere Seiten auf Dark Mode umstellen

### Ausgangslage

Die Zahlen stammen aus einer groben Textsuche im Verzeichnis `TeslaLogger/www/admin` und sind als Anhaltspunkt gedacht.

- **Theme:** 22 Seiten (einschließlich der Startseite) laden `teslalogger_style.css` und das jQuery-UI-Theme „smoothness". Beide sind hell und kennen kein dunkles Schema.
- **`dashboard.php`:** hat eine eigene Gestaltung mit schwarzem Hintergrund (`dashboard.css`) und lädt das Theme nicht.
- **Farben im Theme, die jede Seite betreffen** (`teslalogger_style.css`):

| Zeile | Regel | Wert |
|---|---|---|
| 116 | Textfarbe für `body`, `button`, `input`, `select`, `textarea` | `#2b2b2b` |
| 124 | Hintergrund von `body` | `#f5f5f5` |
| 130, 145 | Links und Links unter dem Mauszeiger | `#24890d`, `#41a62a` |
| 237, 362 | Rahmen von `pre` und `fieldset` | `rgba(0, 0, 0, 0.1)` |
| 484–486 | Rahmen und Textfarbe von `input` und `textarea` | `rgba(0, 0, 0, 0.1)`, `#2b2b2b` |
| 507 | Hintergrund der Buttons | `#24890d` |

- **Eigene `<style>`-Blöcke:** `chargingcost.php`, `geoadd.php`, `geofencing.php`, `journeys.php`, `ovms.php`, `settings.php`.
- **Feste Farben im Seitencode:** `logfile.php` ist bereits umgestellt (siehe [Logfile: erste weitere Seite](#logfile-erste-weitere-seite)), außerdem `ovms.php`, `chargingcost.php`, `geofencing.php` und `adminpanelpassword.php`.
- **jQuery UI:** Dialoge in `password.php`, `password_fleet.php` und `settings.php`, dazu der Timepicker in `settings.php`.
- **Nicht mehr benutzte Theme-Regeln:** `.HeaderStyle`, `.HeaderL`, `.THeader`, `.b1` und `#displayname` (Zeilen 4349–4382) verwendet nach dem Umbau der Startseite keine Seite mehr. `.HeaderT` braucht die Info-Box weiterhin.

### Vorgehen

1. **Variablen auslagern.** Die beiden Blöcke `:root` und `@media (prefers-color-scheme: dark)` stehen in einer gemeinsamen Datei, die Werte dürfen nur an einer Stelle stehen.
    - Diese gemeinsame Datei ist schon angelegt: `teslalogger_ui.css` (Tokens und die Basis-Komponenten `.tl-page`, `.tl-card`, `.tl-card-title`, `.tl-alert`, Formular). Neue Seiten laden sie direkt.
    - Seit der [Stylesheet-Einteilung](#stylesheet-einteilung-entscheidung-17112026) stehen auch die seiten-spezifischen Regeln in `teslalogger_ui.css` (als scoping-Wrapper), es gibt keine separate CSS-Datei pro Seite.
2. **Nur umgestellte Seiten laden die Datei.** `color-scheme: dark` und der dunkle Hintergrund wirken auf die ganze Seite. Eine Seite, deren Inhalt noch helle feste Farben hat, würde sonst halb dunkel erscheinen.
3. **Grundfarben der Seite setzen.** Auf einer umgestellten Seite müssen mindestens diese Theme-Werte durch Variablen ersetzt werden:
   - Hintergrund von `body`: `--tl-bg`
   - Textfarbe: `--tl-text`
   - Links: `--tl-link` und `--tl-link-hover`
   - Rahmen von Eingabefeldern, `pre`, `fieldset` und Tabellen: `--tl-border`
   - Hintergrund und Textfarbe von `input`, `select` und `textarea`: `--tl-card` und `--tl-text`
4. **Feste Farben im Seitencode ersetzen.** `<font color>`, `style="color: …"` und Farben in `<style>`-Blöcken durch Klassen mit Variablen ersetzen. Rot wird `--tl-danger-strong`, Grün `--tl-accent-strong`, graue Tabellenköpfe `--tl-neutral-soft` mit `--tl-text`.
5. **Neue Farben immer doppelt anlegen.** Braucht eine Seite eine Farbe, die es noch nicht gibt, kommt eine neue Variable mit hellem und dunklem Wert in die gemeinsame Datei. In den Regeln der Seite steht nie ein fester Farbwert.
6. **Icons prüfen.** Schwarze PNG-Icons bekommen `filter: var(--tl-icon-filter)`. Fotos, Screenshots und farbige Logos bekommen keinen Filter.
7. **Fremde Bausteine entscheiden.**
   - Landkarten bleiben hell, siehe [Sonderfälle](#sonderfälle).
   - jQuery-UI-Dialoge und der Timepicker sind hell. Entweder bleiben sie mit `color-scheme: light` hell wie die Landkarte, oder `.ui-widget-content`, `.ui-widget-header` und `.ui-state-default` bekommen Farben aus den Variablen.
8. **Formulare ansehen.** Mit `color-scheme: dark` zeichnet der Browser Checkboxen, Auswahllisten und Datumsfelder dunkel. Felder mit eigenem hellem Hintergrund fallen dann auf.
9. **Klassennamen.** Auch auf anderen Seiten gilt: keine Klassennamen mit `content` oder `site`.
10. **Prüfen.** Jede Seite in beiden Schemata ansehen, auch Zustände wie Fehlermeldungen, leere Listen, Hover und Fokus sowie die schmale Ansicht. Neue Farbpaare nach WCAG nachrechnen.

### Wenn alle Seiten umgestellt sind

- Die Variablen können direkt in `teslalogger_style.css` verwendet werden. Die Regel `body { background: var(--tl-bg); }` in `teslalogger_ui.css` entfällt dann.
- Die gemeinsame Datei ist `teslalogger_ui.css`. Wenn alle Seiten umgestellt sind, können die Token-Blöcke direkt in `teslalogger_style.css` wandern.
- Die nicht mehr benutzten Theme-Regeln können gelöscht werden.
- Das Menü ist in beiden Schemata schwarz mit grünen Untermenüs und braucht keine Anpassung.

## Stylesheet-Einteilung (Entscheidung, 17.11.2026)

**Seiten-spezifische CSS-Dateien werden zusammengeführt in `teslalogger_ui.css`.**
`teslalogger_ui.css` ist die gemeinsame Datei für Tokens, Basis-Komponenten (`.tl-page`,
`.tl-card`, `.tl-card-title`, `.tl-alert`, Formular) **und** die seiten-spezifischen
Regeln der umgestylten Seiten. Die Regeln werden über scoping-Wrapper gekapselt
(`.tl-table-card`, `.tl-journey-card`), damit sie nicht auf andere Seiten auswirken.

Begründung: eine separate Datei pro Seite (z. B. `teslalogger_journeys.css`) erzeugt
einen zusätzlichen `docker cp`-Punkt, einen `?v=`-Cache-Key und einen zweiten Ort,
der bei einer Änderung gepflegt werden muss. Die gemeinsame Datei wird einmal
kopiert und einmal versioniert, und die Regeln bleiben über scoping kapselbar.

- `journeys.php` lädt nur `teslalogger_ui.css?v=4` (statt `teslalogger_style.css`
  + `teslalogger_journeys.css`); `teslalogger_journeys.css` ist gelöscht.
- `logfile.php` und `index.php` laden die gemeinsame Datei weiterhin; ihre lokale
  Duplikate der `.tl-form`/`.tl-page`-Regeln können beim nächsten Touch entfernt
  werden, die Regeln sind jetzt identisch in `teslalogger_ui.css`.
- Neue Seiten: `teslalogger_ui.css` (Token + Basis-Komponenten) + `teslalogger_style.css`
  laden, die eigene Regeln als scoping-Wrapper (z. B. `.tl-xyz-card`) in
  `teslalogger_ui.css` ablegen, und den `?v=` hochzahlen.

## Logfile: erste weitere Seite

`logfile.php` ist die erste Seite neben der Startseite, die der Hell-/Dunkel-Einstellung folgt. Damit sie wie die Startseite aussieht, wurde sie im selben Umfang umgebaut: Seitenkopf, Filterformular und Protokoll-Box in Karten, gleiche Schrift und gleiche Farben.

### Vorgehen

1. **Gemeinsames Stylesheet einbinden.** `logfile.php` lädt jetzt, wie die Startseite, `teslalogger_ui.css` (Design-Tokens, `.tl-page`, `.tl-card`, `.tl-card-title`, `.tl-alert`, Formular-Farben) und zusätzlich `teslalogger_logfile.css`. Dadurch werden die Tokens und die Basis-Komponenten nur an einer Stelle gepflegt.
2. **Markup wie die Startseite.** Die `<div>` mit `id="log"` und das `<table>`-Formular werden zu `tl-card`-Karten. Die Formular-Zeilen kommen in `tl-form` (CSS-Grid), die Beschriftungen in `<label>`-Elemente, die Überschrift in `.tl-card-title`.
3. **Log-Box.** Die Protokoll-Box (`.tl-log`) bekommt in beiden Schemata die Karten-Farben aus den Tokens, in der Schriftart `monospace`. Im dunklen Schema wird der Scrollbalken dunkel.
4. **Fehler-Highlight.** Die `<font color="red">`-Tags, die der PHP-Code für die Fehlerausblendungen erzeugt, bekommen eine CSS-Regel `font[color="red"]` mit `color: var(--tl-danger)`. Das ist im hellen Schema `#c62828` und im dunklen Schema `#e5534b`.
5. **Links und Formulare.** Links im Protokoll-Text und im Seitenkopf bekommen `color: var(--tl-link)` (hell `#24890d`, dunkel `#6fcf58`). Die Eingabefelder (`.tl-page input`/`select`/`textarea`) sind in den Tokens definiert: im hellen Schema weiß mit `#24890d`-Grenze, im dunklen Schema `#1c1f24` mit `#2e333a`-Grenze.
6. **Version-Parameter.** In `logfile.php` wird das CSS-Link auf `?v=2` erhöht, um den Browser-Cache zu brechen.

### Kontraste (gemessen, WCAG)

| Element | Wert | Kontrast |
|---|---|---|
| Protokoll-Text `#e6e8eb` auf `#111315` (Log-Box) | 15,17:1 |
| Protokoll-Text `#e6e8eb` auf `#1c1f24` (Karte) | 13,46:1 |
| Beschriftungen `#9aa3ad` auf `#1c1f24` | 6,47:1 |
| Links `#6fcf58` auf `#1c1f24` | 8,45:1 |
| Fehlerausblendung `#e5534b` auf `#111315` | 5,03:1 |

### Nicht geändert

- **`menu.php`** (schwarz mit grün, funktioniert in beiden Schemata)
- **`teslalogger_style.css`** (Theme, bleibt hell, wird von `logfile.php` weiter geladen)
- **Die Buttons** (die grünen `input[type="submit"]`-Buttons behalten ihre grünen Theme-Stile)

## Übersetzungen

Zwei neue Schlüssel in `language-en.txt` und `language-de.txt`:

| Schlüssel | Deutsch |
|---|---|
| `Follow vehicle` | Fahrzeug folgen |
| `No connection to Teslalogger` | Keine Verbindung zum Teslalogger |

Alle anderen Sprachen fallen auf den englischen Text zurück.

Das Admin-Panel holt die Sprachdateien vom Teslalogger-Dienst (`getfile/language-xx.txt`). Die neuen Texte erscheinen deshalb erst übersetzt, wenn die Sprachdateien dort aktualisiert sind. Bis dahin steht der englische Schlüssel auf der Seite.

## Deployment in den Docker-Container

Die Web-Dateien liegen im Image des Containers `teslalogger-webserver` und sind nicht als Volume eingebunden. Aus dem Repository-Verzeichnis:

```
docker cp TeslaLogger\www\admin\index.php teslalogger-webserver:/var/www/html/admin/
docker cp TeslaLogger\www\admin\static\teslalogger_index.css teslalogger-webserver:/var/www/html/admin/static/
docker cp TeslaLogger\www\admin\static\teslalogger_ui.css teslalogger-webserver:/var/www/html/admin/static/
docker cp TeslaLogger\www\admin\static\teslalogger_logfile.css teslalogger-webserver:/var/www/html/admin/static/
docker cp TeslaLogger\www\admin\logfile.php teslalogger-webserver:/var/www/html/admin/
```

- Die Dateien gehören zusammen. Ohne `teslalogger_ui.css` erscheinen die umgestylten Seiten ungestaltet.
- Die Kopien gehen verloren, sobald der Container neu erstellt wird, zum Beispiel bei einem Image-Update durch Watchtower.
- `TeslaLogger/copy-web-to.net8docker.cmd` kopiert die neuen Stylesheets noch nicht mit. Für `logfile.php` gilt das für `teslalogger_logfile.css` und die geänderte `logfile.php`, außerdem für `teslalogger_ui.css` (neu, wird von `index.php` und `logfile.php` geladen).

## Test

Geprüft am 16.11.2026 im Container `teslalogger-webserver` (PHP 8.5.12) mit einem automatisierten Browser (Playwright):

- PHP-Syntaxprüfung mit `php -l`.
- Echte Daten: ein inaktives Fahrzeug ohne Werte, die Seite zeigt „Inaktiv" und 0 %.
- Laden, Fahren, Online, Schlafen und Fatal Error mit simulierten Antworten von `current_json.php` im Browser.
- Schalter „Fahrzeug folgen" per Maus und Tastatur, Zoomen bei eingeschaltetem Folgen.
- Verbindungsabbruch und Wiederherstellung.
- Breiten 1280 px und 390 px, kein horizontales Scrollen.
- Info-Box über `showInfoRestricted()`.
- Keine neuen Meldungen in der Browser-Konsole, keine fehlgeschlagenen Abrufe auf der Startseite.

Dark Mode, mit emuliertem `prefers-color-scheme`:

- Helles Schema: Hintergrund, Kartenfläche, Linkfarbe und Schatten haben dieselben Werte wie vor dem Einbau.
- Dunkles Schema: echte Daten sowie Laden, Fahren, Schlafen, Fatal Error, Info-Box, Schalter aus und Verbindungsabbruch, dazu die Breite 390 px.
- Bedienelemente und Links der Landkarte behalten im dunklen Schema ihre Farben.
- `changelog.php` und `settings.php` bleiben hell, wenn der Browser ein dunkles Schema verlangt.
- `logfile.php`: helles Schema wie die Startseite (Karten, Karten-Überschrift, Links, Eingabefelder); dunkles Schema mit echtem Protokoll, Fehlerausblendungen, Links und Eingabefeldern; `settings.php` und `changelog.php` bleiben auch dort hell.

Nicht geprüft: ein echtes ladendes oder fahrendes Fahrzeug, Meilen als Längeneinheit, andere Browser als der des Testwerkzeugs, die Hinweise für Fleet API und Abo.

## Bekannte Einschränkungen

- **Hängende Abfragen:** Der Verbindungshinweis erscheint nur, wenn Abfragen fehlschlagen. Eine Abfrage ohne Antwort wird nicht erkannt, weil kein Timeout gesetzt ist.
- **Fenster ohne Fokus:** Dann läuft wie bisher keine Abfrage, die Werte bleiben ohne Hinweis stehen.
- **Fahrzeug ohne Daten:** Der Fahrtbeginn erscheint als „01.01.1, 00:53:28", und die Karte zeigt die Position 0/0. Das Verhalten ist unverändert.
- **Feste Texte:** „To:", „In:", „Delay:", „Update available:", „Funktion eingeschränkt" und die Tooltips der Fahrzeug-Icons sind weiterhin nicht übersetzbar.
- **Seitenaufbau:** Die Update-Prüfung und der Abruf des Blocks von teslalogger.de blockieren weiterhin das Laden der Seite.
- **Stand der Arbeitskopie:** `info.php` und `tools.php` sind im Docker-Image neuer als in der lokalen Arbeitskopie. Beim nächsten Abgleich mit dem Repository ist im Statusbereich von `index.php` mit einem Merge-Konflikt zu rechnen.

Zum Dark Mode:

- **Wechsel zwischen Seiten:** Wer im dunklen Schema von der Startseite auf eine andere Admin-Seite wechselt, landet auf einer hellen Seite. Ausnahme: `logfile.php` folgt ebenfalls dem Schema.
- **Helle Flächen:** Die Landkarte und das Vorschaubild des Batteriezertifikats bleiben hell.
- **Kein Schalter:** Das Schema lässt sich nur über Browser oder Betriebssystem wählen.
- **Inhalt von teslalogger.de:** Der Block wird fremd geliefert. Ändert sich dort etwas, kann es im dunklen Schema schlecht lesbar sein.

## Nicht umgesetzt

Die beiden anderen Vorschläge aus der Analyse stehen noch aus:

- **Hinweise und Handlungsaufforderungen:** einheitliche Banner mit einem eindeutigen Button für Abo, Fleet API und Update, asynchrone Update-Prüfung, gestaltete Karten für Batteriezertifikat und Spende.
- **Navigation:** Gruppierung nach Aufgaben, Bestätigungsdialog für Neustart und Update, erkennbarer Fahrzeugwechsel.
