# CO2 Calculation (Beta)

![image](https://user-images.githubusercontent.com/6816385/215699322-a54dfaf2-5fb3-4a6b-8888-3639dc67429e.png)

After a reboot Teslalogger tries to calculate the CO2 for the country you have charged your car. After this feature will enter release state, it will be calculated every night.

## Where is the data from?
It is mainly based on the free data from [ENTSO-E](https://www.entsoe.eu/) and Fraunhofer's https://www.energy-charts.info/ . 
The production of the different energy sources will be multiplied with a spezific gCO2eq/kWh based on UNECE / IPCC / INCER ACV.

## How is it calculated?
Energy imported from other countries will be calculated with the avg CO2/kWh from 2021 to prevent a infinite loop (Germany is exporting to Swizerland, Switzerland to Austria and Austria back to Germany).
Hydro pumped storage is not taken into account because the stored energy might be from whenever and whereever. 

## Known limitations
The CO2 calculation is made only for the starting point of the charge. A charging over many hours might be a little off. In a future update we will fix this
As the data is manly based on entsoe, the supported countries are mainly in Europe.
Italiy's data might be off, because they import an huge amount of energy from other counties due to their nuclear power phase-out. 
As mention before energy import is calculated with average from 2021 and the weighting because of the huge import is high the data might be off.

## FAQ:
- Why is Teslalogger's calculation a little bit off to the calculation of https://www.electricitymaps.com/ ? Electricitymaps doesn't distinguish between brown coal and hard coal. So Teslalogger is more accurate.
- Why is my solar not taken into account? Because it makes no difference whether you consume the electricity yourself or others consume it.
- Why is country xxx not supported? Because we might not have data for this country. If there is a free source and many Teslalogger users, we might support it in the future.
