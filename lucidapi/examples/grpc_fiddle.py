from typing import Any
from google.protobuf.unknown_fields import UnknownFieldSet # type: ignore

import uuid
import grpc
import time
import logging
import getpass
import google._upb # type: ignore
import google.protobuf

import sys
sys.path.insert(0, '.')

from lucidmotors.gen import login_session_pb2
from lucidmotors.gen import login_session_pb2_grpc

from lucidmotors.gen import user_profile_service_pb2
from lucidmotors.gen import user_profile_service_pb2_grpc

from lucidmotors.gen import trip_service_pb2
from lucidmotors.gen import trip_service_pb2_grpc

from lucidmotors.gen import vehicle_state_service_pb2
from lucidmotors.gen import vehicle_state_service_pb2_grpc

from lucidmotors.gen import salesforce_service_pb2
from lucidmotors.gen import salesforce_service_pb2_grpc

from lucidmotors.gen import charging_service_pb2
from lucidmotors.gen import charging_service_pb2_grpc

wire_types = {
    0: 'varint',
    1: 'fixed-64bit',
    2: 'length-delimited',
    3: 'group-start',
    4: 'group-end',
    5: 'fixed-32bit',
}


def message_dump_recursive(message: Any, depth: int = 0):
    if isinstance(message, (google._upb._message.RepeatedScalarContainer, google._upb._message.RepeatedCompositeContainer)):  # type: ignore
        for elem in message:
            message_dump_recursive(elem, depth=depth)
        return

    if not isinstance(message, google.protobuf.message.Message):
        return

    indent = ' ' * depth
    print(f'{indent}{type(message)}:')

    depth += 1
    indent = ' ' * depth

    for field in UnknownFieldSet(message):
        wire_type = wire_types[field.wire_type]
        print(
            f'{indent}Unknown field {field.field_number} wire type {wire_type}: {field.data!r}'
        )

    for descriptor, field in message.ListFields():
        if isinstance(field, (google.protobuf.message.Message, google._upb._message.RepeatedScalarContainer, google._upb._message.RepeatedCompositeContainer)):  # type: ignore
            field_desc_short = ''
        elif descriptor.enum_type is not None:
            # TODO: Handle a list of enum, like LoginResponse.subscriptions
            enum = descriptor.enum_type
            if field in enum.values_by_number:
                name = enum.values_by_number[field].name
                field_desc_short = f'{name} ({field})'
            else:
                field_desc_short = f'UNKNOWN ENUMERATOR: {field}'
        else:
            field_desc_short = str(field)
        print(
            f'{indent}Field {descriptor.number}, {descriptor.name}: {field_desc_short}'
        )
        message_dump_recursive(field, depth=depth)


def main():
    username = input("Username: ")
    password = getpass.getpass()

    print('Authenticating')

    with grpc.secure_channel(
        "mobile.deneb.prod.infotainment.pdx.atieva.com", grpc.ssl_channel_credentials()
    ) as channel:
        stub = login_session_pb2_grpc.LoginSessionStub(channel)
        device_id = f'{uuid.getnode():x}'
        req = login_session_pb2.LoginRequest(
            username=username,
            password=password,
            notification_channel_type=login_session_pb2.NotificationChannelType.NOTIFICATION_CHANNEL_ONE,
            notification_device_token=device_id,
            os=login_session_pb2.Os.OS_IOS,
            locale='en_US',
            client_name='python-lucidmotors',
            device_id=device_id,
        )
        response = stub.Login(req)
        message_dump_recursive(response)

        id_token = response.session_info.id_token
        refresh_token = response.session_info.refresh_token
        gigya_jwt = response.session_info.gigya_jwt

        vehicle_id = response.user_vehicle_data[0].vehicle_id
        ema_id = response.user_vehicle_data[0].config.ema_id

    time.sleep(1)
    print('Establishing token-based secure channel')

    token_creds = grpc.access_token_call_credentials(id_token)
    creds = grpc.composite_channel_credentials(
        grpc.ssl_channel_credentials(), token_creds
    )

    with grpc.secure_channel(
        "mobile.deneb.prod.infotainment.pdx.atieva.com", creds
    ) as channel:
        pass

        # stub = trip_service_pb2_grpc.TripServiceStub(channel)
        # trip = trip_service_pb2.Trip(
        #     name="22 6th St NW, Hillsboro, ND 58045",
        #     route=[
        #         trip_service_pb2.Waypoint(
        #             latitude=37.634690,
        #             longitude=-77.456089,
        #             name="22 6th st NW, Hillsboro, ND 58045",
        #         )
        #     ],
        # )
        # req = trip_service_pb2.ShareTripRequest(
        #     trip=trip,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.ShareTrip(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.GetDocumentInfoRequest(
        #     version="2.1.42",
        #     document_type=vehicle_state_service_pb2.DocumentType.DOCUMENT_TYPE_RELEASE_NOTES_POST,
        # )
        # response = stub.GetDocumentInfo(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.DoorLocksControlRequest(
        #     door_location=[1],
        #     lock_state=vehicle_state_service_pb2.LockState.LOCK_STATE_LOCKED,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.DoorLocksControl(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.SetCabinTemperatureRequest(
        #     temperature=0,
        #     state=vehicle_state_service_pb2.HvacPower.HVAC_OFF,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.SetCabinTemperature(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.WakeupVehicleRequest(
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.WakeupVehicle(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.GetVehicleStateRequest(
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.GetVehicleState(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.SetChargeLimitRequest(
        #     limit_percent=80,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.SetChargeLimit(req)
        # message_dump_recursive(response)

        # stub = salesforce_service_pb2_grpc.SalesforceServiceStub(channel)
        # req = salesforce_service_pb2.GetSalesForceServiceAppointmentsRequest(
        # )
        # response = stub.GetSalesForceServiceAppointments(req)
        # message_dump_recursive(response)

        # stub = salesforce_service_pb2_grpc.SalesforceServiceStub(channel)
        # req = salesforce_service_pb2.ReferralHistoryApiRequest(
        #     email=username,
        # )
        # response = stub.ReferralHistory(req, metadata = [('gigyajwt', gigya_jwt)])
        # message_dump_recursive(response)

        # stub = charging_service_pb2_grpc.ChargingServiceStub(channel)
        # req = charging_service_pb2.GetCdrsRequest(
        #     ema_id=ema_id,
        #     limit=999,
        # )
        # response = stub.GetCdrs(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.SecurityAlarmControlRequest(
        #     status=vehicle_state_service_pb2.AlarmStatus.ALARM_STATUS_ARMED,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.SecurityAlarmControl(req)
        # message_dump_recursive(response)

        # stub = login_session_pb2_grpc.LoginSessionStub(channel)
        # req = login_session_pb2.SetNickNameRequest(
        #     vehicle_id=vehicle_id,
        #     nickname='iCloud',
        # )
        # response = stub.SetNickName(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.CancelScheduledUpdateRequest(
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.CancelScheduledUpdate(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.ChargeControlRequest(
        #     action=vehicle_state_service_pb2.ChargeAction.CHARGE_ACTION_UNKNOWN,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.ChargeControl(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.ControlChargePortRequest(
        #     closure_state=vehicle_state_service_pb2.DoorState.DOOR_STATE_OPEN,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.ControlChargePort(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.HonkHornRequest(
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.HonkHorn(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.HvacDefrostControlRequest(
        #     vehicle_id=vehicle_id,
        #     hvac_defrost=vehicle_state_service_pb2.DefrostState.DEFROST_OFF,
        # )
        # response = stub.HvacDefrostControl(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.LightsControlRequest(
        #     action=vehicle_state_service_pb2.LightAction.LIGHT_ACTION_OFF,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.LightsControl(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.FrontCargoControlRequest(
        #     closure_state=vehicle_state_service_pb2.DoorState.DOOR_STATE_CLOSED,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.FrontCargoControl(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.RearCargoControlRequest(
        #     closure_state=vehicle_state_service_pb2.DoorState.DOOR_STATE_CLOSED,
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.RearCargoControl(req)
        # message_dump_recursive(response)

        # stub = vehicle_state_service_pb2_grpc.VehicleStateServiceStub(channel)
        # req = vehicle_state_service_pb2.ApplySoftwareUpdateRequest(
        #     vehicle_id=vehicle_id,
        # )
        # response = stub.ApplySoftwareUpdate(req)
        # message_dump_recursive(response)

        # stub = login_session_pb2_grpc.LoginSessionStub(channel)
        # req = login_session_pb2.GetUserVehiclesRequest()
        # response = stub.GetUserVehicles(req)
        # message_dump_recursive(response)


if __name__ == "__main__":
    logging.basicConfig(level=logging.DEBUG)
    main()
