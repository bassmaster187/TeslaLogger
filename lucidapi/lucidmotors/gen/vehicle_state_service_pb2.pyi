from google.protobuf.internal import containers as _containers
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class AccessLevel(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ACCESS_LEVEL_UNKNOWN: _ClassVar[AccessLevel]
    ACCESS_LEVEL_PREDELIVERY_OWNER: _ClassVar[AccessLevel]
    ACCESS_LEVEL_PRIMARY_OWNER: _ClassVar[AccessLevel]

class Model(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    MODEL_UNKNOWN: _ClassVar[Model]
    MODEL_AIR: _ClassVar[Model]
    MODEL_GRAVITY: _ClassVar[Model]

class ModelVariant(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    MODEL_VARIANT_UNKNOWN: _ClassVar[ModelVariant]
    MODEL_VARIANT_DREAM_EDITION: _ClassVar[ModelVariant]
    MODEL_VARIANT_GRAND_TOURING: _ClassVar[ModelVariant]
    MODEL_VARIANT_TOURING: _ClassVar[ModelVariant]
    MODEL_VARIANT_PURE: _ClassVar[ModelVariant]
    MODEL_VARIANT_SAPPHIRE: _ClassVar[ModelVariant]
    MODEL_VARIANT_HYPER: _ClassVar[ModelVariant]
    MODEL_VARIANT_EXECUTIVE: _ClassVar[ModelVariant]

class PaintColor(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    PAINT_COLOR_UNKNOWN: _ClassVar[PaintColor]
    PAINT_COLOR_EUREKA_GOLD: _ClassVar[PaintColor]
    PAINT_COLOR_STELLAR_WHITE: _ClassVar[PaintColor]
    PAINT_COLOR_INFINITE_BLACK: _ClassVar[PaintColor]
    PAINT_COLOR_COSMOS_SILVER: _ClassVar[PaintColor]
    PAINT_COLOR_QUANTUM_GREY: _ClassVar[PaintColor]
    PAINT_COLOR_ZENITH_RED: _ClassVar[PaintColor]
    PAINT_COLOR_FATHOM_BLUE: _ClassVar[PaintColor]
    PAINT_COLOR_CUSTOM: _ClassVar[PaintColor]
    PAINT_COLOR_SAPPHIRE_BLUE: _ClassVar[PaintColor]

class Look(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    LOOK_UNKNOWN: _ClassVar[Look]
    LOOK_PLATINUM: _ClassVar[Look]
    LOOK_STEALTH: _ClassVar[Look]
    LOOK_SAPPHIRE: _ClassVar[Look]
    LOOK_SURFRIDER: _ClassVar[Look]
    LOOK_BASE: _ClassVar[Look]

class Wheels(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    WHEELS_UNKNOWN: _ClassVar[Wheels]
    WHEELS_DREAM: _ClassVar[Wheels]
    WHEELS_BLADE: _ClassVar[Wheels]
    WHEELS_LITE: _ClassVar[Wheels]
    WHEELS_RANGE: _ClassVar[Wheels]
    WHEELS_SPORT_STEALTH: _ClassVar[Wheels]
    WHEELS_BLADE_GRAPHITE: _ClassVar[Wheels]
    WHEELS_LITE_STEALTH: _ClassVar[Wheels]
    WHEELS_SPORT_LUSTER: _ClassVar[Wheels]
    WHEELS_SAPPHIRE_PACKAGE: _ClassVar[Wheels]
    WHEELS_RANGE_STEALTH: _ClassVar[Wheels]

class SubscriptionStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SUBSCRIPTION_STATUS_UNKNOWN: _ClassVar[SubscriptionStatus]
    SUBSCRIPTION_STATUS_CURRENT: _ClassVar[SubscriptionStatus]

class ChargingAccountStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CHARGING_ACCOUNT_STATUS_UNKNOWN: _ClassVar[ChargingAccountStatus]
    CHARGING_ACCOUNT_STATUS_DISABLED: _ClassVar[ChargingAccountStatus]
    CHARGING_ACCOUNT_STATUS_ENROLLED: _ClassVar[ChargingAccountStatus]

class ChargingVendor(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CHARGING_VENDOR_UNKNOWN: _ClassVar[ChargingVendor]
    CHARGING_VENDOR_ELECTRIFY_AMERICA: _ClassVar[ChargingVendor]
    CHARGING_VENDOR_BOSCH: _ClassVar[ChargingVendor]

class Edition(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    EDITION_UNKNOWN: _ClassVar[Edition]
    EDITION_PERFORMANCE: _ClassVar[Edition]
    EDITION_RANGE: _ClassVar[Edition]
    EDITION_STANDARD: _ClassVar[Edition]

class BatteryType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    BATTERY_TYPE_UNKNOWN: _ClassVar[BatteryType]
    BATTERY_TYPE_01: _ClassVar[BatteryType]
    BATTERY_TYPE_02: _ClassVar[BatteryType]
    BATTERY_TYPE_03: _ClassVar[BatteryType]
    BATTERY_TYPE_04: _ClassVar[BatteryType]
    BATTERY_TYPE_05: _ClassVar[BatteryType]
    BATTERY_TYPE_06: _ClassVar[BatteryType]
    BATTERY_TYPE_07: _ClassVar[BatteryType]
    BATTERY_TYPE_08: _ClassVar[BatteryType]
    BATTERY_TYPE_09: _ClassVar[BatteryType]
    BATTERY_TYPE_25: _ClassVar[BatteryType]

class Interior(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    INTERIOR_UNKNOWN: _ClassVar[Interior]
    INTERIOR_SANTA_CRUZ: _ClassVar[Interior]
    INTERIOR_TAHOE: _ClassVar[Interior]
    INTERIOR_MOJAVE: _ClassVar[Interior]
    INTERIOR_SANTA_MONICA: _ClassVar[Interior]

class StrutType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    STRUT_TYPE_UNKNOWN: _ClassVar[StrutType]
    STRUT_TYPE_GAS: _ClassVar[StrutType]
    STRUT_TYPE_POWER: _ClassVar[StrutType]

class RoofType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ROOF_TYPE_UNKNOWN: _ClassVar[RoofType]
    ROOF_TYPE_GLASS_CANOPY: _ClassVar[RoofType]
    ROOF_TYPE_METAL: _ClassVar[RoofType]

class FrontSeatsVentilationAvailability(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    FRONT_SEATS_VENTILATION_UNKNOWN: _ClassVar[FrontSeatsVentilationAvailability]
    FRONT_SEATS_VENTILATION_UNAVAILABLE: _ClassVar[FrontSeatsVentilationAvailability]
    FRONT_SEATS_VENTILATION_AVAILABLE: _ClassVar[FrontSeatsVentilationAvailability]

class FrontSeatsHeatingAvailability(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    FRONT_SEATS_HEATING_UNKNOWN: _ClassVar[FrontSeatsHeatingAvailability]
    FRONT_SEATS_HEATING_UNAVAILABLE: _ClassVar[FrontSeatsHeatingAvailability]
    FRONT_SEATS_HEATING_AVAILABLE: _ClassVar[FrontSeatsHeatingAvailability]

class SecondRowHeatedSeatsAvailability(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SECOND_ROW_HEATED_SEATS_UNKNOWN: _ClassVar[SecondRowHeatedSeatsAvailability]
    SECOND_ROW_HEATED_SEATS_UNAVAILABLE: _ClassVar[SecondRowHeatedSeatsAvailability]
    SECOND_ROW_HEATED_SEATS_AVAILABLE: _ClassVar[SecondRowHeatedSeatsAvailability]

class HeatedSteeringWheelAvailability(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    HEATED_STEERING_WHEEL_UNKNOWN: _ClassVar[HeatedSteeringWheelAvailability]
    HEATED_STEERING_WHEEL_UNAVAILABLE: _ClassVar[HeatedSteeringWheelAvailability]
    HEATED_STEERING_WHEEL_AVAILABLE: _ClassVar[HeatedSteeringWheelAvailability]

class RearSeatConfig(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    REAR_SEAT_CONFIG_UNKNOWN: _ClassVar[RearSeatConfig]
    REAR_SEAT_CONFIG_5_SEAT: _ClassVar[RearSeatConfig]
    REAR_SEAT_CONFIG_6_SEAT: _ClassVar[RearSeatConfig]
    REAR_SEAT_CONFIG_7_SEAT: _ClassVar[RearSeatConfig]

class WarningState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    WARNING_UNKNOWN: _ClassVar[WarningState]
    WARNING_OFF: _ClassVar[WarningState]
    WARNING_ON: _ClassVar[WarningState]

class BatteryPreconStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    BATTERY_PRECON_UNKNOWN: _ClassVar[BatteryPreconStatus]
    BATTERY_PRECON_OFF: _ClassVar[BatteryPreconStatus]
    BATTERY_PRECON_ON: _ClassVar[BatteryPreconStatus]
    BATTERY_PRECON_UNAVAILABLE: _ClassVar[BatteryPreconStatus]

class BatteryCellType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    BATTERY_CELL_TYPE_UNKNOWN: _ClassVar[BatteryCellType]
    BATTERY_CELL_TYPE_LG_M48: _ClassVar[BatteryCellType]
    BATTERY_CELL_TYPE_SDI_50G: _ClassVar[BatteryCellType]
    BATTERY_CELL_TYPE_PANA_2170M: _ClassVar[BatteryCellType]
    BATTERY_CELL_TYPE_SDI_50GV2: _ClassVar[BatteryCellType]

class BatteryPackType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    BATTERY_PACK_TYPE_UNKNOWN: _ClassVar[BatteryPackType]
    BATTERY_PACK_TYPE_AIR_22: _ClassVar[BatteryPackType]
    BATTERY_PACK_TYPE_AIR_18: _ClassVar[BatteryPackType]
    BATTERY_PACK_TYPE_AIR_16: _ClassVar[BatteryPackType]

class PowerState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    POWER_STATE_UNKNOWN: _ClassVar[PowerState]
    POWER_STATE_SLEEP: _ClassVar[PowerState]
    POWER_STATE_WINK: _ClassVar[PowerState]
    POWER_STATE_ACCESSORY: _ClassVar[PowerState]
    POWER_STATE_DRIVE: _ClassVar[PowerState]
    POWER_STATE_LIVE_CHARGE: _ClassVar[PowerState]
    POWER_STATE_SLEEP_CHARGE: _ClassVar[PowerState]
    POWER_STATE_LIVE_UPDATE: _ClassVar[PowerState]
    POWER_STATE_CLOUD_2: _ClassVar[PowerState]
    POWER_STATE_MONITOR: _ClassVar[PowerState]

class LockState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    LOCK_STATE_UNKNOWN: _ClassVar[LockState]
    LOCK_STATE_UNLOCKED: _ClassVar[LockState]
    LOCK_STATE_LOCKED: _ClassVar[LockState]

class DoorState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    DOOR_STATE_UNKNOWN: _ClassVar[DoorState]
    DOOR_STATE_OPEN: _ClassVar[DoorState]
    DOOR_STATE_CLOSED: _ClassVar[DoorState]
    DOOR_STATE_AJAR: _ClassVar[DoorState]

class WalkawayState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    WALKAWAY_UNKNOWN: _ClassVar[WalkawayState]
    WALKAWAY_ACTIVE: _ClassVar[WalkawayState]
    WALKAWAY_DISABLE: _ClassVar[WalkawayState]

class AccessRequest(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ACCESS_REQUEST_UNKNOWN: _ClassVar[AccessRequest]
    ACCESS_REQUEST_ACTIVE: _ClassVar[AccessRequest]
    ACCESS_REQUEST_PASSIVE: _ClassVar[AccessRequest]
    ACCESS_REQUEST_PASSIVE_DRIVER: _ClassVar[AccessRequest]
    ACCESS_REQUEST_PASSIVE_TEMP_DISABLED: _ClassVar[AccessRequest]

class KeyfobBatteryStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    KEYFOB_BATTERY_STATUS_UNKNOWN: _ClassVar[KeyfobBatteryStatus]
    KEYFOB_BATTERY_STATUS_LOW: _ClassVar[KeyfobBatteryStatus]
    KEYFOB_BATTERY_STATUS_SUFFICIENT: _ClassVar[KeyfobBatteryStatus]

class AllWindowPosition(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ALL_WINDOW_POSITION_UNKNOWN: _ClassVar[AllWindowPosition]
    ALL_WINDOW_POSITION_IDLE: _ClassVar[AllWindowPosition]
    ALL_WINDOW_POSITION_OPEN: _ClassVar[AllWindowPosition]
    ALL_WINDOW_POSITION_CLOSED: _ClassVar[AllWindowPosition]
    ALL_WINDOW_POSITION_ERROR: _ClassVar[AllWindowPosition]

class WindowPositionStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    WINDOW_POSITION_STATUS_UNKNOWN: _ClassVar[WindowPositionStatus]
    WINDOW_POSITION_STATUS_FULLY_CLOSED: _ClassVar[WindowPositionStatus]
    WINDOW_POSITION_STATUS_ABOVE_SHORT_DROP_POSITION: _ClassVar[WindowPositionStatus]
    WINDOW_POSITION_STATUS_SHORT_DROP_POSITION: _ClassVar[WindowPositionStatus]
    WINDOW_POSITION_STATUS_BELOW_SHORT_DROP_POSITION: _ClassVar[WindowPositionStatus]
    WINDOW_POSITION_STATUS_FULLY_OPEN: _ClassVar[WindowPositionStatus]
    WINDOW_POSITION_STATUS_BETWEEN_FULLY_CLOSED_AND_SHORT_DROP_DOWN: _ClassVar[WindowPositionStatus]
    WINDOW_POSITION_STATUS_BETWEEN_SHORT_DROP_DOWN_AND_FULLY_OPEN: _ClassVar[WindowPositionStatus]

class MirrorFoldState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    MIRROR_FOLD_STATE_UNKNOWN: _ClassVar[MirrorFoldState]
    MIRROR_FOLD_STATE_IDLE: _ClassVar[MirrorFoldState]
    MIRROR_FOLD_STATE_FOLDED_OUT: _ClassVar[MirrorFoldState]
    MIRROR_FOLD_STATE_FOLDED_IN: _ClassVar[MirrorFoldState]

class LivingObjectDetectionStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    LIVING_OBJECT_DETECTION_STATUS_UNKNOWN: _ClassVar[LivingObjectDetectionStatus]
    LIVING_OBJECT_DETECTION_STATUS_DISABLED: _ClassVar[LivingObjectDetectionStatus]
    LIVING_OBJECT_DETECTION_STATUS_NOT_ACTIVE: _ClassVar[LivingObjectDetectionStatus]
    LIVING_OBJECT_DETECTION_STATUS_LEVEL_1_WARNING: _ClassVar[LivingObjectDetectionStatus]
    LIVING_OBJECT_DETECTION_STATUS_LEVEL_2_WARNING: _ClassVar[LivingObjectDetectionStatus]
    LIVING_OBJECT_DETECTION_STATUS_LEVEL_3_WARNING: _ClassVar[LivingObjectDetectionStatus]

class LightState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    LIGHT_STATE_REALLY_UNKNOWN: _ClassVar[LightState]
    LIGHT_STATE_OFF: _ClassVar[LightState]
    LIGHT_STATE_ON: _ClassVar[LightState]
    LIGHT_STATE_UNKNOWN: _ClassVar[LightState]

class LightAction(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    LIGHT_ACTION_UNKNOWN: _ClassVar[LightAction]
    LIGHT_ACTION_FLASH: _ClassVar[LightAction]
    LIGHT_ACTION_ON: _ClassVar[LightAction]
    LIGHT_ACTION_OFF: _ClassVar[LightAction]
    LIGHT_ACTION_HAZARD_ON: _ClassVar[LightAction]
    LIGHT_ACTION_HAZARD_OFF: _ClassVar[LightAction]

class WelcomeAction(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    WELCOME_ACTION_UNKNOWN: _ClassVar[WelcomeAction]
    WELCOME_ACTION_UNLOCK: _ClassVar[WelcomeAction]
    WELCOME_ACTION_DEPARTURE: _ClassVar[WelcomeAction]
    WELCOME_ACTION_LIGHTS: _ClassVar[WelcomeAction]
    WELCOME_ACTION_BLINKERS: _ClassVar[WelcomeAction]

class TirePressureSensorDefective(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    TIRE_PRESSURE_SENSOR_DEFECTIVE_UNKNOWN: _ClassVar[TirePressureSensorDefective]
    TIRE_PRESSURE_SENSOR_DEFECTIVE_OFF: _ClassVar[TirePressureSensorDefective]
    TIRE_PRESSURE_SENSOR_DEFECTIVE_ON: _ClassVar[TirePressureSensorDefective]

class ChargeState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CHARGE_STATE_UNKNOWN: _ClassVar[ChargeState]
    CHARGE_STATE_NOT_CONNECTED: _ClassVar[ChargeState]
    CHARGE_STATE_CABLE_CONNECTED: _ClassVar[ChargeState]
    CHARGE_STATE_ESTABLISHING_SESSION: _ClassVar[ChargeState]
    CHARGE_STATE_AUTHORIZING_PNC: _ClassVar[ChargeState]
    CHARGE_STATE_AUTHORIZING_EXTERNAL: _ClassVar[ChargeState]
    CHARGE_STATE_AUTHORIZED: _ClassVar[ChargeState]
    CHARGE_STATE_CHARGER_PREPARATION: _ClassVar[ChargeState]
    CHARGE_STATE_CHARGING: _ClassVar[ChargeState]
    CHARGE_STATE_CHARGING_END_OK: _ClassVar[ChargeState]
    CHARGE_STATE_CHARGING_STOPPED: _ClassVar[ChargeState]
    CHARGE_STATE_EVSE_MALFUNCTION: _ClassVar[ChargeState]
    CHARGE_STATE_DISCHARGING: _ClassVar[ChargeState]
    CHARGE_STATE_DISCHARGING_COMPLETED: _ClassVar[ChargeState]
    CHARGE_STATE_DISCHARGING_STOPPED: _ClassVar[ChargeState]
    CHARGE_STATE_DISCHARGING_FAULT: _ClassVar[ChargeState]
    CHARGE_STATE_DISCHARGING_UNAVAILABLE: _ClassVar[ChargeState]

class ScheduledChargeState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SCHEDULED_CHARGE_STATE_UNKNOWN: _ClassVar[ScheduledChargeState]
    SCHEDULED_CHARGE_STATE_IDLE: _ClassVar[ScheduledChargeState]
    SCHEDULED_CHARGE_STATE_SCHEDULED_TO_CHARGE: _ClassVar[ScheduledChargeState]
    SCHEDULED_CHARGE_STATE_REQUEST_TO_CHARGE: _ClassVar[ScheduledChargeState]

class ScheduledChargeUnavailableState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SCHEDULED_CHARGE_UNAVAILABLE_UNKNOWN: _ClassVar[ScheduledChargeUnavailableState]
    SCHEDULED_CHARGE_UNAVAILABLE_NO_REQUEST: _ClassVar[ScheduledChargeUnavailableState]

class EnergyType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ENERGY_TYPE_UNKNOWN: _ClassVar[EnergyType]
    ENERGY_TYPE_AC: _ClassVar[EnergyType]
    ENERGY_TYPE_DC: _ClassVar[EnergyType]
    ENERGY_TYPE_V2V: _ClassVar[EnergyType]

class MobileDischargingCommand(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    MOBILE_DISCHARGING_COMMAND_UNKNOWN: _ClassVar[MobileDischargingCommand]
    MOBILE_DISCHARGING_COMMAND_START_DISCHARGING: _ClassVar[MobileDischargingCommand]

class ChargingSessionRestartAllowed(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CHARGING_SESSION_RESTART_ALLOWED_STATUS_UNKNOWN: _ClassVar[ChargingSessionRestartAllowed]
    CHARGING_SESSION_RESTART_ALLOWED_STATUS_IDLE: _ClassVar[ChargingSessionRestartAllowed]
    CHARGING_SESSION_RESTART_ALLOWED_STATUS_NOT_ALLOWED: _ClassVar[ChargingSessionRestartAllowed]
    CHARGING_SESSION_RESTART_ALLOWED_STATUS_ALLOWED: _ClassVar[ChargingSessionRestartAllowed]

class EaPncStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    EA_PNC_STATUS_UNKNOWN: _ClassVar[EaPncStatus]
    EA_PNC_STATUS_IDLE: _ClassVar[EaPncStatus]
    EA_PNC_STATUS_ENABLE: _ClassVar[EaPncStatus]
    EA_PNC_STATUS_DISABLE: _ClassVar[EaPncStatus]
    EA_PNC_STATUS_NO_NOTIFICATION: _ClassVar[EaPncStatus]

class AcOutletUnavailableReason(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    AC_OUTLET_UNAVAILABLE_REASON_UNKNOWN: _ClassVar[AcOutletUnavailableReason]
    AC_OUTLET_UNAVAILABLE_REASON_NONE: _ClassVar[AcOutletUnavailableReason]
    AC_OUTLET_UNAVAILABLE_REASON_WARNING_FAULT: _ClassVar[AcOutletUnavailableReason]
    AC_OUTLET_UNAVAILABLE_REASON_CRITICAL_FAULT: _ClassVar[AcOutletUnavailableReason]
    AC_OUTLET_UNAVAILABLE_REASON_CHARGING: _ClassVar[AcOutletUnavailableReason]
    AC_OUTLET_UNAVAILABLE_REASON_LOW_VEH_RANGE: _ClassVar[AcOutletUnavailableReason]
    AC_OUTLET_UNAVAILABLE_REASON_WARNING_FAULT_CAMP: _ClassVar[AcOutletUnavailableReason]
    AC_OUTLET_UNAVAILABLE_REASON_CRITICAL_FAULT_CAMP: _ClassVar[AcOutletUnavailableReason]
    AC_OUTLET_UNAVAILABLE_REASON_LOW_VEH_RANGE_CAMP: _ClassVar[AcOutletUnavailableReason]

class UpdateState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    UPDATE_STATE_UNKNOWN: _ClassVar[UpdateState]
    UPDATE_STATE_IN_PROGRESS: _ClassVar[UpdateState]
    UPDATE_STATE_SUCCESS: _ClassVar[UpdateState]
    UPDATE_STATE_FAILED: _ClassVar[UpdateState]
    UPDATE_FAILED_DRIVE_ALLOWED: _ClassVar[UpdateState]
    UPDATE_SUCCESS_WITH_WARNINGS: _ClassVar[UpdateState]
    UPDATE_NOTSTARTED_WITH_WARNINGS: _ClassVar[UpdateState]

class UpdateAvailability(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    UPDATE_AVAILABILITY_UNKNOWN: _ClassVar[UpdateAvailability]
    UPDATE_AVAILABLE: _ClassVar[UpdateAvailability]

class TcuDownloadStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    TCU_SOFTWARE_DOWNLOAD_STATUS_UNKNOWN: _ClassVar[TcuDownloadStatus]
    TCU_SOFTWARE_DOWNLOAD_STATUS_IDLE: _ClassVar[TcuDownloadStatus]
    TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOADING: _ClassVar[TcuDownloadStatus]
    TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOAD_PAUSED: _ClassVar[TcuDownloadStatus]
    TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOAD_COMPLETE: _ClassVar[TcuDownloadStatus]
    TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOAD_FAILED: _ClassVar[TcuDownloadStatus]
    TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOAD_CANCELED: _ClassVar[TcuDownloadStatus]

class AlarmStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ALARM_STATUS_UNKNOWN: _ClassVar[AlarmStatus]
    ALARM_STATUS_DISARMED: _ClassVar[AlarmStatus]
    ALARM_STATUS_ARMED: _ClassVar[AlarmStatus]

class AlarmMode(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ALARM_MODE_UNKNOWN: _ClassVar[AlarmMode]
    ALARM_MODE_OFF: _ClassVar[AlarmMode]
    ALARM_MODE_ON: _ClassVar[AlarmMode]
    ALARM_MODE_SILENT: _ClassVar[AlarmMode]

class CloudConnectionState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CLOUD_CONNECTION_UNKNOWN: _ClassVar[CloudConnectionState]
    CLOUD_CONNECTION_CONNECTED: _ClassVar[CloudConnectionState]
    CLOUD_CONNECTION_DISCONNECTED: _ClassVar[CloudConnectionState]

class KeylessDrivingState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    KEYLESS_DRIVING_UNKNOWN: _ClassVar[KeylessDrivingState]
    KEYLESS_DRIVING_ON: _ClassVar[KeylessDrivingState]
    KEYLESS_DRIVING_OFF: _ClassVar[KeylessDrivingState]

class HvacPower(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    HVAC_POWER_UNKNOWN: _ClassVar[HvacPower]
    HVAC_ON: _ClassVar[HvacPower]
    HVAC_OFF: _ClassVar[HvacPower]
    HVAC_PRECONDITION: _ClassVar[HvacPower]
    HVAC_KEEP_TEMP: _ClassVar[HvacPower]

class DefrostState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    DEFROST_STATE_UNKNOWN: _ClassVar[DefrostState]
    DEFROST_ON: _ClassVar[DefrostState]
    DEFROST_OFF: _ClassVar[DefrostState]

class HvacPreconditionStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    HVAC_PRECONDITION_STATUS_UNKNOWN: _ClassVar[HvacPreconditionStatus]
    HVAC_PRECONDITION_STATUS_STILL_ACTIVE: _ClassVar[HvacPreconditionStatus]
    HVAC_PRECONDITION_STATUS_TEMP_REACHED: _ClassVar[HvacPreconditionStatus]
    HVAC_PRECONDITION_STATUS_TIMEOUT: _ClassVar[HvacPreconditionStatus]
    HVAC_PRECONDITION_STATUS_USER_INPUT: _ClassVar[HvacPreconditionStatus]
    HVAC_PRECONDITION_STATUS_NOT_ACTIVE_PRECONDITION: _ClassVar[HvacPreconditionStatus]

class KeepClimateStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    KEEP_CLIMATE_STATUS_UNKNOWN: _ClassVar[KeepClimateStatus]
    KEEP_CLIMATE_STATUS_INACTIVE: _ClassVar[KeepClimateStatus]
    KEEP_CLIMATE_STATUS_ENABLED: _ClassVar[KeepClimateStatus]
    KEEP_CLIMATE_STATUS_CANCELED: _ClassVar[KeepClimateStatus]
    KEEP_CLIMATE_STATUS_PET_MODE_ON: _ClassVar[KeepClimateStatus]

class KeepClimateCondition(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    KEEP_CLIMATE_CONDITION_UNKNOWN: _ClassVar[KeepClimateCondition]

class SeatClimateMode(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SEAT_CLIMATE_MODE_UNKNOWN: _ClassVar[SeatClimateMode]
    SEAT_CLIMATE_MODE_OFF: _ClassVar[SeatClimateMode]
    SEAT_CLIMATE_MODE_LOW: _ClassVar[SeatClimateMode]
    SEAT_CLIMATE_MODE_MEDIUM: _ClassVar[SeatClimateMode]
    SEAT_CLIMATE_MODE_HIGH: _ClassVar[SeatClimateMode]

class SteeringHeaterStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    STEERING_HEATER_STATUS_UNKNOWN: _ClassVar[SteeringHeaterStatus]
    STEERING_HEATER_STATUS_OFF: _ClassVar[SteeringHeaterStatus]
    STEERING_HEATER_STATUS_ON: _ClassVar[SteeringHeaterStatus]

class SyncSet(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SYNC_SET_UNKNOWN: _ClassVar[SyncSet]
    SYNC_SET_OFF: _ClassVar[SyncSet]
    SYNC_SET_ON: _ClassVar[SyncSet]

class RearWindowHeatingStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    REAR_WINDOW_HEATING_STATUS_UNKNOWN: _ClassVar[RearWindowHeatingStatus]
    REAR_WINDOW_HEATING_STATUS_OFF: _ClassVar[RearWindowHeatingStatus]
    REAR_WINDOW_HEATING_STATUS_ON: _ClassVar[RearWindowHeatingStatus]
    REAR_WINDOW_HEATING_STATUS_OFF_LOST_COMM_WITH_DCM: _ClassVar[RearWindowHeatingStatus]
    REAR_WINDOW_HEATING_STATUS_ON_LOST_COMM_WITH_DCM: _ClassVar[RearWindowHeatingStatus]

class HvacLimited(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    HVAC_LIMITED_UNKNOWN: _ClassVar[HvacLimited]
    HVAC_LIMITED_OFF: _ClassVar[HvacLimited]
    HVAC_LIMITED_ON: _ClassVar[HvacLimited]

class DriveMode(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    DRIVE_MODE_UNKNOWN: _ClassVar[DriveMode]
    DRIVE_MODE_COMFORT: _ClassVar[DriveMode]
    DRIVE_MODE_SWIFT: _ClassVar[DriveMode]
    DRIVE_MODE_WINTER: _ClassVar[DriveMode]
    DRIVE_MODE_VALET: _ClassVar[DriveMode]
    DRIVE_MODE_SPORT_PLUS: _ClassVar[DriveMode]
    DRIVE_MODE_RESERVED_1: _ClassVar[DriveMode]
    DRIVE_MODE_RESERVED_2: _ClassVar[DriveMode]
    DRIVE_MODE_SERVICE: _ClassVar[DriveMode]
    DRIVE_MODE_LAUNCH: _ClassVar[DriveMode]
    DRIVE_MODE_FACTORY: _ClassVar[DriveMode]
    DRIVE_MODE_DEV1: _ClassVar[DriveMode]
    DRIVE_MODE_DEV2: _ClassVar[DriveMode]
    DRIVE_MODE_TRANSPORT: _ClassVar[DriveMode]
    DRIVE_MODE_SHOWROOM: _ClassVar[DriveMode]
    DRIVE_MODE_TOW: _ClassVar[DriveMode]
    DRIVE_MODE_TEST_DRIVE: _ClassVar[DriveMode]
    DRIVE_MODE_RESERVED_3: _ClassVar[DriveMode]

class PrivacyMode(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    PRIVACY_MODE_UNKNOWN: _ClassVar[PrivacyMode]
    PRIVACY_MODE_CONNECTIVITY_ENABLED: _ClassVar[PrivacyMode]
    PRIVACY_MODE_CONNECTIVITY_DISABLED: _ClassVar[PrivacyMode]

class GearPosition(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    GEAR_UNKNOWN: _ClassVar[GearPosition]
    GEAR_PARK: _ClassVar[GearPosition]
    GEAR_REVERSE: _ClassVar[GearPosition]
    GEAR_NEUTRAL: _ClassVar[GearPosition]
    GEAR_DRIVE: _ClassVar[GearPosition]

class SharedTripState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SHARED_TRIP_UNKNOWN: _ClassVar[SharedTripState]
    SHARED_TRIP_AVAILABLE: _ClassVar[SharedTripState]
    SHARED_TRIP_PROFILE_UPDATED: _ClassVar[SharedTripState]

class PanicState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    PANIC_ALARM_UNKNOWN: _ClassVar[PanicState]
    PANIC_ALARM_ON: _ClassVar[PanicState]

class TcuState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    TCU_UNKNOWN: _ClassVar[TcuState]
    TCU_SLEEP: _ClassVar[TcuState]
    TCU_DROWSY: _ClassVar[TcuState]
    TCU_FULL: _ClassVar[TcuState]
    TCU_FACTORY: _ClassVar[TcuState]
    TCU_POWER: _ClassVar[TcuState]
    TCU_OFF: _ClassVar[TcuState]

class LteType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    LTE_TYPE_UNKNOWN: _ClassVar[LteType]
    LTE_TYPE_3G: _ClassVar[LteType]
    LTE_TYPE_4G: _ClassVar[LteType]

class InternetStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    INTERNET_STATUS_UNKNOWN: _ClassVar[InternetStatus]
    INTERNET_DISCONNECTED: _ClassVar[InternetStatus]
    INTERNET_CONNECTED: _ClassVar[InternetStatus]

class MpbFaultStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    MPB_FAULT_STATUS_UNKNOWN: _ClassVar[MpbFaultStatus]
    MPB_FAULT_STATUS_NORMAL: _ClassVar[MpbFaultStatus]
    MPB_FAULT_STATUS_CRITICAL: _ClassVar[MpbFaultStatus]

class PowertrainMessage(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    POWERTRAIN_MESSAGE_UNKNOWN: _ClassVar[PowertrainMessage]
    POWERTRAIN_MESSAGE_BLANK_NO_MESSAGE: _ClassVar[PowertrainMessage]

class PowertrainNotifyStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    POWERTRAIN_NOTIFY_UNKNOWN: _ClassVar[PowertrainNotifyStatus]
    POWERTRAIN_NOTIFY_NONE: _ClassVar[PowertrainNotifyStatus]

class GeneralChargeStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    GENERAL_CHARGE_UNKNOWN: _ClassVar[GeneralChargeStatus]
    GENERAL_CHARGE_DEFAULT: _ClassVar[GeneralChargeStatus]
    GENERAL_CHARGE_DERATED_CHARGING_POWER: _ClassVar[GeneralChargeStatus]
    GENERAL_CHARGE_SAVETIME_TEMP_PRECON: _ClassVar[GeneralChargeStatus]

class EnablementState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ENABLEMENT_STATE_UNKNOWN: _ClassVar[EnablementState]
    ENABLEMENT_STATE_IDLE: _ClassVar[EnablementState]

class SentryThreat(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SENTRY_THREAT_LEVEL_UNKNOWN: _ClassVar[SentryThreat]
    SENTRY_THREAT_IDLE: _ClassVar[SentryThreat]
    SENTRY_THREAT_LEVEL_ONE: _ClassVar[SentryThreat]
    SENTRY_THREAT_LEVEL_TWO: _ClassVar[SentryThreat]
    SENTRY_THREAT_LEVEL_THREE: _ClassVar[SentryThreat]
    SENTRY_THREAT_NO_THREAT: _ClassVar[SentryThreat]

class SentryUsbDriveStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    UNKNOWN_SENTRY_USB_DRIVE_STATUS: _ClassVar[SentryUsbDriveStatus]
    SENTRY_USB_DRIVE_IDLE: _ClassVar[SentryUsbDriveStatus]
    SENTRY_USB_DRIVE_CONNECTED: _ClassVar[SentryUsbDriveStatus]
    SENTRY_USB_DRIVE_NOT_CONNECTED: _ClassVar[SentryUsbDriveStatus]

class EnhancedDeterrenceState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    ENHANCED_DETERRENCE_STATE_UNKNOWN: _ClassVar[EnhancedDeterrenceState]
    ENHANCED_DETERRENCE_ENABLED: _ClassVar[EnhancedDeterrenceState]
    ENHANCED_DETERRENCE_DISABLED: _ClassVar[EnhancedDeterrenceState]
    ENHANCED_DETERRENCE_IDLE: _ClassVar[EnhancedDeterrenceState]

class SentryRemoteAlarmState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SENTRY_REMOTE_ALARM_STATE_UNKNOWN: _ClassVar[SentryRemoteAlarmState]
    SENTRY_REMOTE_ALARM_IDLE: _ClassVar[SentryRemoteAlarmState]
    SENTRY_REMOTE_ALARM_ON: _ClassVar[SentryRemoteAlarmState]
    SENTRY_REMOTE_ALARM_OFF: _ClassVar[SentryRemoteAlarmState]

class LowPowerModeStatus(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    LOW_POWER_MODE_STATUS_UNKNOWN: _ClassVar[LowPowerModeStatus]
    LOW_POWER_MODE_STATUS_INACTIVE: _ClassVar[LowPowerModeStatus]
    LOW_POWER_MODE_STATUS_ACTIVE: _ClassVar[LowPowerModeStatus]

class ChargeAction(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CHARGE_ACTION_UNKNOWN: _ClassVar[ChargeAction]
    CHARGE_ACTION_START: _ClassVar[ChargeAction]
    CHARGE_ACTION_STOP: _ClassVar[ChargeAction]

class DocumentType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    DOCUMENT_TYPE_UNKNOWN: _ClassVar[DocumentType]
    DOCUMENT_TYPE_RELEASE_NOTES_PRE: _ClassVar[DocumentType]
    DOCUMENT_TYPE_RELEASE_NOTES_POST: _ClassVar[DocumentType]
    DOCUMENT_TYPE_OWNERS_MANUAL: _ClassVar[DocumentType]

class DischargeCommand(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    DISCHARGE_UNKNOWN: _ClassVar[DischargeCommand]

class WindowSwitchState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    WINDOW_SWITCH_STATE_UNKNOWN: _ClassVar[WindowSwitchState]
    WINDOW_SWITCH_STATE_IDLE: _ClassVar[WindowSwitchState]
    WINDOW_SWITCH_STATE_AUTO_UP_ALL: _ClassVar[WindowSwitchState]
    WINDOW_SWITCH_STATE_VENT_ALL: _ClassVar[WindowSwitchState]
    WINDOW_SWITCH_STATE_AUTO_DOWN_ALL: _ClassVar[WindowSwitchState]
    WINDOW_SWITCH_STATE_ERROR: _ClassVar[WindowSwitchState]

class MaxACState(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    MAX_AC_STATE_UNKNOWN: _ClassVar[MaxACState]
    MAX_AC_STATE_OFF: _ClassVar[MaxACState]
    MAX_AC_STATE_ON: _ClassVar[MaxACState]

class SteeringWheelHeaterLevel(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    STEERING_WHEEL_HEATER_LEVEL_UNKNOWN: _ClassVar[SteeringWheelHeaterLevel]
    STEERING_WHEEL_HEATER_LEVEL_OFF: _ClassVar[SteeringWheelHeaterLevel]
    STEERING_WHEEL_HEATER_LEVEL_1: _ClassVar[SteeringWheelHeaterLevel]
    STEERING_WHEEL_HEATER_LEVEL_2: _ClassVar[SteeringWheelHeaterLevel]
    STEERING_WHEEL_HEATER_LEVEL_3: _ClassVar[SteeringWheelHeaterLevel]

class CreatureComfortMode(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    CREATURE_COMFORT_MODE_UNKNOWN: _ClassVar[CreatureComfortMode]
    CREATURE_COMFORT_MODE_OFF: _ClassVar[CreatureComfortMode]
    CREATURE_COMFORT_MODE_ON: _ClassVar[CreatureComfortMode]
ACCESS_LEVEL_UNKNOWN: AccessLevel
ACCESS_LEVEL_PREDELIVERY_OWNER: AccessLevel
ACCESS_LEVEL_PRIMARY_OWNER: AccessLevel
MODEL_UNKNOWN: Model
MODEL_AIR: Model
MODEL_GRAVITY: Model
MODEL_VARIANT_UNKNOWN: ModelVariant
MODEL_VARIANT_DREAM_EDITION: ModelVariant
MODEL_VARIANT_GRAND_TOURING: ModelVariant
MODEL_VARIANT_TOURING: ModelVariant
MODEL_VARIANT_PURE: ModelVariant
MODEL_VARIANT_SAPPHIRE: ModelVariant
MODEL_VARIANT_HYPER: ModelVariant
MODEL_VARIANT_EXECUTIVE: ModelVariant
PAINT_COLOR_UNKNOWN: PaintColor
PAINT_COLOR_EUREKA_GOLD: PaintColor
PAINT_COLOR_STELLAR_WHITE: PaintColor
PAINT_COLOR_INFINITE_BLACK: PaintColor
PAINT_COLOR_COSMOS_SILVER: PaintColor
PAINT_COLOR_QUANTUM_GREY: PaintColor
PAINT_COLOR_ZENITH_RED: PaintColor
PAINT_COLOR_FATHOM_BLUE: PaintColor
PAINT_COLOR_CUSTOM: PaintColor
PAINT_COLOR_SAPPHIRE_BLUE: PaintColor
LOOK_UNKNOWN: Look
LOOK_PLATINUM: Look
LOOK_STEALTH: Look
LOOK_SAPPHIRE: Look
LOOK_SURFRIDER: Look
LOOK_BASE: Look
WHEELS_UNKNOWN: Wheels
WHEELS_DREAM: Wheels
WHEELS_BLADE: Wheels
WHEELS_LITE: Wheels
WHEELS_RANGE: Wheels
WHEELS_SPORT_STEALTH: Wheels
WHEELS_BLADE_GRAPHITE: Wheels
WHEELS_LITE_STEALTH: Wheels
WHEELS_SPORT_LUSTER: Wheels
WHEELS_SAPPHIRE_PACKAGE: Wheels
WHEELS_RANGE_STEALTH: Wheels
SUBSCRIPTION_STATUS_UNKNOWN: SubscriptionStatus
SUBSCRIPTION_STATUS_CURRENT: SubscriptionStatus
CHARGING_ACCOUNT_STATUS_UNKNOWN: ChargingAccountStatus
CHARGING_ACCOUNT_STATUS_DISABLED: ChargingAccountStatus
CHARGING_ACCOUNT_STATUS_ENROLLED: ChargingAccountStatus
CHARGING_VENDOR_UNKNOWN: ChargingVendor
CHARGING_VENDOR_ELECTRIFY_AMERICA: ChargingVendor
CHARGING_VENDOR_BOSCH: ChargingVendor
EDITION_UNKNOWN: Edition
EDITION_PERFORMANCE: Edition
EDITION_RANGE: Edition
EDITION_STANDARD: Edition
BATTERY_TYPE_UNKNOWN: BatteryType
BATTERY_TYPE_01: BatteryType
BATTERY_TYPE_02: BatteryType
BATTERY_TYPE_03: BatteryType
BATTERY_TYPE_04: BatteryType
BATTERY_TYPE_05: BatteryType
BATTERY_TYPE_06: BatteryType
BATTERY_TYPE_07: BatteryType
BATTERY_TYPE_08: BatteryType
BATTERY_TYPE_09: BatteryType
BATTERY_TYPE_25: BatteryType
INTERIOR_UNKNOWN: Interior
INTERIOR_SANTA_CRUZ: Interior
INTERIOR_TAHOE: Interior
INTERIOR_MOJAVE: Interior
INTERIOR_SANTA_MONICA: Interior
STRUT_TYPE_UNKNOWN: StrutType
STRUT_TYPE_GAS: StrutType
STRUT_TYPE_POWER: StrutType
ROOF_TYPE_UNKNOWN: RoofType
ROOF_TYPE_GLASS_CANOPY: RoofType
ROOF_TYPE_METAL: RoofType
FRONT_SEATS_VENTILATION_UNKNOWN: FrontSeatsVentilationAvailability
FRONT_SEATS_VENTILATION_UNAVAILABLE: FrontSeatsVentilationAvailability
FRONT_SEATS_VENTILATION_AVAILABLE: FrontSeatsVentilationAvailability
FRONT_SEATS_HEATING_UNKNOWN: FrontSeatsHeatingAvailability
FRONT_SEATS_HEATING_UNAVAILABLE: FrontSeatsHeatingAvailability
FRONT_SEATS_HEATING_AVAILABLE: FrontSeatsHeatingAvailability
SECOND_ROW_HEATED_SEATS_UNKNOWN: SecondRowHeatedSeatsAvailability
SECOND_ROW_HEATED_SEATS_UNAVAILABLE: SecondRowHeatedSeatsAvailability
SECOND_ROW_HEATED_SEATS_AVAILABLE: SecondRowHeatedSeatsAvailability
HEATED_STEERING_WHEEL_UNKNOWN: HeatedSteeringWheelAvailability
HEATED_STEERING_WHEEL_UNAVAILABLE: HeatedSteeringWheelAvailability
HEATED_STEERING_WHEEL_AVAILABLE: HeatedSteeringWheelAvailability
REAR_SEAT_CONFIG_UNKNOWN: RearSeatConfig
REAR_SEAT_CONFIG_5_SEAT: RearSeatConfig
REAR_SEAT_CONFIG_6_SEAT: RearSeatConfig
REAR_SEAT_CONFIG_7_SEAT: RearSeatConfig
WARNING_UNKNOWN: WarningState
WARNING_OFF: WarningState
WARNING_ON: WarningState
BATTERY_PRECON_UNKNOWN: BatteryPreconStatus
BATTERY_PRECON_OFF: BatteryPreconStatus
BATTERY_PRECON_ON: BatteryPreconStatus
BATTERY_PRECON_UNAVAILABLE: BatteryPreconStatus
BATTERY_CELL_TYPE_UNKNOWN: BatteryCellType
BATTERY_CELL_TYPE_LG_M48: BatteryCellType
BATTERY_CELL_TYPE_SDI_50G: BatteryCellType
BATTERY_CELL_TYPE_PANA_2170M: BatteryCellType
BATTERY_CELL_TYPE_SDI_50GV2: BatteryCellType
BATTERY_PACK_TYPE_UNKNOWN: BatteryPackType
BATTERY_PACK_TYPE_AIR_22: BatteryPackType
BATTERY_PACK_TYPE_AIR_18: BatteryPackType
BATTERY_PACK_TYPE_AIR_16: BatteryPackType
POWER_STATE_UNKNOWN: PowerState
POWER_STATE_SLEEP: PowerState
POWER_STATE_WINK: PowerState
POWER_STATE_ACCESSORY: PowerState
POWER_STATE_DRIVE: PowerState
POWER_STATE_LIVE_CHARGE: PowerState
POWER_STATE_SLEEP_CHARGE: PowerState
POWER_STATE_LIVE_UPDATE: PowerState
POWER_STATE_CLOUD_2: PowerState
POWER_STATE_MONITOR: PowerState
LOCK_STATE_UNKNOWN: LockState
LOCK_STATE_UNLOCKED: LockState
LOCK_STATE_LOCKED: LockState
DOOR_STATE_UNKNOWN: DoorState
DOOR_STATE_OPEN: DoorState
DOOR_STATE_CLOSED: DoorState
DOOR_STATE_AJAR: DoorState
WALKAWAY_UNKNOWN: WalkawayState
WALKAWAY_ACTIVE: WalkawayState
WALKAWAY_DISABLE: WalkawayState
ACCESS_REQUEST_UNKNOWN: AccessRequest
ACCESS_REQUEST_ACTIVE: AccessRequest
ACCESS_REQUEST_PASSIVE: AccessRequest
ACCESS_REQUEST_PASSIVE_DRIVER: AccessRequest
ACCESS_REQUEST_PASSIVE_TEMP_DISABLED: AccessRequest
KEYFOB_BATTERY_STATUS_UNKNOWN: KeyfobBatteryStatus
KEYFOB_BATTERY_STATUS_LOW: KeyfobBatteryStatus
KEYFOB_BATTERY_STATUS_SUFFICIENT: KeyfobBatteryStatus
ALL_WINDOW_POSITION_UNKNOWN: AllWindowPosition
ALL_WINDOW_POSITION_IDLE: AllWindowPosition
ALL_WINDOW_POSITION_OPEN: AllWindowPosition
ALL_WINDOW_POSITION_CLOSED: AllWindowPosition
ALL_WINDOW_POSITION_ERROR: AllWindowPosition
WINDOW_POSITION_STATUS_UNKNOWN: WindowPositionStatus
WINDOW_POSITION_STATUS_FULLY_CLOSED: WindowPositionStatus
WINDOW_POSITION_STATUS_ABOVE_SHORT_DROP_POSITION: WindowPositionStatus
WINDOW_POSITION_STATUS_SHORT_DROP_POSITION: WindowPositionStatus
WINDOW_POSITION_STATUS_BELOW_SHORT_DROP_POSITION: WindowPositionStatus
WINDOW_POSITION_STATUS_FULLY_OPEN: WindowPositionStatus
WINDOW_POSITION_STATUS_BETWEEN_FULLY_CLOSED_AND_SHORT_DROP_DOWN: WindowPositionStatus
WINDOW_POSITION_STATUS_BETWEEN_SHORT_DROP_DOWN_AND_FULLY_OPEN: WindowPositionStatus
MIRROR_FOLD_STATE_UNKNOWN: MirrorFoldState
MIRROR_FOLD_STATE_IDLE: MirrorFoldState
MIRROR_FOLD_STATE_FOLDED_OUT: MirrorFoldState
MIRROR_FOLD_STATE_FOLDED_IN: MirrorFoldState
LIVING_OBJECT_DETECTION_STATUS_UNKNOWN: LivingObjectDetectionStatus
LIVING_OBJECT_DETECTION_STATUS_DISABLED: LivingObjectDetectionStatus
LIVING_OBJECT_DETECTION_STATUS_NOT_ACTIVE: LivingObjectDetectionStatus
LIVING_OBJECT_DETECTION_STATUS_LEVEL_1_WARNING: LivingObjectDetectionStatus
LIVING_OBJECT_DETECTION_STATUS_LEVEL_2_WARNING: LivingObjectDetectionStatus
LIVING_OBJECT_DETECTION_STATUS_LEVEL_3_WARNING: LivingObjectDetectionStatus
LIGHT_STATE_REALLY_UNKNOWN: LightState
LIGHT_STATE_OFF: LightState
LIGHT_STATE_ON: LightState
LIGHT_STATE_UNKNOWN: LightState
LIGHT_ACTION_UNKNOWN: LightAction
LIGHT_ACTION_FLASH: LightAction
LIGHT_ACTION_ON: LightAction
LIGHT_ACTION_OFF: LightAction
LIGHT_ACTION_HAZARD_ON: LightAction
LIGHT_ACTION_HAZARD_OFF: LightAction
WELCOME_ACTION_UNKNOWN: WelcomeAction
WELCOME_ACTION_UNLOCK: WelcomeAction
WELCOME_ACTION_DEPARTURE: WelcomeAction
WELCOME_ACTION_LIGHTS: WelcomeAction
WELCOME_ACTION_BLINKERS: WelcomeAction
TIRE_PRESSURE_SENSOR_DEFECTIVE_UNKNOWN: TirePressureSensorDefective
TIRE_PRESSURE_SENSOR_DEFECTIVE_OFF: TirePressureSensorDefective
TIRE_PRESSURE_SENSOR_DEFECTIVE_ON: TirePressureSensorDefective
CHARGE_STATE_UNKNOWN: ChargeState
CHARGE_STATE_NOT_CONNECTED: ChargeState
CHARGE_STATE_CABLE_CONNECTED: ChargeState
CHARGE_STATE_ESTABLISHING_SESSION: ChargeState
CHARGE_STATE_AUTHORIZING_PNC: ChargeState
CHARGE_STATE_AUTHORIZING_EXTERNAL: ChargeState
CHARGE_STATE_AUTHORIZED: ChargeState
CHARGE_STATE_CHARGER_PREPARATION: ChargeState
CHARGE_STATE_CHARGING: ChargeState
CHARGE_STATE_CHARGING_END_OK: ChargeState
CHARGE_STATE_CHARGING_STOPPED: ChargeState
CHARGE_STATE_EVSE_MALFUNCTION: ChargeState
CHARGE_STATE_DISCHARGING: ChargeState
CHARGE_STATE_DISCHARGING_COMPLETED: ChargeState
CHARGE_STATE_DISCHARGING_STOPPED: ChargeState
CHARGE_STATE_DISCHARGING_FAULT: ChargeState
CHARGE_STATE_DISCHARGING_UNAVAILABLE: ChargeState
SCHEDULED_CHARGE_STATE_UNKNOWN: ScheduledChargeState
SCHEDULED_CHARGE_STATE_IDLE: ScheduledChargeState
SCHEDULED_CHARGE_STATE_SCHEDULED_TO_CHARGE: ScheduledChargeState
SCHEDULED_CHARGE_STATE_REQUEST_TO_CHARGE: ScheduledChargeState
SCHEDULED_CHARGE_UNAVAILABLE_UNKNOWN: ScheduledChargeUnavailableState
SCHEDULED_CHARGE_UNAVAILABLE_NO_REQUEST: ScheduledChargeUnavailableState
ENERGY_TYPE_UNKNOWN: EnergyType
ENERGY_TYPE_AC: EnergyType
ENERGY_TYPE_DC: EnergyType
ENERGY_TYPE_V2V: EnergyType
MOBILE_DISCHARGING_COMMAND_UNKNOWN: MobileDischargingCommand
MOBILE_DISCHARGING_COMMAND_START_DISCHARGING: MobileDischargingCommand
CHARGING_SESSION_RESTART_ALLOWED_STATUS_UNKNOWN: ChargingSessionRestartAllowed
CHARGING_SESSION_RESTART_ALLOWED_STATUS_IDLE: ChargingSessionRestartAllowed
CHARGING_SESSION_RESTART_ALLOWED_STATUS_NOT_ALLOWED: ChargingSessionRestartAllowed
CHARGING_SESSION_RESTART_ALLOWED_STATUS_ALLOWED: ChargingSessionRestartAllowed
EA_PNC_STATUS_UNKNOWN: EaPncStatus
EA_PNC_STATUS_IDLE: EaPncStatus
EA_PNC_STATUS_ENABLE: EaPncStatus
EA_PNC_STATUS_DISABLE: EaPncStatus
EA_PNC_STATUS_NO_NOTIFICATION: EaPncStatus
AC_OUTLET_UNAVAILABLE_REASON_UNKNOWN: AcOutletUnavailableReason
AC_OUTLET_UNAVAILABLE_REASON_NONE: AcOutletUnavailableReason
AC_OUTLET_UNAVAILABLE_REASON_WARNING_FAULT: AcOutletUnavailableReason
AC_OUTLET_UNAVAILABLE_REASON_CRITICAL_FAULT: AcOutletUnavailableReason
AC_OUTLET_UNAVAILABLE_REASON_CHARGING: AcOutletUnavailableReason
AC_OUTLET_UNAVAILABLE_REASON_LOW_VEH_RANGE: AcOutletUnavailableReason
AC_OUTLET_UNAVAILABLE_REASON_WARNING_FAULT_CAMP: AcOutletUnavailableReason
AC_OUTLET_UNAVAILABLE_REASON_CRITICAL_FAULT_CAMP: AcOutletUnavailableReason
AC_OUTLET_UNAVAILABLE_REASON_LOW_VEH_RANGE_CAMP: AcOutletUnavailableReason
UPDATE_STATE_UNKNOWN: UpdateState
UPDATE_STATE_IN_PROGRESS: UpdateState
UPDATE_STATE_SUCCESS: UpdateState
UPDATE_STATE_FAILED: UpdateState
UPDATE_FAILED_DRIVE_ALLOWED: UpdateState
UPDATE_SUCCESS_WITH_WARNINGS: UpdateState
UPDATE_NOTSTARTED_WITH_WARNINGS: UpdateState
UPDATE_AVAILABILITY_UNKNOWN: UpdateAvailability
UPDATE_AVAILABLE: UpdateAvailability
TCU_SOFTWARE_DOWNLOAD_STATUS_UNKNOWN: TcuDownloadStatus
TCU_SOFTWARE_DOWNLOAD_STATUS_IDLE: TcuDownloadStatus
TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOADING: TcuDownloadStatus
TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOAD_PAUSED: TcuDownloadStatus
TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOAD_COMPLETE: TcuDownloadStatus
TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOAD_FAILED: TcuDownloadStatus
TCU_SOFTWARE_DOWNLOAD_STATUS_DOWNLOAD_CANCELED: TcuDownloadStatus
ALARM_STATUS_UNKNOWN: AlarmStatus
ALARM_STATUS_DISARMED: AlarmStatus
ALARM_STATUS_ARMED: AlarmStatus
ALARM_MODE_UNKNOWN: AlarmMode
ALARM_MODE_OFF: AlarmMode
ALARM_MODE_ON: AlarmMode
ALARM_MODE_SILENT: AlarmMode
CLOUD_CONNECTION_UNKNOWN: CloudConnectionState
CLOUD_CONNECTION_CONNECTED: CloudConnectionState
CLOUD_CONNECTION_DISCONNECTED: CloudConnectionState
KEYLESS_DRIVING_UNKNOWN: KeylessDrivingState
KEYLESS_DRIVING_ON: KeylessDrivingState
KEYLESS_DRIVING_OFF: KeylessDrivingState
HVAC_POWER_UNKNOWN: HvacPower
HVAC_ON: HvacPower
HVAC_OFF: HvacPower
HVAC_PRECONDITION: HvacPower
HVAC_KEEP_TEMP: HvacPower
DEFROST_STATE_UNKNOWN: DefrostState
DEFROST_ON: DefrostState
DEFROST_OFF: DefrostState
HVAC_PRECONDITION_STATUS_UNKNOWN: HvacPreconditionStatus
HVAC_PRECONDITION_STATUS_STILL_ACTIVE: HvacPreconditionStatus
HVAC_PRECONDITION_STATUS_TEMP_REACHED: HvacPreconditionStatus
HVAC_PRECONDITION_STATUS_TIMEOUT: HvacPreconditionStatus
HVAC_PRECONDITION_STATUS_USER_INPUT: HvacPreconditionStatus
HVAC_PRECONDITION_STATUS_NOT_ACTIVE_PRECONDITION: HvacPreconditionStatus
KEEP_CLIMATE_STATUS_UNKNOWN: KeepClimateStatus
KEEP_CLIMATE_STATUS_INACTIVE: KeepClimateStatus
KEEP_CLIMATE_STATUS_ENABLED: KeepClimateStatus
KEEP_CLIMATE_STATUS_CANCELED: KeepClimateStatus
KEEP_CLIMATE_STATUS_PET_MODE_ON: KeepClimateStatus
KEEP_CLIMATE_CONDITION_UNKNOWN: KeepClimateCondition
SEAT_CLIMATE_MODE_UNKNOWN: SeatClimateMode
SEAT_CLIMATE_MODE_OFF: SeatClimateMode
SEAT_CLIMATE_MODE_LOW: SeatClimateMode
SEAT_CLIMATE_MODE_MEDIUM: SeatClimateMode
SEAT_CLIMATE_MODE_HIGH: SeatClimateMode
STEERING_HEATER_STATUS_UNKNOWN: SteeringHeaterStatus
STEERING_HEATER_STATUS_OFF: SteeringHeaterStatus
STEERING_HEATER_STATUS_ON: SteeringHeaterStatus
SYNC_SET_UNKNOWN: SyncSet
SYNC_SET_OFF: SyncSet
SYNC_SET_ON: SyncSet
REAR_WINDOW_HEATING_STATUS_UNKNOWN: RearWindowHeatingStatus
REAR_WINDOW_HEATING_STATUS_OFF: RearWindowHeatingStatus
REAR_WINDOW_HEATING_STATUS_ON: RearWindowHeatingStatus
REAR_WINDOW_HEATING_STATUS_OFF_LOST_COMM_WITH_DCM: RearWindowHeatingStatus
REAR_WINDOW_HEATING_STATUS_ON_LOST_COMM_WITH_DCM: RearWindowHeatingStatus
HVAC_LIMITED_UNKNOWN: HvacLimited
HVAC_LIMITED_OFF: HvacLimited
HVAC_LIMITED_ON: HvacLimited
DRIVE_MODE_UNKNOWN: DriveMode
DRIVE_MODE_COMFORT: DriveMode
DRIVE_MODE_SWIFT: DriveMode
DRIVE_MODE_WINTER: DriveMode
DRIVE_MODE_VALET: DriveMode
DRIVE_MODE_SPORT_PLUS: DriveMode
DRIVE_MODE_RESERVED_1: DriveMode
DRIVE_MODE_RESERVED_2: DriveMode
DRIVE_MODE_SERVICE: DriveMode
DRIVE_MODE_LAUNCH: DriveMode
DRIVE_MODE_FACTORY: DriveMode
DRIVE_MODE_DEV1: DriveMode
DRIVE_MODE_DEV2: DriveMode
DRIVE_MODE_TRANSPORT: DriveMode
DRIVE_MODE_SHOWROOM: DriveMode
DRIVE_MODE_TOW: DriveMode
DRIVE_MODE_TEST_DRIVE: DriveMode
DRIVE_MODE_RESERVED_3: DriveMode
PRIVACY_MODE_UNKNOWN: PrivacyMode
PRIVACY_MODE_CONNECTIVITY_ENABLED: PrivacyMode
PRIVACY_MODE_CONNECTIVITY_DISABLED: PrivacyMode
GEAR_UNKNOWN: GearPosition
GEAR_PARK: GearPosition
GEAR_REVERSE: GearPosition
GEAR_NEUTRAL: GearPosition
GEAR_DRIVE: GearPosition
SHARED_TRIP_UNKNOWN: SharedTripState
SHARED_TRIP_AVAILABLE: SharedTripState
SHARED_TRIP_PROFILE_UPDATED: SharedTripState
PANIC_ALARM_UNKNOWN: PanicState
PANIC_ALARM_ON: PanicState
TCU_UNKNOWN: TcuState
TCU_SLEEP: TcuState
TCU_DROWSY: TcuState
TCU_FULL: TcuState
TCU_FACTORY: TcuState
TCU_POWER: TcuState
TCU_OFF: TcuState
LTE_TYPE_UNKNOWN: LteType
LTE_TYPE_3G: LteType
LTE_TYPE_4G: LteType
INTERNET_STATUS_UNKNOWN: InternetStatus
INTERNET_DISCONNECTED: InternetStatus
INTERNET_CONNECTED: InternetStatus
MPB_FAULT_STATUS_UNKNOWN: MpbFaultStatus
MPB_FAULT_STATUS_NORMAL: MpbFaultStatus
MPB_FAULT_STATUS_CRITICAL: MpbFaultStatus
POWERTRAIN_MESSAGE_UNKNOWN: PowertrainMessage
POWERTRAIN_MESSAGE_BLANK_NO_MESSAGE: PowertrainMessage
POWERTRAIN_NOTIFY_UNKNOWN: PowertrainNotifyStatus
POWERTRAIN_NOTIFY_NONE: PowertrainNotifyStatus
GENERAL_CHARGE_UNKNOWN: GeneralChargeStatus
GENERAL_CHARGE_DEFAULT: GeneralChargeStatus
GENERAL_CHARGE_DERATED_CHARGING_POWER: GeneralChargeStatus
GENERAL_CHARGE_SAVETIME_TEMP_PRECON: GeneralChargeStatus
ENABLEMENT_STATE_UNKNOWN: EnablementState
ENABLEMENT_STATE_IDLE: EnablementState
SENTRY_THREAT_LEVEL_UNKNOWN: SentryThreat
SENTRY_THREAT_IDLE: SentryThreat
SENTRY_THREAT_LEVEL_ONE: SentryThreat
SENTRY_THREAT_LEVEL_TWO: SentryThreat
SENTRY_THREAT_LEVEL_THREE: SentryThreat
SENTRY_THREAT_NO_THREAT: SentryThreat
UNKNOWN_SENTRY_USB_DRIVE_STATUS: SentryUsbDriveStatus
SENTRY_USB_DRIVE_IDLE: SentryUsbDriveStatus
SENTRY_USB_DRIVE_CONNECTED: SentryUsbDriveStatus
SENTRY_USB_DRIVE_NOT_CONNECTED: SentryUsbDriveStatus
ENHANCED_DETERRENCE_STATE_UNKNOWN: EnhancedDeterrenceState
ENHANCED_DETERRENCE_ENABLED: EnhancedDeterrenceState
ENHANCED_DETERRENCE_DISABLED: EnhancedDeterrenceState
ENHANCED_DETERRENCE_IDLE: EnhancedDeterrenceState
SENTRY_REMOTE_ALARM_STATE_UNKNOWN: SentryRemoteAlarmState
SENTRY_REMOTE_ALARM_IDLE: SentryRemoteAlarmState
SENTRY_REMOTE_ALARM_ON: SentryRemoteAlarmState
SENTRY_REMOTE_ALARM_OFF: SentryRemoteAlarmState
LOW_POWER_MODE_STATUS_UNKNOWN: LowPowerModeStatus
LOW_POWER_MODE_STATUS_INACTIVE: LowPowerModeStatus
LOW_POWER_MODE_STATUS_ACTIVE: LowPowerModeStatus
CHARGE_ACTION_UNKNOWN: ChargeAction
CHARGE_ACTION_START: ChargeAction
CHARGE_ACTION_STOP: ChargeAction
DOCUMENT_TYPE_UNKNOWN: DocumentType
DOCUMENT_TYPE_RELEASE_NOTES_PRE: DocumentType
DOCUMENT_TYPE_RELEASE_NOTES_POST: DocumentType
DOCUMENT_TYPE_OWNERS_MANUAL: DocumentType
DISCHARGE_UNKNOWN: DischargeCommand
WINDOW_SWITCH_STATE_UNKNOWN: WindowSwitchState
WINDOW_SWITCH_STATE_IDLE: WindowSwitchState
WINDOW_SWITCH_STATE_AUTO_UP_ALL: WindowSwitchState
WINDOW_SWITCH_STATE_VENT_ALL: WindowSwitchState
WINDOW_SWITCH_STATE_AUTO_DOWN_ALL: WindowSwitchState
WINDOW_SWITCH_STATE_ERROR: WindowSwitchState
MAX_AC_STATE_UNKNOWN: MaxACState
MAX_AC_STATE_OFF: MaxACState
MAX_AC_STATE_ON: MaxACState
STEERING_WHEEL_HEATER_LEVEL_UNKNOWN: SteeringWheelHeaterLevel
STEERING_WHEEL_HEATER_LEVEL_OFF: SteeringWheelHeaterLevel
STEERING_WHEEL_HEATER_LEVEL_1: SteeringWheelHeaterLevel
STEERING_WHEEL_HEATER_LEVEL_2: SteeringWheelHeaterLevel
STEERING_WHEEL_HEATER_LEVEL_3: SteeringWheelHeaterLevel
CREATURE_COMFORT_MODE_UNKNOWN: CreatureComfortMode
CREATURE_COMFORT_MODE_OFF: CreatureComfortMode
CREATURE_COMFORT_MODE_ON: CreatureComfortMode

class ChargingSubscription(_message.Message):
    __slots__ = ("name", "expiration_date", "start_date", "status")
    NAME_FIELD_NUMBER: _ClassVar[int]
    EXPIRATION_DATE_FIELD_NUMBER: _ClassVar[int]
    START_DATE_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    name: str
    expiration_date: int
    start_date: int
    status: SubscriptionStatus
    def __init__(self, name: _Optional[str] = ..., expiration_date: _Optional[int] = ..., start_date: _Optional[int] = ..., status: _Optional[_Union[SubscriptionStatus, str]] = ...) -> None: ...

class ChargingAccount(_message.Message):
    __slots__ = ("ema_id", "vehicle_id", "status", "created_at_epoch_sec", "expiry_on_epoch_sec", "vendor_name", "valid_payment_method", "plan_id")
    EMA_ID_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    CREATED_AT_EPOCH_SEC_FIELD_NUMBER: _ClassVar[int]
    EXPIRY_ON_EPOCH_SEC_FIELD_NUMBER: _ClassVar[int]
    VENDOR_NAME_FIELD_NUMBER: _ClassVar[int]
    VALID_PAYMENT_METHOD_FIELD_NUMBER: _ClassVar[int]
    PLAN_ID_FIELD_NUMBER: _ClassVar[int]
    ema_id: str
    vehicle_id: str
    status: ChargingAccountStatus
    created_at_epoch_sec: int
    expiry_on_epoch_sec: int
    vendor_name: ChargingVendor
    valid_payment_method: bool
    plan_id: str
    def __init__(self, ema_id: _Optional[str] = ..., vehicle_id: _Optional[str] = ..., status: _Optional[_Union[ChargingAccountStatus, str]] = ..., created_at_epoch_sec: _Optional[int] = ..., expiry_on_epoch_sec: _Optional[int] = ..., vendor_name: _Optional[_Union[ChargingVendor, str]] = ..., valid_payment_method: bool = ..., plan_id: _Optional[str] = ...) -> None: ...

class SpecialIdentifiers(_message.Message):
    __slots__ = ("door_plate",)
    DOOR_PLATE_FIELD_NUMBER: _ClassVar[int]
    door_plate: str
    def __init__(self, door_plate: _Optional[str] = ...) -> None: ...

class Reservation(_message.Message):
    __slots__ = ("date",)
    DATE_FIELD_NUMBER: _ClassVar[int]
    date: int
    def __init__(self, date: _Optional[int] = ...) -> None: ...

class VehicleConfig(_message.Message):
    __slots__ = ("vin", "model", "variant", "nickname", "paint_color", "ema_id", "wheels", "ea_subscription", "charging_accounts", "country_code", "region_code", "edition", "battery", "interior", "special_identifiers", "look", "exterior_color_code", "interior_color_code", "frunk_strut", "reservation", "roof", "front_seats_heating", "front_seats_ventilation", "second_row_heated_seats", "rear_seat_config", "heated_steering_wheel")
    VIN_FIELD_NUMBER: _ClassVar[int]
    MODEL_FIELD_NUMBER: _ClassVar[int]
    VARIANT_FIELD_NUMBER: _ClassVar[int]
    NICKNAME_FIELD_NUMBER: _ClassVar[int]
    PAINT_COLOR_FIELD_NUMBER: _ClassVar[int]
    EMA_ID_FIELD_NUMBER: _ClassVar[int]
    WHEELS_FIELD_NUMBER: _ClassVar[int]
    EA_SUBSCRIPTION_FIELD_NUMBER: _ClassVar[int]
    CHARGING_ACCOUNTS_FIELD_NUMBER: _ClassVar[int]
    COUNTRY_CODE_FIELD_NUMBER: _ClassVar[int]
    REGION_CODE_FIELD_NUMBER: _ClassVar[int]
    EDITION_FIELD_NUMBER: _ClassVar[int]
    BATTERY_FIELD_NUMBER: _ClassVar[int]
    INTERIOR_FIELD_NUMBER: _ClassVar[int]
    SPECIAL_IDENTIFIERS_FIELD_NUMBER: _ClassVar[int]
    LOOK_FIELD_NUMBER: _ClassVar[int]
    EXTERIOR_COLOR_CODE_FIELD_NUMBER: _ClassVar[int]
    INTERIOR_COLOR_CODE_FIELD_NUMBER: _ClassVar[int]
    FRUNK_STRUT_FIELD_NUMBER: _ClassVar[int]
    RESERVATION_FIELD_NUMBER: _ClassVar[int]
    ROOF_FIELD_NUMBER: _ClassVar[int]
    FRONT_SEATS_HEATING_FIELD_NUMBER: _ClassVar[int]
    FRONT_SEATS_VENTILATION_FIELD_NUMBER: _ClassVar[int]
    SECOND_ROW_HEATED_SEATS_FIELD_NUMBER: _ClassVar[int]
    REAR_SEAT_CONFIG_FIELD_NUMBER: _ClassVar[int]
    HEATED_STEERING_WHEEL_FIELD_NUMBER: _ClassVar[int]
    vin: str
    model: Model
    variant: ModelVariant
    nickname: str
    paint_color: PaintColor
    ema_id: str
    wheels: Wheels
    ea_subscription: ChargingSubscription
    charging_accounts: _containers.RepeatedCompositeFieldContainer[ChargingAccount]
    country_code: str
    region_code: str
    edition: Edition
    battery: BatteryType
    interior: Interior
    special_identifiers: SpecialIdentifiers
    look: Look
    exterior_color_code: str
    interior_color_code: str
    frunk_strut: StrutType
    reservation: Reservation
    roof: RoofType
    front_seats_heating: FrontSeatsHeatingAvailability
    front_seats_ventilation: FrontSeatsVentilationAvailability
    second_row_heated_seats: SecondRowHeatedSeatsAvailability
    rear_seat_config: RearSeatConfig
    heated_steering_wheel: HeatedSteeringWheelAvailability
    def __init__(self, vin: _Optional[str] = ..., model: _Optional[_Union[Model, str]] = ..., variant: _Optional[_Union[ModelVariant, str]] = ..., nickname: _Optional[str] = ..., paint_color: _Optional[_Union[PaintColor, str]] = ..., ema_id: _Optional[str] = ..., wheels: _Optional[_Union[Wheels, str]] = ..., ea_subscription: _Optional[_Union[ChargingSubscription, _Mapping]] = ..., charging_accounts: _Optional[_Iterable[_Union[ChargingAccount, _Mapping]]] = ..., country_code: _Optional[str] = ..., region_code: _Optional[str] = ..., edition: _Optional[_Union[Edition, str]] = ..., battery: _Optional[_Union[BatteryType, str]] = ..., interior: _Optional[_Union[Interior, str]] = ..., special_identifiers: _Optional[_Union[SpecialIdentifiers, _Mapping]] = ..., look: _Optional[_Union[Look, str]] = ..., exterior_color_code: _Optional[str] = ..., interior_color_code: _Optional[str] = ..., frunk_strut: _Optional[_Union[StrutType, str]] = ..., reservation: _Optional[_Union[Reservation, _Mapping]] = ..., roof: _Optional[_Union[RoofType, str]] = ..., front_seats_heating: _Optional[_Union[FrontSeatsHeatingAvailability, str]] = ..., front_seats_ventilation: _Optional[_Union[FrontSeatsVentilationAvailability, str]] = ..., second_row_heated_seats: _Optional[_Union[SecondRowHeatedSeatsAvailability, str]] = ..., rear_seat_config: _Optional[_Union[RearSeatConfig, str]] = ..., heated_steering_wheel: _Optional[_Union[HeatedSteeringWheelAvailability, str]] = ...) -> None: ...

class BatteryState(_message.Message):
    __slots__ = ("remaining_range", "charge_percent", "kwhr", "capacity_kwhr", "battery_health", "low_charge_level", "critical_charge_level", "unavailable_range", "preconditioning_status", "preconditioning_time_remaining", "battery_health_level", "bmu_software_version_major", "bmu_software_version_minor", "bmu_software_version_micro", "battery_cell_type", "battery_pack_type", "max_cell_temp", "min_cell_temp")
    REMAINING_RANGE_FIELD_NUMBER: _ClassVar[int]
    CHARGE_PERCENT_FIELD_NUMBER: _ClassVar[int]
    KWHR_FIELD_NUMBER: _ClassVar[int]
    CAPACITY_KWHR_FIELD_NUMBER: _ClassVar[int]
    BATTERY_HEALTH_FIELD_NUMBER: _ClassVar[int]
    LOW_CHARGE_LEVEL_FIELD_NUMBER: _ClassVar[int]
    CRITICAL_CHARGE_LEVEL_FIELD_NUMBER: _ClassVar[int]
    UNAVAILABLE_RANGE_FIELD_NUMBER: _ClassVar[int]
    PRECONDITIONING_STATUS_FIELD_NUMBER: _ClassVar[int]
    PRECONDITIONING_TIME_REMAINING_FIELD_NUMBER: _ClassVar[int]
    BATTERY_HEALTH_LEVEL_FIELD_NUMBER: _ClassVar[int]
    BMU_SOFTWARE_VERSION_MAJOR_FIELD_NUMBER: _ClassVar[int]
    BMU_SOFTWARE_VERSION_MINOR_FIELD_NUMBER: _ClassVar[int]
    BMU_SOFTWARE_VERSION_MICRO_FIELD_NUMBER: _ClassVar[int]
    BATTERY_CELL_TYPE_FIELD_NUMBER: _ClassVar[int]
    BATTERY_PACK_TYPE_FIELD_NUMBER: _ClassVar[int]
    MAX_CELL_TEMP_FIELD_NUMBER: _ClassVar[int]
    MIN_CELL_TEMP_FIELD_NUMBER: _ClassVar[int]
    remaining_range: float
    charge_percent: float
    kwhr: float
    capacity_kwhr: float
    battery_health: WarningState
    low_charge_level: WarningState
    critical_charge_level: WarningState
    unavailable_range: float
    preconditioning_status: BatteryPreconStatus
    preconditioning_time_remaining: int
    battery_health_level: float
    bmu_software_version_major: int
    bmu_software_version_minor: int
    bmu_software_version_micro: int
    battery_cell_type: BatteryCellType
    battery_pack_type: BatteryPackType
    max_cell_temp: float
    min_cell_temp: float
    def __init__(self, remaining_range: _Optional[float] = ..., charge_percent: _Optional[float] = ..., kwhr: _Optional[float] = ..., capacity_kwhr: _Optional[float] = ..., battery_health: _Optional[_Union[WarningState, str]] = ..., low_charge_level: _Optional[_Union[WarningState, str]] = ..., critical_charge_level: _Optional[_Union[WarningState, str]] = ..., unavailable_range: _Optional[float] = ..., preconditioning_status: _Optional[_Union[BatteryPreconStatus, str]] = ..., preconditioning_time_remaining: _Optional[int] = ..., battery_health_level: _Optional[float] = ..., bmu_software_version_major: _Optional[int] = ..., bmu_software_version_minor: _Optional[int] = ..., bmu_software_version_micro: _Optional[int] = ..., battery_cell_type: _Optional[_Union[BatteryCellType, str]] = ..., battery_pack_type: _Optional[_Union[BatteryPackType, str]] = ..., max_cell_temp: _Optional[float] = ..., min_cell_temp: _Optional[float] = ...) -> None: ...

class CabinState(_message.Message):
    __slots__ = ("interior_temp", "exterior_temp")
    INTERIOR_TEMP_FIELD_NUMBER: _ClassVar[int]
    EXTERIOR_TEMP_FIELD_NUMBER: _ClassVar[int]
    interior_temp: float
    exterior_temp: float
    def __init__(self, interior_temp: _Optional[float] = ..., exterior_temp: _Optional[float] = ...) -> None: ...

class WindowPositionState(_message.Message):
    __slots__ = ("left_front", "left_rear", "right_front", "right_rear")
    LEFT_FRONT_FIELD_NUMBER: _ClassVar[int]
    LEFT_REAR_FIELD_NUMBER: _ClassVar[int]
    RIGHT_FRONT_FIELD_NUMBER: _ClassVar[int]
    RIGHT_REAR_FIELD_NUMBER: _ClassVar[int]
    left_front: WindowPositionStatus
    left_rear: WindowPositionStatus
    right_front: WindowPositionStatus
    right_rear: WindowPositionStatus
    def __init__(self, left_front: _Optional[_Union[WindowPositionStatus, str]] = ..., left_rear: _Optional[_Union[WindowPositionStatus, str]] = ..., right_front: _Optional[_Union[WindowPositionStatus, str]] = ..., right_rear: _Optional[_Union[WindowPositionStatus, str]] = ...) -> None: ...

class BodyState(_message.Message):
    __slots__ = ("door_locks", "front_cargo", "rear_cargo", "front_left_door", "front_right_door", "rear_left_door", "rear_right_door", "charge_port", "walkaway_lock", "access_type_status", "keyfob_battery_status", "front_left_mirror_fold_state", "front_right_mirror_fold_state", "all_windows_position", "living_object_detection_status", "window_position")
    DOOR_LOCKS_FIELD_NUMBER: _ClassVar[int]
    FRONT_CARGO_FIELD_NUMBER: _ClassVar[int]
    REAR_CARGO_FIELD_NUMBER: _ClassVar[int]
    FRONT_LEFT_DOOR_FIELD_NUMBER: _ClassVar[int]
    FRONT_RIGHT_DOOR_FIELD_NUMBER: _ClassVar[int]
    REAR_LEFT_DOOR_FIELD_NUMBER: _ClassVar[int]
    REAR_RIGHT_DOOR_FIELD_NUMBER: _ClassVar[int]
    CHARGE_PORT_FIELD_NUMBER: _ClassVar[int]
    WALKAWAY_LOCK_FIELD_NUMBER: _ClassVar[int]
    ACCESS_TYPE_STATUS_FIELD_NUMBER: _ClassVar[int]
    KEYFOB_BATTERY_STATUS_FIELD_NUMBER: _ClassVar[int]
    FRONT_LEFT_MIRROR_FOLD_STATE_FIELD_NUMBER: _ClassVar[int]
    FRONT_RIGHT_MIRROR_FOLD_STATE_FIELD_NUMBER: _ClassVar[int]
    ALL_WINDOWS_POSITION_FIELD_NUMBER: _ClassVar[int]
    LIVING_OBJECT_DETECTION_STATUS_FIELD_NUMBER: _ClassVar[int]
    WINDOW_POSITION_FIELD_NUMBER: _ClassVar[int]
    door_locks: LockState
    front_cargo: DoorState
    rear_cargo: DoorState
    front_left_door: DoorState
    front_right_door: DoorState
    rear_left_door: DoorState
    rear_right_door: DoorState
    charge_port: DoorState
    walkaway_lock: WalkawayState
    access_type_status: AccessRequest
    keyfob_battery_status: KeyfobBatteryStatus
    front_left_mirror_fold_state: MirrorFoldState
    front_right_mirror_fold_state: MirrorFoldState
    all_windows_position: AllWindowPosition
    living_object_detection_status: LivingObjectDetectionStatus
    window_position: WindowPositionState
    def __init__(self, door_locks: _Optional[_Union[LockState, str]] = ..., front_cargo: _Optional[_Union[DoorState, str]] = ..., rear_cargo: _Optional[_Union[DoorState, str]] = ..., front_left_door: _Optional[_Union[DoorState, str]] = ..., front_right_door: _Optional[_Union[DoorState, str]] = ..., rear_left_door: _Optional[_Union[DoorState, str]] = ..., rear_right_door: _Optional[_Union[DoorState, str]] = ..., charge_port: _Optional[_Union[DoorState, str]] = ..., walkaway_lock: _Optional[_Union[WalkawayState, str]] = ..., access_type_status: _Optional[_Union[AccessRequest, str]] = ..., keyfob_battery_status: _Optional[_Union[KeyfobBatteryStatus, str]] = ..., front_left_mirror_fold_state: _Optional[_Union[MirrorFoldState, str]] = ..., front_right_mirror_fold_state: _Optional[_Union[MirrorFoldState, str]] = ..., all_windows_position: _Optional[_Union[AllWindowPosition, str]] = ..., living_object_detection_status: _Optional[_Union[LivingObjectDetectionStatus, str]] = ..., window_position: _Optional[_Union[WindowPositionState, _Mapping]] = ...) -> None: ...

class ChassisState(_message.Message):
    __slots__ = ("odometer_km", "front_left_tire_pressure_bar", "front_right_tire_pressure_bar", "rear_left_tire_pressure_bar", "rear_right_tire_pressure_bar", "headlights", "hard_warn_left_front", "hard_warn_left_rear", "hard_warn_right_front", "hard_warn_right_rear", "soft_warn_left_front", "soft_warn_left_rear", "soft_warn_right_front", "soft_warn_right_rear", "software_version", "speed", "sensor_defective_left_front", "sensor_defective_left_rear", "sensor_defective_right_front", "sensor_defective_right_rear", "tire_pressure_last_updated")
    ODOMETER_KM_FIELD_NUMBER: _ClassVar[int]
    FRONT_LEFT_TIRE_PRESSURE_BAR_FIELD_NUMBER: _ClassVar[int]
    FRONT_RIGHT_TIRE_PRESSURE_BAR_FIELD_NUMBER: _ClassVar[int]
    REAR_LEFT_TIRE_PRESSURE_BAR_FIELD_NUMBER: _ClassVar[int]
    REAR_RIGHT_TIRE_PRESSURE_BAR_FIELD_NUMBER: _ClassVar[int]
    HEADLIGHTS_FIELD_NUMBER: _ClassVar[int]
    HARD_WARN_LEFT_FRONT_FIELD_NUMBER: _ClassVar[int]
    HARD_WARN_LEFT_REAR_FIELD_NUMBER: _ClassVar[int]
    HARD_WARN_RIGHT_FRONT_FIELD_NUMBER: _ClassVar[int]
    HARD_WARN_RIGHT_REAR_FIELD_NUMBER: _ClassVar[int]
    SOFT_WARN_LEFT_FRONT_FIELD_NUMBER: _ClassVar[int]
    SOFT_WARN_LEFT_REAR_FIELD_NUMBER: _ClassVar[int]
    SOFT_WARN_RIGHT_FRONT_FIELD_NUMBER: _ClassVar[int]
    SOFT_WARN_RIGHT_REAR_FIELD_NUMBER: _ClassVar[int]
    SOFTWARE_VERSION_FIELD_NUMBER: _ClassVar[int]
    SPEED_FIELD_NUMBER: _ClassVar[int]
    SENSOR_DEFECTIVE_LEFT_FRONT_FIELD_NUMBER: _ClassVar[int]
    SENSOR_DEFECTIVE_LEFT_REAR_FIELD_NUMBER: _ClassVar[int]
    SENSOR_DEFECTIVE_RIGHT_FRONT_FIELD_NUMBER: _ClassVar[int]
    SENSOR_DEFECTIVE_RIGHT_REAR_FIELD_NUMBER: _ClassVar[int]
    TIRE_PRESSURE_LAST_UPDATED_FIELD_NUMBER: _ClassVar[int]
    odometer_km: float
    front_left_tire_pressure_bar: float
    front_right_tire_pressure_bar: float
    rear_left_tire_pressure_bar: float
    rear_right_tire_pressure_bar: float
    headlights: LightState
    hard_warn_left_front: WarningState
    hard_warn_left_rear: WarningState
    hard_warn_right_front: WarningState
    hard_warn_right_rear: WarningState
    soft_warn_left_front: WarningState
    soft_warn_left_rear: WarningState
    soft_warn_right_front: WarningState
    soft_warn_right_rear: WarningState
    software_version: str
    speed: float
    sensor_defective_left_front: TirePressureSensorDefective
    sensor_defective_left_rear: TirePressureSensorDefective
    sensor_defective_right_front: TirePressureSensorDefective
    sensor_defective_right_rear: TirePressureSensorDefective
    tire_pressure_last_updated: int
    def __init__(self, odometer_km: _Optional[float] = ..., front_left_tire_pressure_bar: _Optional[float] = ..., front_right_tire_pressure_bar: _Optional[float] = ..., rear_left_tire_pressure_bar: _Optional[float] = ..., rear_right_tire_pressure_bar: _Optional[float] = ..., headlights: _Optional[_Union[LightState, str]] = ..., hard_warn_left_front: _Optional[_Union[WarningState, str]] = ..., hard_warn_left_rear: _Optional[_Union[WarningState, str]] = ..., hard_warn_right_front: _Optional[_Union[WarningState, str]] = ..., hard_warn_right_rear: _Optional[_Union[WarningState, str]] = ..., soft_warn_left_front: _Optional[_Union[WarningState, str]] = ..., soft_warn_left_rear: _Optional[_Union[WarningState, str]] = ..., soft_warn_right_front: _Optional[_Union[WarningState, str]] = ..., soft_warn_right_rear: _Optional[_Union[WarningState, str]] = ..., software_version: _Optional[str] = ..., speed: _Optional[float] = ..., sensor_defective_left_front: _Optional[_Union[TirePressureSensorDefective, str]] = ..., sensor_defective_left_rear: _Optional[_Union[TirePressureSensorDefective, str]] = ..., sensor_defective_right_front: _Optional[_Union[TirePressureSensorDefective, str]] = ..., sensor_defective_right_rear: _Optional[_Union[TirePressureSensorDefective, str]] = ..., tire_pressure_last_updated: _Optional[int] = ...) -> None: ...

class ChargingState(_message.Message):
    __slots__ = ("charge_state", "energy_type", "charge_session_mi", "charge_session_kwh", "session_minutes_remaining", "charge_limit", "cable_lock", "charge_rate_kwh_precise", "charge_rate_mph_precise", "charge_rate_miles_min_precise", "charge_limit_percent", "charge_scheduled_time", "scheduled_charge", "scheduled_charge_unavailable", "port_power", "ac_outlet_unavailable_reason", "discharge_command", "discharge_soe_limit", "discharge_target_soe", "discharge_energy", "active_session_ac_current_limit", "energy_ac_current_limit", "ea_pnc_status", "charging_session_restart_allowed")
    CHARGE_STATE_FIELD_NUMBER: _ClassVar[int]
    ENERGY_TYPE_FIELD_NUMBER: _ClassVar[int]
    CHARGE_SESSION_MI_FIELD_NUMBER: _ClassVar[int]
    CHARGE_SESSION_KWH_FIELD_NUMBER: _ClassVar[int]
    SESSION_MINUTES_REMAINING_FIELD_NUMBER: _ClassVar[int]
    CHARGE_LIMIT_FIELD_NUMBER: _ClassVar[int]
    CABLE_LOCK_FIELD_NUMBER: _ClassVar[int]
    CHARGE_RATE_KWH_PRECISE_FIELD_NUMBER: _ClassVar[int]
    CHARGE_RATE_MPH_PRECISE_FIELD_NUMBER: _ClassVar[int]
    CHARGE_RATE_MILES_MIN_PRECISE_FIELD_NUMBER: _ClassVar[int]
    CHARGE_LIMIT_PERCENT_FIELD_NUMBER: _ClassVar[int]
    CHARGE_SCHEDULED_TIME_FIELD_NUMBER: _ClassVar[int]
    SCHEDULED_CHARGE_FIELD_NUMBER: _ClassVar[int]
    SCHEDULED_CHARGE_UNAVAILABLE_FIELD_NUMBER: _ClassVar[int]
    PORT_POWER_FIELD_NUMBER: _ClassVar[int]
    AC_OUTLET_UNAVAILABLE_REASON_FIELD_NUMBER: _ClassVar[int]
    DISCHARGE_COMMAND_FIELD_NUMBER: _ClassVar[int]
    DISCHARGE_SOE_LIMIT_FIELD_NUMBER: _ClassVar[int]
    DISCHARGE_TARGET_SOE_FIELD_NUMBER: _ClassVar[int]
    DISCHARGE_ENERGY_FIELD_NUMBER: _ClassVar[int]
    ACTIVE_SESSION_AC_CURRENT_LIMIT_FIELD_NUMBER: _ClassVar[int]
    ENERGY_AC_CURRENT_LIMIT_FIELD_NUMBER: _ClassVar[int]
    EA_PNC_STATUS_FIELD_NUMBER: _ClassVar[int]
    CHARGING_SESSION_RESTART_ALLOWED_FIELD_NUMBER: _ClassVar[int]
    charge_state: ChargeState
    energy_type: EnergyType
    charge_session_mi: float
    charge_session_kwh: float
    session_minutes_remaining: int
    charge_limit: int
    cable_lock: LockState
    charge_rate_kwh_precise: float
    charge_rate_mph_precise: float
    charge_rate_miles_min_precise: float
    charge_limit_percent: float
    charge_scheduled_time: int
    scheduled_charge: ScheduledChargeState
    scheduled_charge_unavailable: ScheduledChargeUnavailableState
    port_power: float
    ac_outlet_unavailable_reason: AcOutletUnavailableReason
    discharge_command: MobileDischargingCommand
    discharge_soe_limit: int
    discharge_target_soe: int
    discharge_energy: float
    active_session_ac_current_limit: int
    energy_ac_current_limit: int
    ea_pnc_status: EaPncStatus
    charging_session_restart_allowed: ChargingSessionRestartAllowed
    def __init__(self, charge_state: _Optional[_Union[ChargeState, str]] = ..., energy_type: _Optional[_Union[EnergyType, str]] = ..., charge_session_mi: _Optional[float] = ..., charge_session_kwh: _Optional[float] = ..., session_minutes_remaining: _Optional[int] = ..., charge_limit: _Optional[int] = ..., cable_lock: _Optional[_Union[LockState, str]] = ..., charge_rate_kwh_precise: _Optional[float] = ..., charge_rate_mph_precise: _Optional[float] = ..., charge_rate_miles_min_precise: _Optional[float] = ..., charge_limit_percent: _Optional[float] = ..., charge_scheduled_time: _Optional[int] = ..., scheduled_charge: _Optional[_Union[ScheduledChargeState, str]] = ..., scheduled_charge_unavailable: _Optional[_Union[ScheduledChargeUnavailableState, str]] = ..., port_power: _Optional[float] = ..., ac_outlet_unavailable_reason: _Optional[_Union[AcOutletUnavailableReason, str]] = ..., discharge_command: _Optional[_Union[MobileDischargingCommand, str]] = ..., discharge_soe_limit: _Optional[int] = ..., discharge_target_soe: _Optional[int] = ..., discharge_energy: _Optional[float] = ..., active_session_ac_current_limit: _Optional[int] = ..., energy_ac_current_limit: _Optional[int] = ..., ea_pnc_status: _Optional[_Union[EaPncStatus, str]] = ..., charging_session_restart_allowed: _Optional[_Union[ChargingSessionRestartAllowed, str]] = ...) -> None: ...

class Location(_message.Message):
    __slots__ = ("latitude", "longitude")
    LATITUDE_FIELD_NUMBER: _ClassVar[int]
    LONGITUDE_FIELD_NUMBER: _ClassVar[int]
    latitude: float
    longitude: float
    def __init__(self, latitude: _Optional[float] = ..., longitude: _Optional[float] = ...) -> None: ...

class Gps(_message.Message):
    __slots__ = ("location", "elevation", "position_time", "heading_precise")
    LOCATION_FIELD_NUMBER: _ClassVar[int]
    ELEVATION_FIELD_NUMBER: _ClassVar[int]
    POSITION_TIME_FIELD_NUMBER: _ClassVar[int]
    HEADING_PRECISE_FIELD_NUMBER: _ClassVar[int]
    location: Location
    elevation: int
    position_time: int
    heading_precise: float
    def __init__(self, location: _Optional[_Union[Location, _Mapping]] = ..., elevation: _Optional[int] = ..., position_time: _Optional[int] = ..., heading_precise: _Optional[float] = ...) -> None: ...

class SoftwareUpdate(_message.Message):
    __slots__ = ("version_available", "install_duration_minutes", "percent_complete", "state", "version_available_raw", "update_available", "scheduled_start_time_sec", "tcu_download_status")
    VERSION_AVAILABLE_FIELD_NUMBER: _ClassVar[int]
    INSTALL_DURATION_MINUTES_FIELD_NUMBER: _ClassVar[int]
    PERCENT_COMPLETE_FIELD_NUMBER: _ClassVar[int]
    STATE_FIELD_NUMBER: _ClassVar[int]
    VERSION_AVAILABLE_RAW_FIELD_NUMBER: _ClassVar[int]
    UPDATE_AVAILABLE_FIELD_NUMBER: _ClassVar[int]
    SCHEDULED_START_TIME_SEC_FIELD_NUMBER: _ClassVar[int]
    TCU_DOWNLOAD_STATUS_FIELD_NUMBER: _ClassVar[int]
    version_available: str
    install_duration_minutes: int
    percent_complete: int
    state: UpdateState
    version_available_raw: int
    update_available: UpdateAvailability
    scheduled_start_time_sec: int
    tcu_download_status: TcuDownloadStatus
    def __init__(self, version_available: _Optional[str] = ..., install_duration_minutes: _Optional[int] = ..., percent_complete: _Optional[int] = ..., state: _Optional[_Union[UpdateState, str]] = ..., version_available_raw: _Optional[int] = ..., update_available: _Optional[_Union[UpdateAvailability, str]] = ..., scheduled_start_time_sec: _Optional[int] = ..., tcu_download_status: _Optional[_Union[TcuDownloadStatus, str]] = ...) -> None: ...

class AlarmState(_message.Message):
    __slots__ = ("status", "mode")
    STATUS_FIELD_NUMBER: _ClassVar[int]
    MODE_FIELD_NUMBER: _ClassVar[int]
    status: AlarmStatus
    mode: AlarmMode
    def __init__(self, status: _Optional[_Union[AlarmStatus, str]] = ..., mode: _Optional[_Union[AlarmMode, str]] = ...) -> None: ...

class SeatClimateState(_message.Message):
    __slots__ = ("driver_heat_backrest_zone1", "driver_heat_backrest_zone3", "driver_heat_cushion_zone2", "driver_heat_cushion_zone4", "driver_vent_backrest", "driver_vent_cushion", "front_passenger_heat_backrest_zone1", "front_passenger_heat_backrest_zone3", "front_passenger_heat_cushion_zone2", "front_passenger_heat_cushion_zone4", "front_passenger_vent_backrest", "front_passenger_vent_cushion", "rear_passenger_heat_left", "rear_passenger_heat_center", "rear_passenger_heat_right")
    DRIVER_HEAT_BACKREST_ZONE1_FIELD_NUMBER: _ClassVar[int]
    DRIVER_HEAT_BACKREST_ZONE3_FIELD_NUMBER: _ClassVar[int]
    DRIVER_HEAT_CUSHION_ZONE2_FIELD_NUMBER: _ClassVar[int]
    DRIVER_HEAT_CUSHION_ZONE4_FIELD_NUMBER: _ClassVar[int]
    DRIVER_VENT_BACKREST_FIELD_NUMBER: _ClassVar[int]
    DRIVER_VENT_CUSHION_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_HEAT_BACKREST_ZONE1_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_HEAT_BACKREST_ZONE3_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_HEAT_CUSHION_ZONE2_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_HEAT_CUSHION_ZONE4_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_VENT_BACKREST_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_VENT_CUSHION_FIELD_NUMBER: _ClassVar[int]
    REAR_PASSENGER_HEAT_LEFT_FIELD_NUMBER: _ClassVar[int]
    REAR_PASSENGER_HEAT_CENTER_FIELD_NUMBER: _ClassVar[int]
    REAR_PASSENGER_HEAT_RIGHT_FIELD_NUMBER: _ClassVar[int]
    driver_heat_backrest_zone1: SeatClimateMode
    driver_heat_backrest_zone3: SeatClimateMode
    driver_heat_cushion_zone2: SeatClimateMode
    driver_heat_cushion_zone4: SeatClimateMode
    driver_vent_backrest: SeatClimateMode
    driver_vent_cushion: SeatClimateMode
    front_passenger_heat_backrest_zone1: SeatClimateMode
    front_passenger_heat_backrest_zone3: SeatClimateMode
    front_passenger_heat_cushion_zone2: SeatClimateMode
    front_passenger_heat_cushion_zone4: SeatClimateMode
    front_passenger_vent_backrest: SeatClimateMode
    front_passenger_vent_cushion: SeatClimateMode
    rear_passenger_heat_left: SeatClimateMode
    rear_passenger_heat_center: SeatClimateMode
    rear_passenger_heat_right: SeatClimateMode
    def __init__(self, driver_heat_backrest_zone1: _Optional[_Union[SeatClimateMode, str]] = ..., driver_heat_backrest_zone3: _Optional[_Union[SeatClimateMode, str]] = ..., driver_heat_cushion_zone2: _Optional[_Union[SeatClimateMode, str]] = ..., driver_heat_cushion_zone4: _Optional[_Union[SeatClimateMode, str]] = ..., driver_vent_backrest: _Optional[_Union[SeatClimateMode, str]] = ..., driver_vent_cushion: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_heat_backrest_zone1: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_heat_backrest_zone3: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_heat_cushion_zone2: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_heat_cushion_zone4: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_vent_backrest: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_vent_cushion: _Optional[_Union[SeatClimateMode, str]] = ..., rear_passenger_heat_left: _Optional[_Union[SeatClimateMode, str]] = ..., rear_passenger_heat_center: _Optional[_Union[SeatClimateMode, str]] = ..., rear_passenger_heat_right: _Optional[_Union[SeatClimateMode, str]] = ...) -> None: ...

class HvacState(_message.Message):
    __slots__ = ("power", "defrost", "precondition_status", "keep_climate_status", "max_ac_status", "seats", "sync_set", "rear_window_heating_status", "steering_heater", "steering_heater_level", "front_left_set_temperature", "hvac_limited")
    POWER_FIELD_NUMBER: _ClassVar[int]
    DEFROST_FIELD_NUMBER: _ClassVar[int]
    PRECONDITION_STATUS_FIELD_NUMBER: _ClassVar[int]
    KEEP_CLIMATE_STATUS_FIELD_NUMBER: _ClassVar[int]
    MAX_AC_STATUS_FIELD_NUMBER: _ClassVar[int]
    SEATS_FIELD_NUMBER: _ClassVar[int]
    SYNC_SET_FIELD_NUMBER: _ClassVar[int]
    REAR_WINDOW_HEATING_STATUS_FIELD_NUMBER: _ClassVar[int]
    STEERING_HEATER_FIELD_NUMBER: _ClassVar[int]
    STEERING_HEATER_LEVEL_FIELD_NUMBER: _ClassVar[int]
    FRONT_LEFT_SET_TEMPERATURE_FIELD_NUMBER: _ClassVar[int]
    HVAC_LIMITED_FIELD_NUMBER: _ClassVar[int]
    power: HvacPower
    defrost: DefrostState
    precondition_status: HvacPreconditionStatus
    keep_climate_status: KeepClimateStatus
    max_ac_status: MaxACState
    seats: SeatClimateState
    sync_set: SyncSet
    rear_window_heating_status: RearWindowHeatingStatus
    steering_heater: SteeringHeaterStatus
    steering_heater_level: SteeringWheelHeaterLevel
    front_left_set_temperature: float
    hvac_limited: HvacLimited
    def __init__(self, power: _Optional[_Union[HvacPower, str]] = ..., defrost: _Optional[_Union[DefrostState, str]] = ..., precondition_status: _Optional[_Union[HvacPreconditionStatus, str]] = ..., keep_climate_status: _Optional[_Union[KeepClimateStatus, str]] = ..., max_ac_status: _Optional[_Union[MaxACState, str]] = ..., seats: _Optional[_Union[SeatClimateState, _Mapping]] = ..., sync_set: _Optional[_Union[SyncSet, str]] = ..., rear_window_heating_status: _Optional[_Union[RearWindowHeatingStatus, str]] = ..., steering_heater: _Optional[_Union[SteeringHeaterStatus, str]] = ..., steering_heater_level: _Optional[_Union[SteeringWheelHeaterLevel, str]] = ..., front_left_set_temperature: _Optional[float] = ..., hvac_limited: _Optional[_Union[HvacLimited, str]] = ...) -> None: ...

class MobileAppReqState(_message.Message):
    __slots__ = ("alarm_set_request", "charge_port_request", "frunk_cargo_request", "horn_request", "hvac_defrost", "hvac_precondition", "light_request", "panic_request", "shared_trip_request", "trunk_cargo_request", "vehicle_unlock_request")
    ALARM_SET_REQUEST_FIELD_NUMBER: _ClassVar[int]
    CHARGE_PORT_REQUEST_FIELD_NUMBER: _ClassVar[int]
    FRUNK_CARGO_REQUEST_FIELD_NUMBER: _ClassVar[int]
    HORN_REQUEST_FIELD_NUMBER: _ClassVar[int]
    HVAC_DEFROST_FIELD_NUMBER: _ClassVar[int]
    HVAC_PRECONDITION_FIELD_NUMBER: _ClassVar[int]
    LIGHT_REQUEST_FIELD_NUMBER: _ClassVar[int]
    PANIC_REQUEST_FIELD_NUMBER: _ClassVar[int]
    SHARED_TRIP_REQUEST_FIELD_NUMBER: _ClassVar[int]
    TRUNK_CARGO_REQUEST_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_UNLOCK_REQUEST_FIELD_NUMBER: _ClassVar[int]
    alarm_set_request: AlarmMode
    charge_port_request: DoorState
    frunk_cargo_request: DoorState
    horn_request: DoorState
    hvac_defrost: DefrostState
    hvac_precondition: HvacPower
    light_request: LightAction
    panic_request: PanicState
    shared_trip_request: SharedTripState
    trunk_cargo_request: DoorState
    vehicle_unlock_request: LockState
    def __init__(self, alarm_set_request: _Optional[_Union[AlarmMode, str]] = ..., charge_port_request: _Optional[_Union[DoorState, str]] = ..., frunk_cargo_request: _Optional[_Union[DoorState, str]] = ..., horn_request: _Optional[_Union[DoorState, str]] = ..., hvac_defrost: _Optional[_Union[DefrostState, str]] = ..., hvac_precondition: _Optional[_Union[HvacPower, str]] = ..., light_request: _Optional[_Union[LightAction, str]] = ..., panic_request: _Optional[_Union[PanicState, str]] = ..., shared_trip_request: _Optional[_Union[SharedTripState, str]] = ..., trunk_cargo_request: _Optional[_Union[DoorState, str]] = ..., vehicle_unlock_request: _Optional[_Union[LockState, str]] = ...) -> None: ...

class TcuInternetState(_message.Message):
    __slots__ = ("lte_type", "lte_status", "wifi_status", "lte_rssi", "wifi_rssi")
    LTE_TYPE_FIELD_NUMBER: _ClassVar[int]
    LTE_STATUS_FIELD_NUMBER: _ClassVar[int]
    WIFI_STATUS_FIELD_NUMBER: _ClassVar[int]
    LTE_RSSI_FIELD_NUMBER: _ClassVar[int]
    WIFI_RSSI_FIELD_NUMBER: _ClassVar[int]
    lte_type: LteType
    lte_status: InternetStatus
    wifi_status: InternetStatus
    lte_rssi: int
    wifi_rssi: int
    def __init__(self, lte_type: _Optional[_Union[LteType, str]] = ..., lte_status: _Optional[_Union[InternetStatus, str]] = ..., wifi_status: _Optional[_Union[InternetStatus, str]] = ..., lte_rssi: _Optional[int] = ..., wifi_rssi: _Optional[int] = ...) -> None: ...

class FaultState(_message.Message):
    __slots__ = ("mpb_fault_status",)
    MPB_FAULT_STATUS_FIELD_NUMBER: _ClassVar[int]
    mpb_fault_status: MpbFaultStatus
    def __init__(self, mpb_fault_status: _Optional[_Union[MpbFaultStatus, str]] = ...) -> None: ...

class Notifications(_message.Message):
    __slots__ = ("powertrain_message", "powertrain_notify_status", "charging_general_status", "battery_charge_status")
    POWERTRAIN_MESSAGE_FIELD_NUMBER: _ClassVar[int]
    POWERTRAIN_NOTIFY_STATUS_FIELD_NUMBER: _ClassVar[int]
    CHARGING_GENERAL_STATUS_FIELD_NUMBER: _ClassVar[int]
    BATTERY_CHARGE_STATUS_FIELD_NUMBER: _ClassVar[int]
    powertrain_message: PowertrainMessage
    powertrain_notify_status: PowertrainNotifyStatus
    charging_general_status: GeneralChargeStatus
    battery_charge_status: GeneralChargeStatus
    def __init__(self, powertrain_message: _Optional[_Union[PowertrainMessage, str]] = ..., powertrain_notify_status: _Optional[_Union[PowertrainNotifyStatus, str]] = ..., charging_general_status: _Optional[_Union[GeneralChargeStatus, str]] = ..., battery_charge_status: _Optional[_Union[GeneralChargeStatus, str]] = ...) -> None: ...

class MultiplexValues(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SentryState(_message.Message):
    __slots__ = ("enablement_state", "threat_level", "multiplex_values", "usb_drive_status", "enhanced_deterrence_state", "range_cost_per_day", "remote_alarm_state")
    ENABLEMENT_STATE_FIELD_NUMBER: _ClassVar[int]
    THREAT_LEVEL_FIELD_NUMBER: _ClassVar[int]
    MULTIPLEX_VALUES_FIELD_NUMBER: _ClassVar[int]
    USB_DRIVE_STATUS_FIELD_NUMBER: _ClassVar[int]
    ENHANCED_DETERRENCE_STATE_FIELD_NUMBER: _ClassVar[int]
    RANGE_COST_PER_DAY_FIELD_NUMBER: _ClassVar[int]
    REMOTE_ALARM_STATE_FIELD_NUMBER: _ClassVar[int]
    enablement_state: EnablementState
    threat_level: SentryThreat
    multiplex_values: MultiplexValues
    usb_drive_status: SentryUsbDriveStatus
    enhanced_deterrence_state: EnhancedDeterrenceState
    range_cost_per_day: int
    remote_alarm_state: SentryRemoteAlarmState
    def __init__(self, enablement_state: _Optional[_Union[EnablementState, str]] = ..., threat_level: _Optional[_Union[SentryThreat, str]] = ..., multiplex_values: _Optional[_Union[MultiplexValues, _Mapping]] = ..., usb_drive_status: _Optional[_Union[SentryUsbDriveStatus, str]] = ..., enhanced_deterrence_state: _Optional[_Union[EnhancedDeterrenceState, str]] = ..., range_cost_per_day: _Optional[int] = ..., remote_alarm_state: _Optional[_Union[SentryRemoteAlarmState, str]] = ...) -> None: ...

class VehicleState(_message.Message):
    __slots__ = ("battery", "power", "cabin", "body", "last_updated_ms", "chassis", "charging", "gps", "software_update", "alarm", "cloud_connection", "keyless_driving", "hvac", "drive_mode", "privacy_mode", "gear_position", "mobile_app_request", "tcu", "tcu_internet", "sentry_state", "fault_state", "notifications", "low_power_mode_status")
    BATTERY_FIELD_NUMBER: _ClassVar[int]
    POWER_FIELD_NUMBER: _ClassVar[int]
    CABIN_FIELD_NUMBER: _ClassVar[int]
    BODY_FIELD_NUMBER: _ClassVar[int]
    LAST_UPDATED_MS_FIELD_NUMBER: _ClassVar[int]
    CHASSIS_FIELD_NUMBER: _ClassVar[int]
    CHARGING_FIELD_NUMBER: _ClassVar[int]
    GPS_FIELD_NUMBER: _ClassVar[int]
    SOFTWARE_UPDATE_FIELD_NUMBER: _ClassVar[int]
    ALARM_FIELD_NUMBER: _ClassVar[int]
    CLOUD_CONNECTION_FIELD_NUMBER: _ClassVar[int]
    KEYLESS_DRIVING_FIELD_NUMBER: _ClassVar[int]
    HVAC_FIELD_NUMBER: _ClassVar[int]
    DRIVE_MODE_FIELD_NUMBER: _ClassVar[int]
    PRIVACY_MODE_FIELD_NUMBER: _ClassVar[int]
    GEAR_POSITION_FIELD_NUMBER: _ClassVar[int]
    MOBILE_APP_REQUEST_FIELD_NUMBER: _ClassVar[int]
    TCU_FIELD_NUMBER: _ClassVar[int]
    TCU_INTERNET_FIELD_NUMBER: _ClassVar[int]
    SENTRY_STATE_FIELD_NUMBER: _ClassVar[int]
    FAULT_STATE_FIELD_NUMBER: _ClassVar[int]
    NOTIFICATIONS_FIELD_NUMBER: _ClassVar[int]
    LOW_POWER_MODE_STATUS_FIELD_NUMBER: _ClassVar[int]
    battery: BatteryState
    power: PowerState
    cabin: CabinState
    body: BodyState
    last_updated_ms: int
    chassis: ChassisState
    charging: ChargingState
    gps: Gps
    software_update: SoftwareUpdate
    alarm: AlarmState
    cloud_connection: CloudConnectionState
    keyless_driving: KeylessDrivingState
    hvac: HvacState
    drive_mode: DriveMode
    privacy_mode: PrivacyMode
    gear_position: GearPosition
    mobile_app_request: MobileAppReqState
    tcu: TcuState
    tcu_internet: TcuInternetState
    sentry_state: SentryState
    fault_state: FaultState
    notifications: Notifications
    low_power_mode_status: LowPowerModeStatus
    def __init__(self, battery: _Optional[_Union[BatteryState, _Mapping]] = ..., power: _Optional[_Union[PowerState, str]] = ..., cabin: _Optional[_Union[CabinState, _Mapping]] = ..., body: _Optional[_Union[BodyState, _Mapping]] = ..., last_updated_ms: _Optional[int] = ..., chassis: _Optional[_Union[ChassisState, _Mapping]] = ..., charging: _Optional[_Union[ChargingState, _Mapping]] = ..., gps: _Optional[_Union[Gps, _Mapping]] = ..., software_update: _Optional[_Union[SoftwareUpdate, _Mapping]] = ..., alarm: _Optional[_Union[AlarmState, _Mapping]] = ..., cloud_connection: _Optional[_Union[CloudConnectionState, str]] = ..., keyless_driving: _Optional[_Union[KeylessDrivingState, str]] = ..., hvac: _Optional[_Union[HvacState, _Mapping]] = ..., drive_mode: _Optional[_Union[DriveMode, str]] = ..., privacy_mode: _Optional[_Union[PrivacyMode, str]] = ..., gear_position: _Optional[_Union[GearPosition, str]] = ..., mobile_app_request: _Optional[_Union[MobileAppReqState, _Mapping]] = ..., tcu: _Optional[_Union[TcuState, str]] = ..., tcu_internet: _Optional[_Union[TcuInternetState, _Mapping]] = ..., sentry_state: _Optional[_Union[SentryState, _Mapping]] = ..., fault_state: _Optional[_Union[FaultState, _Mapping]] = ..., notifications: _Optional[_Union[Notifications, _Mapping]] = ..., low_power_mode_status: _Optional[_Union[LowPowerModeStatus, str]] = ...) -> None: ...

class Vehicle(_message.Message):
    __slots__ = ("vehicle_id", "access_level", "config", "state")
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    ACCESS_LEVEL_FIELD_NUMBER: _ClassVar[int]
    CONFIG_FIELD_NUMBER: _ClassVar[int]
    STATE_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    access_level: AccessLevel
    config: VehicleConfig
    state: VehicleState
    def __init__(self, vehicle_id: _Optional[str] = ..., access_level: _Optional[_Union[AccessLevel, str]] = ..., config: _Optional[_Union[VehicleConfig, _Mapping]] = ..., state: _Optional[_Union[VehicleState, _Mapping]] = ...) -> None: ...

class ApplySoftwareUpdateRequest(_message.Message):
    __slots__ = ("vehicle_id",)
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    def __init__(self, vehicle_id: _Optional[str] = ...) -> None: ...

class ApplySoftwareUpdateResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class CancelScheduledUpdateRequest(_message.Message):
    __slots__ = ("vehicle_id",)
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    def __init__(self, vehicle_id: _Optional[str] = ...) -> None: ...

class CancelScheduledUpdateResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class ChargeControlRequest(_message.Message):
    __slots__ = ("action", "vehicle_id")
    ACTION_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    action: ChargeAction
    vehicle_id: str
    def __init__(self, action: _Optional[_Union[ChargeAction, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class ChargeControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class ControlChargePortRequest(_message.Message):
    __slots__ = ("closure_state", "vehicle_id")
    CLOSURE_STATE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    closure_state: DoorState
    vehicle_id: str
    def __init__(self, closure_state: _Optional[_Union[DoorState, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class ControlChargePortResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class DoorLocksControlRequest(_message.Message):
    __slots__ = ("door_location", "lock_state", "vehicle_id")
    DOOR_LOCATION_FIELD_NUMBER: _ClassVar[int]
    LOCK_STATE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    door_location: _containers.RepeatedScalarFieldContainer[int]
    lock_state: LockState
    vehicle_id: str
    def __init__(self, door_location: _Optional[_Iterable[int]] = ..., lock_state: _Optional[_Union[LockState, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class DoorLocksControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class FrontCargoControlRequest(_message.Message):
    __slots__ = ("closure_state", "vehicle_id")
    CLOSURE_STATE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    closure_state: DoorState
    vehicle_id: str
    def __init__(self, closure_state: _Optional[_Union[DoorState, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class FrontCargoControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class DocumentInfoUnknown(_message.Message):
    __slots__ = ("timestamp",)
    TIMESTAMP_FIELD_NUMBER: _ClassVar[int]
    timestamp: int
    def __init__(self, timestamp: _Optional[int] = ...) -> None: ...

class DocumentInfo(_message.Message):
    __slots__ = ("type", "version", "description", "unknown")
    TYPE_FIELD_NUMBER: _ClassVar[int]
    VERSION_FIELD_NUMBER: _ClassVar[int]
    DESCRIPTION_FIELD_NUMBER: _ClassVar[int]
    UNKNOWN_FIELD_NUMBER: _ClassVar[int]
    type: DocumentType
    version: str
    description: str
    unknown: DocumentInfoUnknown
    def __init__(self, type: _Optional[_Union[DocumentType, str]] = ..., version: _Optional[str] = ..., description: _Optional[str] = ..., unknown: _Optional[_Union[DocumentInfoUnknown, _Mapping]] = ...) -> None: ...

class GetDocumentInfoRequest(_message.Message):
    __slots__ = ("version", "document_type")
    VERSION_FIELD_NUMBER: _ClassVar[int]
    DOCUMENT_TYPE_FIELD_NUMBER: _ClassVar[int]
    version: str
    document_type: DocumentType
    def __init__(self, version: _Optional[str] = ..., document_type: _Optional[_Union[DocumentType, str]] = ...) -> None: ...

class GetDocumentInfoResponse(_message.Message):
    __slots__ = ("url", "info")
    URL_FIELD_NUMBER: _ClassVar[int]
    INFO_FIELD_NUMBER: _ClassVar[int]
    url: str
    info: DocumentInfo
    def __init__(self, url: _Optional[str] = ..., info: _Optional[_Union[DocumentInfo, _Mapping]] = ...) -> None: ...

class GetVehicleStateRequest(_message.Message):
    __slots__ = ("vehicle_id",)
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    def __init__(self, vehicle_id: _Optional[str] = ...) -> None: ...

class GetVehicleStateResponse(_message.Message):
    __slots__ = ("vehicle_id", "state")
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    STATE_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    state: VehicleState
    def __init__(self, vehicle_id: _Optional[str] = ..., state: _Optional[_Union[VehicleState, _Mapping]] = ...) -> None: ...

class HonkHornRequest(_message.Message):
    __slots__ = ("vehicle_id",)
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    def __init__(self, vehicle_id: _Optional[str] = ...) -> None: ...

class HonkHornResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class HvacDefrostControlRequest(_message.Message):
    __slots__ = ("vehicle_id", "hvac_defrost")
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    HVAC_DEFROST_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    hvac_defrost: DefrostState
    def __init__(self, vehicle_id: _Optional[str] = ..., hvac_defrost: _Optional[_Union[DefrostState, str]] = ...) -> None: ...

class HvacDefrostControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class LightsControlRequest(_message.Message):
    __slots__ = ("action", "vehicle_id")
    ACTION_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    action: LightAction
    vehicle_id: str
    def __init__(self, action: _Optional[_Union[LightAction, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class LightsControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class RearCargoControlRequest(_message.Message):
    __slots__ = ("closure_state", "vehicle_id")
    CLOSURE_STATE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    closure_state: DoorState
    vehicle_id: str
    def __init__(self, closure_state: _Optional[_Union[DoorState, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class RearCargoControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SecurityAlarmControlRequest(_message.Message):
    __slots__ = ("mode", "vehicle_id")
    MODE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    mode: AlarmMode
    vehicle_id: str
    def __init__(self, mode: _Optional[_Union[AlarmMode, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class SecurityAlarmControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SetCabinTemperatureRequest(_message.Message):
    __slots__ = ("temperature", "state", "vehicle_id")
    TEMPERATURE_FIELD_NUMBER: _ClassVar[int]
    STATE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    temperature: float
    state: HvacPower
    vehicle_id: str
    def __init__(self, temperature: _Optional[float] = ..., state: _Optional[_Union[HvacPower, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class SetCabinTemperatureResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SetChargeLimitRequest(_message.Message):
    __slots__ = ("limit_percent", "vehicle_id")
    LIMIT_PERCENT_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    limit_percent: int
    vehicle_id: str
    def __init__(self, limit_percent: _Optional[int] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class SetChargeLimitResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class WakeupVehicleRequest(_message.Message):
    __slots__ = ("vehicle_id",)
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    def __init__(self, vehicle_id: _Optional[str] = ...) -> None: ...

class WakeupVehicleResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SetBatteryPreconRequest(_message.Message):
    __slots__ = ("vehicle_id", "status")
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    status: BatteryPreconStatus
    def __init__(self, vehicle_id: _Optional[str] = ..., status: _Optional[_Union[BatteryPreconStatus, str]] = ...) -> None: ...

class SetBatteryPreconResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SetDischargeSoeLimitRequest(_message.Message):
    __slots__ = ("discharge_soe_limit", "vehicle_id")
    DISCHARGE_SOE_LIMIT_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    discharge_soe_limit: int
    vehicle_id: str
    def __init__(self, discharge_soe_limit: _Optional[int] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class SetDischargeSoeLimitResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class DischargeControlRequest(_message.Message):
    __slots__ = ("discharge_command", "vehicle_id")
    DISCHARGE_COMMAND_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    discharge_command: DischargeCommand
    vehicle_id: str
    def __init__(self, discharge_command: _Optional[_Union[DischargeCommand, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class DischargeControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class AllWindowControlRequest(_message.Message):
    __slots__ = ("state", "vehicle_id")
    STATE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    state: WindowSwitchState
    vehicle_id: str
    def __init__(self, state: _Optional[_Union[WindowSwitchState, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class AllWindowControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SeatClimateControlRequest(_message.Message):
    __slots__ = ("vehicle_id", "driver_heat_backrest_zone1", "driver_heat_backrest_zone3", "driver_heat_cushion_zone2", "driver_heat_cushion_zone4", "driver_vent_backrest", "driver_vent_cushion", "front_passenger_heat_backrest_zone1", "front_passenger_heat_backrest_zone3", "front_passenger_heat_cushion_zone2", "front_passenger_heat_cushion_zone4", "front_passenger_vent_backrest", "front_passenger_vent_cushion", "rear_passenger_heat_left", "rear_passenger_heat_center", "rear_passenger_heat_right")
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    DRIVER_HEAT_BACKREST_ZONE1_FIELD_NUMBER: _ClassVar[int]
    DRIVER_HEAT_BACKREST_ZONE3_FIELD_NUMBER: _ClassVar[int]
    DRIVER_HEAT_CUSHION_ZONE2_FIELD_NUMBER: _ClassVar[int]
    DRIVER_HEAT_CUSHION_ZONE4_FIELD_NUMBER: _ClassVar[int]
    DRIVER_VENT_BACKREST_FIELD_NUMBER: _ClassVar[int]
    DRIVER_VENT_CUSHION_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_HEAT_BACKREST_ZONE1_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_HEAT_BACKREST_ZONE3_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_HEAT_CUSHION_ZONE2_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_HEAT_CUSHION_ZONE4_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_VENT_BACKREST_FIELD_NUMBER: _ClassVar[int]
    FRONT_PASSENGER_VENT_CUSHION_FIELD_NUMBER: _ClassVar[int]
    REAR_PASSENGER_HEAT_LEFT_FIELD_NUMBER: _ClassVar[int]
    REAR_PASSENGER_HEAT_CENTER_FIELD_NUMBER: _ClassVar[int]
    REAR_PASSENGER_HEAT_RIGHT_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    driver_heat_backrest_zone1: SeatClimateMode
    driver_heat_backrest_zone3: SeatClimateMode
    driver_heat_cushion_zone2: SeatClimateMode
    driver_heat_cushion_zone4: SeatClimateMode
    driver_vent_backrest: SeatClimateMode
    driver_vent_cushion: SeatClimateMode
    front_passenger_heat_backrest_zone1: SeatClimateMode
    front_passenger_heat_backrest_zone3: SeatClimateMode
    front_passenger_heat_cushion_zone2: SeatClimateMode
    front_passenger_heat_cushion_zone4: SeatClimateMode
    front_passenger_vent_backrest: SeatClimateMode
    front_passenger_vent_cushion: SeatClimateMode
    rear_passenger_heat_left: SeatClimateMode
    rear_passenger_heat_center: SeatClimateMode
    rear_passenger_heat_right: SeatClimateMode
    def __init__(self, vehicle_id: _Optional[str] = ..., driver_heat_backrest_zone1: _Optional[_Union[SeatClimateMode, str]] = ..., driver_heat_backrest_zone3: _Optional[_Union[SeatClimateMode, str]] = ..., driver_heat_cushion_zone2: _Optional[_Union[SeatClimateMode, str]] = ..., driver_heat_cushion_zone4: _Optional[_Union[SeatClimateMode, str]] = ..., driver_vent_backrest: _Optional[_Union[SeatClimateMode, str]] = ..., driver_vent_cushion: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_heat_backrest_zone1: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_heat_backrest_zone3: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_heat_cushion_zone2: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_heat_cushion_zone4: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_vent_backrest: _Optional[_Union[SeatClimateMode, str]] = ..., front_passenger_vent_cushion: _Optional[_Union[SeatClimateMode, str]] = ..., rear_passenger_heat_left: _Optional[_Union[SeatClimateMode, str]] = ..., rear_passenger_heat_center: _Optional[_Union[SeatClimateMode, str]] = ..., rear_passenger_heat_right: _Optional[_Union[SeatClimateMode, str]] = ...) -> None: ...

class SeatClimateControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SetMaxACRequest(_message.Message):
    __slots__ = ("state", "vehicle_id")
    STATE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    state: MaxACState
    vehicle_id: str
    def __init__(self, state: _Optional[_Union[MaxACState, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class SetMaxACResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SteeringWheelHeaterRequest(_message.Message):
    __slots__ = ("vehicle_id", "level")
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    LEVEL_FIELD_NUMBER: _ClassVar[int]
    vehicle_id: str
    level: SteeringWheelHeaterLevel
    def __init__(self, vehicle_id: _Optional[str] = ..., level: _Optional[_Union[SteeringWheelHeaterLevel, str]] = ...) -> None: ...

class SteeringWheelHeaterResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class SetCreatureComfortModeRequest(_message.Message):
    __slots__ = ("mode", "vehicle_id")
    MODE_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    mode: CreatureComfortMode
    vehicle_id: str
    def __init__(self, mode: _Optional[_Union[CreatureComfortMode, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class SetCreatureComfortModeResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class WelcomeControlRequest(_message.Message):
    __slots__ = ("action", "vehicle_id")
    ACTION_FIELD_NUMBER: _ClassVar[int]
    VEHICLE_ID_FIELD_NUMBER: _ClassVar[int]
    action: WelcomeAction
    vehicle_id: str
    def __init__(self, action: _Optional[_Union[WelcomeAction, str]] = ..., vehicle_id: _Optional[str] = ...) -> None: ...

class WelcomeControlResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...
