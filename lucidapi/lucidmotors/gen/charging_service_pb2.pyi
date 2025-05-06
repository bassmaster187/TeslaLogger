from . import vehicle_state_service_pb2 as _vehicle_state_service_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ImageCategory(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    IMAGE_CATEGORY_UNKNOWN: _ClassVar[ImageCategory]
    IMAGE_CATEGORY_OPERATOR: _ClassVar[ImageCategory]

class ChargingStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CHARGING_STATUS_UNKNOWN: _ClassVar[ChargingStatus]
    CHARGING_STATUS_CHARGING: _ClassVar[ChargingStatus]

class ConnectorStandard(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CONNECTOR_STANDARD_UNKNOWN: _ClassVar[ConnectorStandard]
    CONNECTOR_STANDARD_IEC_62196_T1_COMBO: _ClassVar[ConnectorStandard]

class ConnectorFormat(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CONNECTOR_FORMAT_UNKNOWN: _ClassVar[ConnectorFormat]
    CONNECTOR_FORMAT_CABLE: _ClassVar[ConnectorFormat]

class PowerType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    POWER_TYPE_UNKNOWN: _ClassVar[PowerType]
    POWER_TYPE_DC: _ClassVar[PowerType]

class FeeName(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    FEE_NAME_UNKNOWN: _ClassVar[FeeName]
    FEE_NAME_TAX: _ClassVar[FeeName]
    FEE_NAME_PARKING_FEE: _ClassVar[FeeName]

class FeeType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    FEE_TYPE_UNKNOWN: _ClassVar[FeeType]
    FEE_TYPE_ADD_ON_FEE_FLAT: _ClassVar[FeeType]
IMAGE_CATEGORY_UNKNOWN: ImageCategory
IMAGE_CATEGORY_OPERATOR: ImageCategory
CHARGING_STATUS_UNKNOWN: ChargingStatus
CHARGING_STATUS_CHARGING: ChargingStatus
CONNECTOR_STANDARD_UNKNOWN: ConnectorStandard
CONNECTOR_STANDARD_IEC_62196_T1_COMBO: ConnectorStandard
CONNECTOR_FORMAT_UNKNOWN: ConnectorFormat
CONNECTOR_FORMAT_CABLE: ConnectorFormat
POWER_TYPE_UNKNOWN: PowerType
POWER_TYPE_DC: PowerType
FEE_NAME_UNKNOWN: FeeName
FEE_NAME_TAX: FeeName
FEE_NAME_PARKING_FEE: FeeName
FEE_TYPE_UNKNOWN: FeeType
FEE_TYPE_ADD_ON_FEE_FLAT: FeeType

class DateTime(_message.Message):
    __slots__ = ("seconds",)
    SECONDS_FIELD_NUMBER: _ClassVar[int]
    seconds: int
    def __init__(self, seconds: _Optional[int] = ...) -> None: ...

class Unknown(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class Image(_message.Message):
    __slots__ = ("url", "category", "type")
    URL_FIELD_NUMBER: _ClassVar[int]
    CATEGORY_FIELD_NUMBER: _ClassVar[int]
    TYPE_FIELD_NUMBER: _ClassVar[int]
    url: str
    category: ImageCategory
    type: str
    def __init__(self, url: _Optional[str] = ..., category: _Optional[_Union[ImageCategory, str]] = ..., type: _Optional[str] = ...) -> None: ...

class Operator(_message.Message):
    __slots__ = ("name", "logo")
    NAME_FIELD_NUMBER: _ClassVar[int]
    LOGO_FIELD_NUMBER: _ClassVar[int]
    name: str
    logo: Image
    def __init__(self, name: _Optional[str] = ..., logo: _Optional[_Union[Image, _Mapping]] = ...) -> None: ...

class Connector(_message.Message):
    __slots__ = ("id", "standard", "format", "power_type", "voltage", "amperage", "status", "power")
    ID_FIELD_NUMBER: _ClassVar[int]
    STANDARD_FIELD_NUMBER: _ClassVar[int]
    FORMAT_FIELD_NUMBER: _ClassVar[int]
    POWER_TYPE_FIELD_NUMBER: _ClassVar[int]
    VOLTAGE_FIELD_NUMBER: _ClassVar[int]
    AMPERAGE_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    POWER_FIELD_NUMBER: _ClassVar[int]
    id: str
    standard: ConnectorStandard
    format: ConnectorFormat
    power_type: PowerType
    voltage: int
    amperage: int
    status: ChargingStatus
    power: int
    def __init__(self, id: _Optional[str] = ..., standard: _Optional[_Union[ConnectorStandard, str]] = ..., format: _Optional[_Union[ConnectorFormat, str]] = ..., power_type: _Optional[_Union[PowerType, str]] = ..., voltage: _Optional[int] = ..., amperage: _Optional[int] = ..., status: _Optional[_Union[ChargingStatus, str]] = ..., power: _Optional[int] = ...) -> None: ...

class ChargingSession(_message.Message):
    __slots__ = ("uid", "evse_id", "status", "connectors", "coordinates", "physical_reference")
    UID_FIELD_NUMBER: _ClassVar[int]
    EVSE_ID_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    CONNECTORS_FIELD_NUMBER: _ClassVar[int]
    COORDINATES_FIELD_NUMBER: _ClassVar[int]
    PHYSICAL_REFERENCE_FIELD_NUMBER: _ClassVar[int]
    uid: str
    evse_id: str
    status: ChargingStatus
    connectors: _containers.RepeatedCompositeFieldContainer[Connector]
    coordinates: _vehicle_state_service_pb2.Location
    physical_reference: str
    def __init__(self, uid: _Optional[str] = ..., evse_id: _Optional[str] = ..., status: _Optional[_Union[ChargingStatus, str]] = ..., connectors: _Optional[_Iterable[_Union[Connector, _Mapping]]] = ..., coordinates: _Optional[_Union[_vehicle_state_service_pb2.Location, _Mapping]] = ..., physical_reference: _Optional[str] = ...) -> None: ...

class OpeningTimes(_message.Message):
    __slots__ = ("twentyfourseven",)
    TWENTYFOURSEVEN_FIELD_NUMBER: _ClassVar[int]
    twentyfourseven: bool
    def __init__(self, twentyfourseven: bool = ...) -> None: ...

class ChargingLocation(_message.Message):
    __slots__ = ("id", "name", "address", "city", "postal_code", "state", "country", "coordinates", "session", "operator", "suboperator", "timezone", "opening_times")
    ID_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    ADDRESS_FIELD_NUMBER: _ClassVar[int]
    CITY_FIELD_NUMBER: _ClassVar[int]
    POSTAL_CODE_FIELD_NUMBER: _ClassVar[int]
    STATE_FIELD_NUMBER: _ClassVar[int]
    COUNTRY_FIELD_NUMBER: _ClassVar[int]
    COORDINATES_FIELD_NUMBER: _ClassVar[int]
    SESSION_FIELD_NUMBER: _ClassVar[int]
    OPERATOR_FIELD_NUMBER: _ClassVar[int]
    SUBOPERATOR_FIELD_NUMBER: _ClassVar[int]
    TIMEZONE_FIELD_NUMBER: _ClassVar[int]
    OPENING_TIMES_FIELD_NUMBER: _ClassVar[int]
    id: str
    name: str
    address: str
    city: str
    postal_code: str
    state: str
    country: str
    coordinates: _vehicle_state_service_pb2.Location
    session: ChargingSession
    operator: Operator
    suboperator: Operator
    timezone: str
    opening_times: OpeningTimes
    def __init__(self, id: _Optional[str] = ..., name: _Optional[str] = ..., address: _Optional[str] = ..., city: _Optional[str] = ..., postal_code: _Optional[str] = ..., state: _Optional[str] = ..., country: _Optional[str] = ..., coordinates: _Optional[_Union[_vehicle_state_service_pb2.Location, _Mapping]] = ..., session: _Optional[_Union[ChargingSession, _Mapping]] = ..., operator: _Optional[_Union[Operator, _Mapping]] = ..., suboperator: _Optional[_Union[Operator, _Mapping]] = ..., timezone: _Optional[str] = ..., opening_times: _Optional[_Union[OpeningTimes, _Mapping]] = ...) -> None: ...

class Fee(_message.Message):
    __slots__ = ("name", "description", "type")
    NAME_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    TYPE_FIELD_NUMBER: _ClassVar[int]
    name: FeeName
    description: str
    type: FeeType
    def __init__(self, name: _Optional[_Union[FeeName, str]] = ..., description: _Optional[str] = ..., type: _Optional[_Union[FeeType, str]] = ...) -> None: ...

class Cdr(_message.Message):
    __slots__ = ("id", "start_datetime", "stop_datetime", "auth_id", "total_energy", "total_parking_time", "location", "total_time", "add_on_fee", "charge_time", "idle_time", "currency")
    ID_FIELD_NUMBER: _ClassVar[int]
    START_DATETIME_FIELD_NUMBER: _ClassVar[int]
    STOP_DATETIME_FIELD_NUMBER: _ClassVar[int]
    AUTH_ID_FIELD_NUMBER: _ClassVar[int]
    TOTAL_ENERGY_FIELD_NUMBER: _ClassVar[int]
    TOTAL_PARKING_TIME_FIELD_NUMBER: _ClassVar[int]
    LOCATION_FIELD_NUMBER: _ClassVar[int]
    TOTAL_TIME_FIELD_NUMBER: _ClassVar[int]
    ADD_ON_FEE_FIELD_NUMBER: _ClassVar[int]
    CHARGE_TIME_FIELD_NUMBER: _ClassVar[int]
    IDLE_TIME_FIELD_NUMBER: _ClassVar[int]
    CURRENCY_FIELD_NUMBER: _ClassVar[int]
    id: str
    start_datetime: DateTime
    stop_datetime: DateTime
    auth_id: str
    total_energy: float
    total_parking_time: float
    location: ChargingLocation
    total_time: float
    add_on_fee: _containers.RepeatedCompositeFieldContainer[Fee]
    charge_time: float
    idle_time: float
    currency: str
    def __init__(self, id: _Optional[str] = ..., start_datetime: _Optional[_Union[DateTime, _Mapping]] = ..., stop_datetime: _Optional[_Union[DateTime, _Mapping]] = ..., auth_id: _Optional[str] = ..., total_energy: _Optional[float] = ..., total_parking_time: _Optional[float] = ..., location: _Optional[_Union[ChargingLocation, _Mapping]] = ..., total_time: _Optional[float] = ..., add_on_fee: _Optional[_Iterable[_Union[Fee, _Mapping]]] = ..., charge_time: _Optional[float] = ..., idle_time: _Optional[float] = ..., currency: _Optional[str] = ...) -> None: ...

class GetCdrRequest(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class GetCdrResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class GetCdrsRequest(_message.Message):
    __slots__ = ("ema_id", "limit")
    EMA_ID_FIELD_NUMBER: _ClassVar[int]
    LIMIT_FIELD_NUMBER: _ClassVar[int]
    ema_id: str
    limit: int
    def __init__(self, ema_id: _Optional[str] = ..., limit: _Optional[int] = ...) -> None: ...

class GetCdrsResponse(_message.Message):
    __slots__ = ("cdr",)
    CDR_FIELD_NUMBER: _ClassVar[int]
    cdr: _containers.RepeatedCompositeFieldContainer[Cdr]
    def __init__(self, cdr: _Optional[_Iterable[_Union[Cdr, _Mapping]]] = ...) -> None: ...

class GetLocationsBoxRequest(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class GetLocationsBoxResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class ChargingLocationDistance(_message.Message):
    __slots__ = ("location", "distance")
    LOCATION_FIELD_NUMBER: _ClassVar[int]
    DISTANCE_FIELD_NUMBER: _ClassVar[int]
    location: ChargingLocation
    distance: int
    def __init__(self, location: _Optional[_Union[ChargingLocation, _Mapping]] = ..., distance: _Optional[int] = ...) -> None: ...

class GetLocationsByRadiusRequest(_message.Message):
    __slots__ = ("origin", "radius")
    ORIGIN_FIELD_NUMBER: _ClassVar[int]
    RADIUS_FIELD_NUMBER: _ClassVar[int]
    origin: _vehicle_state_service_pb2.Location
    radius: int
    def __init__(self, origin: _Optional[_Union[_vehicle_state_service_pb2.Location, _Mapping]] = ..., radius: _Optional[int] = ...) -> None: ...

class GetLocationsByRadiusResponse(_message.Message):
    __slots__ = ("locations",)
    LOCATIONS_FIELD_NUMBER: _ClassVar[int]
    locations: _containers.RepeatedCompositeFieldContainer[ChargingLocationDistance]
    def __init__(self, locations: _Optional[_Iterable[_Union[ChargingLocationDistance, _Mapping]]] = ...) -> None: ...

class GetTariffRequest(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class GetTariffResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class RegisterRFIDRequest(_message.Message):
    __slots__ = ("ema_id", "rfid_token")
    EMA_ID_FIELD_NUMBER: _ClassVar[int]
    RFID_TOKEN_FIELD_NUMBER: _ClassVar[int]
    ema_id: str
    rfid_token: str
    def __init__(self, ema_id: _Optional[str] = ..., rfid_token: _Optional[str] = ...) -> None: ...

class RegisterRFIDResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class StartSessionRequest(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class StartSessionResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class StopSessionRequest(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class StopSessionResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...
