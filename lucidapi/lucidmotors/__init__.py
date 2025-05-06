"""The Lucid Motors mobile app API"""

from __future__ import annotations
from typing import Optional, Any, Callable, TypeVar, Awaitable
from datetime import datetime, timezone, timedelta
from grpc.aio import ClientCallDetails, UnaryUnaryCall
from google.protobuf.internal.enum_type_wrapper import EnumTypeWrapper
from base64 import b64encode

import uuid
import grpc
import grpc.aio
import logging

from .const import MOBILE_API_REGIONS, Region
from .exceptions import APIError, APIValueError, StatusCode

from .gen import (
    login_session_pb2,
    login_session_pb2_grpc,
    user_profile_service_pb2,
    user_profile_service_pb2_grpc,
    trip_service_pb2,
    trip_service_pb2_grpc,
    vehicle_state_service_pb2,
    vehicle_state_service_pb2 as VSS,
    vehicle_state_service_pb2_grpc,
    charging_service_pb2,
    charging_service_pb2_grpc,
    salesforce_service_pb2,
    salesforce_service_pb2_grpc,
)
from .gen.login_session_pb2 import NotificationChannelType
from .gen.user_profile_service_pb2 import UserProfile
from .gen.vehicle_state_service_pb2 import (
    AccessLevel,
    Model,
    ModelVariant,
    PaintColor,
    Look,
    Wheels,
    SubscriptionStatus,
    ChargingSubscription,
    ChargingAccountStatus,
    ChargingVendor,
    ChargingAccount,
    Edition,
    BatteryType,
    Interior,
    SpecialIdentifiers,
    Reservation,
    StrutType,
    RoofType,
    VehicleConfig,
    WarningState,
    BatteryPreconStatus,
    BatteryState,
    PowerState,
    CabinState,
    LockState,
    DoorState,
    WalkawayState,
    AccessRequest,
    BodyState,
    LightState,
    LightAction,
    ChassisState,
    ChargeState,
    ChargeAction,
    ScheduledChargeState,
    ScheduledChargeUnavailableState,
    EnergyType,
    ChargingState,
    Location,
    Gps,
    UpdateState,
    SoftwareUpdate,
    AlarmStatus,
    AlarmMode,
    AlarmState,
    CloudConnectionState,
    KeylessDrivingState,
    HvacPower,
    DefrostState,
    HvacPreconditionStatus,
    HvacState,
    DriveMode,
    PrivacyMode,
    GearPosition,
    SharedTripState,
    MobileAppReqState,
    TcuState,
    LteType,
    InternetStatus,
    TcuInternetState,
    VehicleState,
    Vehicle,
    GetDocumentInfoResponse,
    DocumentType,
    WindowSwitchState,
    WindowPositionStatus,
    SeatClimateMode,
    MaxACState,
    SteeringHeaterStatus,
    SteeringWheelHeaterLevel,
    CreatureComfortMode,
    FrontSeatsHeatingAvailability,
    FrontSeatsVentilationAvailability,
    SecondRowHeatedSeatsAvailability,
    RearSeatConfig,
    HeatedSteeringWheelAvailability,
)
from .gen.charging_service_pb2 import (
    DateTime,
    Unknown,
    ImageCategory,
    Image,
    Operator,
    ChargingStatus,
    ConnectorStandard,
    ConnectorFormat,
    PowerType,
    Connector,
    ChargingSession,
    OpeningTimes,
    ChargingLocation,
    FeeName,
    FeeType,
    Fee,
    Cdr,
)
from .gen.trip_service_pb2 import (
    WaypointType,
    Waypoint,
    Trip,
)
from .gen.salesforce_service_pb2 import (
    ReferralHistory,
    MemberAttributes,
    ReferralData,
)

__version__ = "1.2.0"

_LOGGER = logging.getLogger(__name__)


T = TypeVar('T')


async def _check_for_api_error(coroutine: Awaitable[T]) -> T:
    """Transform gRPC errors into APIErrors."""
    try:
        return await coroutine
    except grpc.aio.AioRpcError as exc:
        raise APIError(exc.code(), exc.details(), exc.debug_error_string()) from None


def enum_to_str(enum_type: EnumTypeWrapper, value: int) -> str:
    match (enum_type, value):
        case (
            (VSS.AlarmMode, AlarmMode.ALARM_MODE_UNKNOWN)
            | (VSS.AlarmStatus, AlarmStatus.ALARM_STATUS_UNKNOWN)
            | (VSS.Model, Model.MODEL_UNKNOWN)
            | (VSS.ModelVariant, ModelVariant.MODEL_VARIANT_UNKNOWN)
            | (VSS.PaintColor, PaintColor.PAINT_COLOR_UNKNOWN)
            | (VSS.Look, Look.LOOK_UNKNOWN)
            | (VSS.Wheels, Wheels.WHEELS_UNKNOWN)
            | (VSS.PowerState, PowerState.POWER_STATE_UNKNOWN)
            | (VSS.EnergyType, EnergyType.ENERGY_TYPE_UNKNOWN)
            | (VSS.DriveMode, DriveMode.DRIVE_MODE_UNKNOWN)
            | (VSS.GearPosition, GearPosition.GEAR_UNKNOWN)
        ):
            return "Unknown"

        case (VSS.AlarmMode, AlarmMode.ALARM_MODE_ON):
            return "On"
        case (VSS.AlarmMode, AlarmMode.ALARM_MODE_OFF):
            return "Off"
        case (VSS.AlarmMode, AlarmMode.ALARM_MODE_SILENT):
            return "Silent"

        case (VSS.AlarmStatus, AlarmStatus.ALARM_STATUS_DISARMED):
            return "Disarmed"
        case (VSS.AlarmStatus, AlarmStatus.ALARM_STATUS_ARMED):
            return "Armed"

        case (VSS.Model, Model.MODEL_AIR):
            return "Air"
        case (VSS.Model, Model.MODEL_GRAVITY):
            return "Gravity"

        case (VSS.ModelVariant, ModelVariant.MODEL_VARIANT_DREAM_EDITION):
            return "Dream Edition"
        case (VSS.ModelVariant, ModelVariant.MODEL_VARIANT_GRAND_TOURING):
            return "Grand Touring"
        case (VSS.ModelVariant, ModelVariant.MODEL_VARIANT_TOURING):
            return "Touring"
        case (VSS.ModelVariant, ModelVariant.MODEL_VARIANT_PURE):
            return "Pure"
        case (VSS.ModelVariant, ModelVariant.MODEL_VARIANT_SAPPHIRE):
            return "Sapphire"

        case (VSS.PaintColor, PaintColor.PAINT_COLOR_EUREKA_GOLD):
            return "Eureka Gold"
        case (VSS.PaintColor, PaintColor.PAINT_COLOR_STELLAR_WHITE):
            return "Stellar White"
        case (VSS.PaintColor, PaintColor.PAINT_COLOR_INFINITE_BLACK):
            return "Infinite Black"
        case (VSS.PaintColor, PaintColor.PAINT_COLOR_COSMOS_SILVER):
            return "Cosmos Silver"
        case (VSS.PaintColor, PaintColor.PAINT_COLOR_QUANTUM_GREY):
            return "Quantum Grey"
        case (VSS.PaintColor, PaintColor.PAINT_COLOR_ZENITH_RED):
            return "Zenith Red"
        case (VSS.PaintColor, PaintColor.PAINT_COLOR_FATHOM_BLUE):
            return "Fathom Blue"
        case (VSS.PaintColor, PaintColor.PAINT_COLOR_CUSTOM):
            return "Custom"
        case (VSS.PaintColor, PaintColor.PAINT_COLOR_SAPPHIRE_BLUE):
            return "Sapphire Blue"

        case (VSS.Look, Look.LOOK_PLATINUM):
            return "Platinum"
        case (VSS.Look, Look.LOOK_STEALTH):
            return "Stealth"
        case (VSS.Look, Look.LOOK_SAPPHIRE):
            return "Sapphire"
        case (VSS.Look, Look.LOOK_SURFRIDER):
            return "Surfrider"
        case (VSS.Look, Look.LOOK_BASE):
            return "Base"

        case (VSS.Wheels, Wheels.WHEELS_DREAM):
            return "Dream"
        case (VSS.Wheels, Wheels.WHEELS_BLADE):
            return "Blade"
        case (VSS.Wheels, Wheels.WHEELS_LITE):
            return "Lite"
        case (VSS.Wheels, Wheels.WHEELS_RANGE):
            return "Range"
        case (VSS.Wheels, Wheels.WHEELS_SPORT_STEALTH):
            return "Sport Stealth"
        case (VSS.Wheels, Wheels.WHEELS_BLADE_GRAPHITE):
            return "Blade Graphite"
        case (VSS.Wheels, Wheels.WHEELS_LITE_STEALTH):
            return "Lite Stealth"
        case (VSS.Wheels, Wheels.WHEELS_SPORT_LUSTER):
            return "Sport Luster"
        case (VSS.Wheels, Wheels.WHEELS_SAPPHIRE_PACKAGE):
            return "Sapphire Package"
        case (VSS.Wheels, Wheels.WHEELS_RANGE_STEALTH):
            return "Range Stealth"

        case (VSS.PowerState, PowerState.POWER_STATE_SLEEP):
            return "Sleep"
        case (VSS.PowerState, PowerState.POWER_STATE_WINK):
            return "Wink"
        case (VSS.PowerState, PowerState.POWER_STATE_ACCESSORY):
            return "Accessory"
        case (VSS.PowerState, PowerState.POWER_STATE_DRIVE):
            return "Drive"
        case (VSS.PowerState, PowerState.POWER_STATE_LIVE_CHARGE):
            return "Live/Charge"
        case (VSS.PowerState, PowerState.POWER_STATE_SLEEP_CHARGE):
            return "Sleep/Charge"
        case (VSS.PowerState, PowerState.POWER_STATE_LIVE_UPDATE):
            return "Live/Update"
        case (VSS.PowerState, PowerState.POWER_STATE_CLOUD_2):
            return "Cloud 2"
        case (VSS.PowerState, PowerState.POWER_STATE_MONITOR):
            return "Monitor"

        case (VSS.EnergyType, EnergyType.ENERGY_TYPE_AC):
            return "AC"
        case (VSS.EnergyType, EnergyType.ENERGY_TYPE_DC):
            return "DC"
        case (VSS.EnergyType, EnergyType.ENERGY_TYPE_V2V):
            return "V2V"

        case (VSS.DriveMode, DriveMode.DRIVE_MODE_COMFORT):
            return "Smooth"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_SWIFT):
            return "Swift"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_WINTER):
            return "Winter"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_VALET):
            return "Valet"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_SPORT_PLUS):
            return "Sprint"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_SERVICE):
            return "Service"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_LAUNCH):
            return "Launch"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_FACTORY):
            return "Factory"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_TRANSPORT):
            return "Transport"
        case (VSS.DriveMode, DriveMode.DRIVE_MODE_TOW):
            return "Tow"

        case (VSS.GearPosition, GearPosition.GEAR_PARK):
            return "Park"
        case (VSS.GearPosition, GearPosition.GEAR_REVERSE):
            return "Reverse"
        case (VSS.GearPosition, GearPosition.GEAR_NEUTRAL):
            return "Neutral"
        case (VSS.GearPosition, GearPosition.GEAR_DRIVE):
            return "Drive"

        case _:
            if value in enum_type.values():
                return enum_type.Name(value)
            else:
                return f"{enum_type.DESCRIPTOR.name} {value}"


class LucidAPIInterceptor(grpc.aio.UnaryUnaryClientInterceptor):
    """RPC interceptor adding token-based authentication."""

    # RPC call credentials (includes session token)
    _credentials: Optional[grpc.CallCredentials] = None

    def set_credentials(self, credentials: Optional[grpc.CallCredentials]) -> None:
        """Set (or clear) the gRPC credentials used for this channel."""
        self._credentials = credentials

    async def intercept_unary_unary(
        self,
        continuation: Callable[[ClientCallDetails, None], UnaryUnaryCall],
        client_call_details: ClientCallDetails,
        request: None,
    ) -> UnaryUnaryCall | None:
        """Intercept a unary-unary invocation asynchronously."""

        client_call_details = ClientCallDetails(
            client_call_details.method,
            client_call_details.timeout,
            client_call_details.metadata,
            self._credentials,
            client_call_details.wait_for_ready,
        )

        response = await continuation(client_call_details, request)

        return response


class LucidAPI:
    """A wrapper around the API used by the Lucid mobile apps"""

    # API RPC channel
    _channel: grpc.aio.Channel

    # Expiration time of our current authentication token, or None if we do not
    # have a valid one yet (call .login()).
    _token_expiry_time: Optional[datetime]

    # Refresh token for our session
    _refresh_token: Optional[str]

    # Gigya JWT token used for some other authentication?
    _gigya_jwt: Optional[str]

    # User profile data from most recent login request, or None if not logged
    # in yet.
    _user_profile: Optional[UserProfile]

    # List of user vehicles
    _vehicles: list[Vehicle]

    # RPC call interceptor
    _interceptor: LucidAPIInterceptor

    # Service stubs generated from the gRPC Service definitions
    _login_service: login_session_pb2_grpc.LoginSessionStub
    _user_profile_service: user_profile_service_pb2_grpc.UserProfileServiceStub
    _trip_service: trip_service_pb2_grpc.TripServiceStub
    _vehicle_service: vehicle_state_service_pb2_grpc.VehicleStateServiceStub
    _charging_service: charging_service_pb2_grpc.ChargingServiceStub
    _salesforce_service: salesforce_service_pb2_grpc.SalesforceServiceStub

    # Automatically wake sleeping vehicle along with commands?
    _auto_wake: bool

    def __init__(self, auto_wake: bool = False, region: Region = Region.US) -> None:
        """Initialize the API client

        :param auto_wake: Automatically send a wake request with commands that
        require it if the vehicle is sleeping.
        """

        # We start with a channel secured with "SSL credentials," i.e. normal
        # SSL/TLS certificate verification. Once we log in we can upgrade to
        # "token" credentials to keep an authenticated session.
        ssl_creds = grpc.ssl_channel_credentials()
        self._interceptor = LucidAPIInterceptor()
        # Typing ignored due to "_PartialStubMustCastOrIgnore" insanity in
        # grpc-stubs package.
        self._channel = grpc.aio.secure_channel(
            MOBILE_API_REGIONS[region],
            credentials=ssl_creds,
            interceptors=[self._interceptor],  # type: ignore
        )
        self._login_service = login_session_pb2_grpc.LoginSessionStub(self._channel)
        self._user_profile_service = (
            user_profile_service_pb2_grpc.UserProfileServiceStub(self._channel)
        )
        self._trip_service = trip_service_pb2_grpc.TripServiceStub(self._channel)
        self._vehicle_service = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(
            self._channel
        )
        self._charging_service = charging_service_pb2_grpc.ChargingServiceStub(
            self._channel
        )
        self._salesforce_service = salesforce_service_pb2_grpc.SalesforceServiceStub(
            self._channel
        )
        self._refresh_token = None
        self._gigya_jwt = None
        self._token_expiry_time = None
        self._user_profile = None
        self._vehicles = []
        self._auto_wake = auto_wake

    async def __aenter__(self) -> "LucidAPI":
        await self._channel.__aenter__()
        return self

    async def __aexit__(self, *exc: Any) -> None:
        await self._channel.__aexit__(*exc)

    def _save_session(self, sess: login_session_pb2.SessionInfo) -> None:
        self._token_expiry_time = datetime.fromtimestamp(
            sess.expiry_time_sec, timezone.utc
        )
        self._refresh_token = sess.refresh_token
        self._gigya_jwt = sess.gigya_jwt

        _LOGGER.debug(
            "API authentication succeeded. Token expires at %s (%s from now)",
            self._token_expiry_time,
            self._token_expiry_time - datetime.now(timezone.utc),
        )

        creds = grpc.access_token_call_credentials(sess.id_token)
        self._interceptor.set_credentials(creds)

    @property
    def session_time_remaining(self) -> timedelta:
        """Time remaining before our session would expire without renewal.

        Returns timedelta(0) if not logged in yet.
        """
        if self._token_expiry_time is None:
            return timedelta(0)
        now = datetime.now(timezone.utc)
        if self._token_expiry_time > now:
            return self._token_expiry_time - now
        return timedelta(0)

    async def login(self, username: str, password: str) -> None:
        """Authenticate to the API using your Lucid account credentials"""

        # Lucid wants some sort of unique device ID. UUID provides a
        # cross-platform way of getting a relatively unique device ID.
        device_id = f'{uuid.getnode():x}'

        # The API responds with helpful schema validation messages if these
        # fields are wrong. If the API requirements change in the future, try
        # just looking at the message it returns to see if it says something
        # like "missing required field XYZ," or "invalid value for enumerator
        # type XYZ."
        request = login_session_pb2.LoginRequest(
            username=username,
            password=password,
            notification_channel_type=NotificationChannelType.NOTIFICATION_CHANNEL_ONE,
            notification_device_token=device_id,
            os=login_session_pb2.Os.OS_IOS,
            locale='en_US',
            client_name=f'python-lucidmotors/{__version__}',
            device_id=device_id,
        )

        # The login endpoint gives us a bearer token we can use in future
        # requests. It comes with an expiration time, but there is a renewal
        # endpoint to keep the session alive. We don't need to log in again
        # unless we miss a renewal window.
        reply = await _check_for_api_error(self._login_service.Login(request))

        self._save_session(reply.session_info)

        self._user_profile = reply.user_profile
        self._vehicles = reply.user_vehicle_data

    async def set_profile_photo(self, photo_bytes: bytes) -> Optional[str]:
        """Set the logged-in user's profile photo.

        Returns the uploaded photo URL on success."""

        request = user_profile_service_pb2.UploadUserProfilePhotoRequest(
            photo_bytes=b64encode(photo_bytes).decode('utf-8'),
        )

        reply = await _check_for_api_error(
            self._user_profile_service.UploadUserProfilePhoto(request)
        )

        return reply.photo_url

    async def get_referral_history(self) -> ReferralData:
        """Fetch the logged-in user's referral history."""

        if self._gigya_jwt is None:
            raise APIValueError('API did not provide a Gigya JWT token')
        if self._user_profile is None:
            raise APIValueError('User profile is missing')

        request = salesforce_service_pb2.ReferralHistoryRequest(
            email=self._user_profile.email,
        )

        reply = await _check_for_api_error(
            self._salesforce_service.ReferralHistory(
                request,
                metadata=[('gigyajwt', self._gigya_jwt)],
            )
        )

        if reply.statusCode != 200:
            raise APIValueError(f'Fetching referral history failed: {reply.message}')

        return reply.data

    async def authentication_refresh(self) -> None:
        """Get a fresh new token by using the refresh token."""
        request = login_session_pb2.GetNewJWTTokenRequest(
            refresh_token=self._refresh_token
        )
        reply = await _check_for_api_error(self._login_service.GetNewJWTToken(request))

        self._save_session(reply.session_info)
        assert self._token_expiry_time is not None  # always set by _save_session

        _LOGGER.debug(
            "Session refresh succeeded. New token expires at %s (%s from now)",
            self._token_expiry_time,
            self._token_expiry_time - datetime.now(timezone.utc),
        )

    async def close(self) -> None:
        """
        Close the underlying channel. Must be called to free resources if
        this object is not used as a context manager.
        """
        await self._channel.close(None)

    @property
    def user(self) -> Optional[UserProfile]:
        """Return the logged-in user's profile information"""
        return self._user_profile

    @property
    def vehicles(self) -> list[Vehicle]:
        """
        Return a cached list of the logged-in user's Vehicles.
        Note: To get fresh vehicle information, call .fetch_vehicles()
        """
        return self._vehicles

    async def fetch_vehicles(self) -> list[Vehicle]:
        """
        Refresh the list (and status) of vehicles from the API.
        """

        request = login_session_pb2.GetUserVehiclesRequest()
        reply = await _check_for_api_error(self._login_service.GetUserVehicles(request))
        self._vehicles = reply.user_vehicle_data

        return self._vehicles

    def vehicle_is_awake(self, vehicle: Vehicle) -> bool:
        """
        Returns `True` if `vehicle` was awake (i.e. responding to commands) as
        of the last status update.
        """

        # There are a lot of power states. Do any others mean it's not listening?
        return vehicle.state.power != PowerState.POWER_STATE_SLEEP

    async def wakeup_vehicle(self, vehicle: Vehicle) -> None:
        """
        Wake up a specific vehicle.
        """

        request = vehicle_state_service_pb2.WakeupVehicleRequest(
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.WakeupVehicle(request))

    async def honk_horn(self, vehicle: Vehicle) -> None:
        """
        Honk the horn of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.HonkHornRequest(
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.HonkHorn(request))

    async def lights_control(self, vehicle: Vehicle, action: LightAction) -> None:
        """
        Control the lights of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.LightsControlRequest(
            vehicle_id=vehicle.vehicle_id,
            action=action,
        )
        await _check_for_api_error(self._vehicle_service.LightsControl(request))

    async def lights_on(self, vehicle: Vehicle) -> None:
        """
        Turn on the lights of a specific vehicle.
        """

        await self.lights_control(vehicle, LightAction.LIGHT_ACTION_ON)

    async def lights_off(self, vehicle: Vehicle) -> None:
        """
        Turn off the lights of a specific vehicle.
        """

        await self.lights_control(vehicle, LightAction.LIGHT_ACTION_OFF)

    async def lights_flash(self, vehicle: Vehicle) -> None:
        """
        Flash the lights of a specific vehicle.
        """

        await self.lights_control(vehicle, LightAction.LIGHT_ACTION_FLASH)

    async def charge_port_control(self, vehicle: Vehicle, state: DoorState) -> None:
        """
        Control the charge port door of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.ControlChargePortRequest(
            closure_state=state,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.ControlChargePort(request))

    async def charge_port_open(self, vehicle: Vehicle) -> None:
        """
        Open the charge port door of a specific vehicle.
        """

        await self.charge_port_control(vehicle, DoorState.DOOR_STATE_OPEN)

    async def charge_port_close(self, vehicle: Vehicle) -> None:
        """
        Close the charge port door of a specific vehicle.
        """

        await self.charge_port_control(vehicle, DoorState.DOOR_STATE_CLOSED)

    async def door_locks_control(
        self, vehicle: Vehicle, state: LockState, doors: list[int] = list(range(1, 5))
    ) -> None:
        """
        Control the doors of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.DoorLocksControlRequest(
            door_location=doors,
            lock_state=state,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.DoorLocksControl(request))

    async def doors_unlock(
        self, vehicle: Vehicle, doors: list[int] = list(range(1, 5))
    ) -> None:
        """
        Open the doors of a specific vehicle.
        """

        await self.door_locks_control(vehicle, LockState.LOCK_STATE_UNLOCKED, doors)

    async def doors_lock(
        self, vehicle: Vehicle, doors: list[int] = list(range(1, 5))
    ) -> None:
        """
        Close the doors of a specific vehicle.
        """

        await self.door_locks_control(vehicle, LockState.LOCK_STATE_LOCKED, doors)

    async def frunk_control(self, vehicle: Vehicle, state: DoorState) -> None:
        """
        Control the frunk door of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.FrontCargoControlRequest(
            closure_state=state,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.FrontCargoControl(request))

    async def frunk_open(self, vehicle: Vehicle) -> None:
        """
        Open the frunk door of a specific vehicle.
        """

        await self.frunk_control(vehicle, DoorState.DOOR_STATE_OPEN)

    async def frunk_close(self, vehicle: Vehicle) -> None:
        """
        Close the frunk door of a specific vehicle.
        """

        await self.frunk_control(vehicle, DoorState.DOOR_STATE_CLOSED)

    async def trunk_control(self, vehicle: Vehicle, state: DoorState) -> None:
        """
        Control the trunk door of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.RearCargoControlRequest(
            closure_state=state,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.RearCargoControl(request))

    async def trunk_open(self, vehicle: Vehicle) -> None:
        """
        Open the trunk door of a specific vehicle.
        """

        await self.trunk_control(vehicle, DoorState.DOOR_STATE_OPEN)

    async def trunk_close(self, vehicle: Vehicle) -> None:
        """
        Close the trunk door of a specific vehicle.
        """

        await self.trunk_control(vehicle, DoorState.DOOR_STATE_CLOSED)

    async def defrost_control(self, vehicle: Vehicle, action: DefrostState) -> None:
        """
        Control the defrost mode of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.HvacDefrostControlRequest(
            hvac_defrost=action,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.HvacDefrostControl(request))

    async def defrost_on(self, vehicle: Vehicle) -> None:
        """
        Turn on the defrost mode of a specific vehicle.
        """

        await self.defrost_control(vehicle, DefrostState.DEFROST_ON)

    async def defrost_off(self, vehicle: Vehicle) -> None:
        """
        Turn off the defrost mode of a specific vehicle.
        """

        await self.defrost_control(vehicle, DefrostState.DEFROST_OFF)

    async def set_cabin_temperature(
        self, vehicle: Vehicle, temperature: Optional[float]
    ) -> None:
        """
        Set cabin temperature (in celcius) for preconditioning.
        Disables preconditioning if temperature is None.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        if temperature is None:
            power = HvacPower.HVAC_OFF
            temperature = 0.0
        else:
            power = HvacPower.HVAC_PRECONDITION

        request = vehicle_state_service_pb2.SetCabinTemperatureRequest(
            temperature=temperature,
            state=power,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.SetCabinTemperature(request))

    async def battery_precon_control(
        self, vehicle: Vehicle, action: BatteryPreconStatus
    ) -> None:
        """
        Control battery preconditioning for a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.SetBatteryPreconRequest(
            vehicle_id=vehicle.vehicle_id,
            status=action,
        )
        await _check_for_api_error(self._vehicle_service.SetBatteryPrecon(request))

    async def battery_precon_on(self, vehicle: Vehicle) -> None:
        """
        Turn on battery preconditioning for a specific vehicle.
        """

        await self.battery_precon_control(
            vehicle, BatteryPreconStatus.BATTERY_PRECON_ON
        )

    async def battery_precon_off(self, vehicle: Vehicle) -> None:
        """
        Turn off battery preconditioning for a specific vehicle.
        """

        await self.battery_precon_control(
            vehicle, BatteryPreconStatus.BATTERY_PRECON_OFF
        )

    async def get_update_release_notes(self, version: str) -> GetDocumentInfoResponse:
        """
        Fetch release notes and description given a software version, e.g.
        '2.1.47'.
        """

        request = vehicle_state_service_pb2.GetDocumentInfoRequest(
            version=version,
            document_type=DocumentType.DOCUMENT_TYPE_RELEASE_NOTES_POST,
        )

        return await _check_for_api_error(
            self._vehicle_service.GetDocumentInfo(request)
        )

    async def get_owners_manual(self, version: str) -> str:
        """
        Fetch owner's manual URL given a software version, e.g. '2.1.47'.
        """

        request = vehicle_state_service_pb2.GetDocumentInfoRequest(
            version=version,
            document_type=DocumentType.DOCUMENT_TYPE_OWNERS_MANUAL,
        )

        result = await _check_for_api_error(
            self._vehicle_service.GetDocumentInfo(request)
        )
        return result.url

    async def apply_update(self, vehicle: Vehicle) -> None:
        """
        Apply an available software update. Target version cannot be chosen, it
        will always be the latest update available to the car.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.ApplySoftwareUpdateRequest(
            vehicle_id=vehicle.vehicle_id,
        )

        await _check_for_api_error(self._vehicle_service.ApplySoftwareUpdate(request))

    async def set_charge_limit(self, vehicle: Vehicle, value: int) -> None:
        """
        Set the charge limit for a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.SetChargeLimitRequest(
            limit_percent=value,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.SetChargeLimit(request))

    async def charging_control(self, vehicle: Vehicle, action: ChargeAction) -> None:
        """
        Enable or disable charging for a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.ChargeControlRequest(
            action=action,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.ChargeControl(request))

    async def start_charging(self, vehicle: Vehicle) -> None:
        """
        Start charging a specific vehicle.
        """

        await self.charging_control(vehicle, ChargeAction.CHARGE_ACTION_START)

    async def stop_charging(self, vehicle: Vehicle) -> None:
        """
        Stop charging a specific vehicle.
        """

        await self.charging_control(vehicle, ChargeAction.CHARGE_ACTION_STOP)

    async def alarm_control(self, vehicle: Vehicle, mode: AlarmMode) -> None:
        """
        Control the alarm of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.SecurityAlarmControlRequest(
            mode=mode,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.SecurityAlarmControl(request))

    async def all_windows_control(
        self, vehicle: Vehicle, action: WindowSwitchState
    ) -> None:
        """
        Control all of the windows of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.AllWindowControlRequest(
            state=action,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.AllWindowControl(request))

    async def close_all_windows(self, vehicle: Vehicle) -> None:
        """
        Close all of the windows of a specific vehicle.
        """

        await self.all_windows_control(
            vehicle, WindowSwitchState.WINDOW_SWITCH_STATE_AUTO_UP_ALL
        )

    async def open_all_windows(self, vehicle: Vehicle) -> None:
        """
        Open all of the windows of a specific vehicle slightly.
        Repeated calls roll down the windows further.
        """

        await self.all_windows_control(
            vehicle, WindowSwitchState.WINDOW_SWITCH_STATE_VENT_ALL
        )

    async def seat_climate_control(
        self, vehicle: Vehicle, **kwargs: SeatClimateMode
    ) -> None:
        """
        Control individual seat heating or venting of a specific vehicle.

        Possible zones to change are:

            - driver_heat_backrest_zone1
            - driver_heat_backrest_zone3
            - driver_heat_cushion_zone2
            - driver_heat_cushion_zone4
            - driver_vent_backrest
            - driver_vent_cushion
            - front_passenger_heat_backrest_zone1
            - front_passenger_heat_backrest_zone3
            - front_passenger_heat_cushion_zone2
            - front_passenger_heat_cushion_zone4
            - front_passenger_vent_backrest
            - front_passenger_vent_cushion
            - rear_passenger_heat_left
            - rear_passenger_heat_center
            - rear_passenger_heat_right

        Omitted zones will be unchanged.

        At least in the Air, the pairs of e.g. "zone2" and "zone4" are bound
        together and cannot be changed individually. Maybe those are for future
        vehicles.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.SeatClimateControlRequest(
            vehicle_id=vehicle.vehicle_id, **kwargs
        )
        await _check_for_api_error(self._vehicle_service.SeatClimateControl(request))

    async def max_ac_control(self, vehicle: Vehicle, action: MaxACState) -> None:
        """
        Control the Max A/C setting of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.SetMaxACRequest(
            state=action,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.SetMaxAC(request))

    async def max_ac_on(self, vehicle: Vehicle) -> None:
        """
        Turn on the Max A/C setting of a specific vehicle.
        """

        await self.max_ac_control(vehicle, MaxACState.MAX_AC_STATE_ON)

    async def max_ac_off(self, vehicle: Vehicle) -> None:
        """
        Turn off the Max A/C setting of a specific vehicle.
        """

        await self.max_ac_control(vehicle, MaxACState.MAX_AC_STATE_OFF)

    async def steering_wheel_heater_control(
        self, vehicle: Vehicle, level: SteeringWheelHeaterLevel
    ) -> None:
        """
        Control the steering wheel heater of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.SteeringWheelHeaterRequest(
            level=level,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(self._vehicle_service.SteeringWheelHeater(request))

    async def creature_comfort_control(
        self, vehicle: Vehicle, mode: CreatureComfortMode
    ) -> None:
        """
        Control the creature comfort mode of a specific vehicle.
        """

        if self._auto_wake and not self.vehicle_is_awake(vehicle):
            await self.wakeup_vehicle(vehicle)

        request = vehicle_state_service_pb2.SetCreatureComfortModeRequest(
            mode=mode,
            vehicle_id=vehicle.vehicle_id,
        )
        await _check_for_api_error(
            self._vehicle_service.SetCreatureComfortMode(request)
        )
