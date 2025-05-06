"""Constants for the Lucid Motors API."""

from enum import Enum


class Region(Enum):
    US = 'us'
    SA = 'sa'
    EU = 'eu'

    @property
    def api_domain(self) -> str:
        return MOBILE_API_REGIONS[self]


# Before Lucid was Lucid, it was Atieva. They still use their old domain in
# their mobile apps for now.
MOBILE_API_REGIONS = {
    Region.US: "mobile.deneb.prod.infotainment.pdx.atieva.com",
    Region.SA: "mobile.ksap.prod.do.lucidcars.io",
    Region.EU: "mobile.do.prod.eu.lcid.io",
}

# Min/max temperatures for HVAC preconditioning. The API rejects anything
# outside of this range. Values are in Celsius.
# StatusCode.INVALID_ARGUMENT:
# SetCabinTemperature failed
# temperature [15.0 <= x <= 30.0] is required with HVAC_PRECONDITION in
# SetCabinTemperatureRequest
PRECONDITION_TEMPERATURE_MIN = 15.0
PRECONDITION_TEMPERATURE_MAX = 30.0

# Maximum tire pressure value (bar). The API returns this when it doesn't have
# a current value.
TIRE_PRESSURE_MAX = 6.3750000949949026

# Maximum charge session time (minutes). The API returns this when there is no
# active charge session.
CHARGE_SESSION_TIME_MAX = 65535
