---
sidebar_position: 11
---
# CO2-Berechnung

![image](https://user-images.githubusercontent.com/6816385/215699322-a54dfaf2-5fb3-4a6b-8888-3639dc67429e.png)

Nach einem Neustart versucht Teslalogger das CO2 für das Land zu berechnen, in dem du dein Auto geladen hast. Nachdem dieses Feature den Release-Status erreicht hat, wird die Berechnung jede Nacht durchgeführt.

## Woher stammen die Daten?
Sie basieren hauptsächlich auf freien Daten von [ENTSO-E](https://www.entsoe.eu/) sowie von Fraunhofer: https://www.energy-charts.info/ .  
Die Erzeugung der verschiedenen Energieträger wird mit einem spezifischen gCO2eq/kWh-Faktor (UNECE / IPCC / INCER ACV) multipliziert.

## Wie wird berechnet?
Importierte Energie aus anderen Ländern wird mit dem durchschnittlichen CO2/kWh-Wert von 2021 angesetzt, um eine Endlosschleife zu verhindern (Deutschland exportiert in die Schweiz, die Schweiz nach Österreich und Österreich zurück nach Deutschland).  
Pumpspeicherkraftwerke werden nicht berücksichtigt, da die gespeicherte Energie zeitlich und geografisch unbestimmt sein kann.

## Bekannte Einschränkungen
Die CO2-Berechnung erfolgt nur für den Startzeitpunkt des Ladevorgangs. Ein sehr langes Laden kann daher etwas ungenauer sein. Dies wird in einem zukünftigen Update verbessert.  
Da die Daten hauptsächlich auf ENTSO-E basieren, werden überwiegend europäische Länder unterstützt.  
Die Daten für Italien können ungenau sein, da es wegen des Atomausstiegs große Energiemengen aus anderen Ländern importiert. Durch die hohe Gewichtung des Imports und die Nutzung des Durchschnittswerts von 2021 kann die Genauigkeit leiden.

## FAQ:
- Warum ist Teslaloggers Berechnung etwas anders als die von https://www.electricitymaps.com/ ? Electricitymaps unterscheidet nicht zwischen Braunkohle und Steinkohle. Teslalogger ist daher präziser.
- Warum wird meine Solaranlage nicht berücksichtigt? Weil es keinen Unterschied macht, ob du den Strom selbst verbrauchst oder andere ihn nutzen.
- Warum wird Land XXX nicht unterstützt? Vermutlich fehlen frei verfügbare Daten. Wenn es eine freie Quelle gibt und genügend Teslalogger-Nutzer, könnte es künftig unterstützt werden.
