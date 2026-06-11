---
sidebar_position: 11
---
# CO2 Calculation

![image](https://user-images.githubusercontent.com/6816385/215699322-a54dfaf2-5fb3-4a6b-8888-3639dc67429e.png)

After a restart, TeslaLogger tries to calculate the CO2 for the country where you charged your car. After this feature reaches release status, the calculation will be performed every night.

## Where does the data come from?
It is mainly based on free data from [ENTSO-E](https://www.entsoe.eu/) and from Fraunhofer: https://www.energy-charts.info/.  
The generation of the different energy sources is multiplied with a specific gCO2eq/kWh factor (UNECE / IPCC / INCER ACV).

## How is it calculated?
Imported energy from other countries is estimated with the average CO2/kWh value of 2021 to prevent an infinite loop (Germany exports to Switzerland, Switzerland to Austria, and Austria back to Germany).  
Pumped storage power plants are not taken into account, as the stored energy can be indeterminate in time and geography.

## Known Limitations
The CO2 calculation is only performed for the start time of the charging session. A very long charging session can therefore be somewhat less accurate. This will be improved in a future update.  
Since the data is mainly based on ENTSO-E, predominantly European countries are supported.  
The data for Italy may be inaccurate due to large amounts of energy imported from other countries following the nuclear phase-out. Due to the high weighting of imports and the use of the 2021 average value, accuracy may suffer.

## FAQ:
- Why is TeslaLogger's calculation slightly different from https://www.electricitymaps.com/? Electricitymaps does not distinguish between lignite and hard coal. TeslaLogger is therefore more precise.
- Why is my solar system not taken into account? Because it makes no difference whether you consume the electricity yourself or others use it.
- Why is country XXX not supported? Probably freely available data is missing. If there is a free source and enough TeslaLogger users, it could be supported in the future.
