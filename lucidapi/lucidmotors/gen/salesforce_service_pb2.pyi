from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ReferralHistory(_message.Message):
    __slots__ = ("refereeEmail", "referralStatus", "referralPoints", "refereeFirstName", "refereeLastName", "referralDate", "trim")
    REFEREEEMAIL_FIELD_NUMBER: _ClassVar[int]
    REFERRALSTATUS_FIELD_NUMBER: _ClassVar[int]
    REFERRALPOINTS_FIELD_NUMBER: _ClassVar[int]
    REFEREEFIRSTNAME_FIELD_NUMBER: _ClassVar[int]
    REFEREELASTNAME_FIELD_NUMBER: _ClassVar[int]
    REFERRALDATE_FIELD_NUMBER: _ClassVar[int]
    TRIM_FIELD_NUMBER: _ClassVar[int]
    refereeEmail: str
    referralStatus: str
    referralPoints: float
    refereeFirstName: str
    refereeLastName: str
    referralDate: str
    trim: str
    def __init__(self, refereeEmail: _Optional[str] = ..., referralStatus: _Optional[str] = ..., referralPoints: _Optional[float] = ..., refereeFirstName: _Optional[str] = ..., refereeLastName: _Optional[str] = ..., referralDate: _Optional[str] = ..., trim: _Optional[str] = ...) -> None: ...

class MemberAttributes(_message.Message):
    __slots__ = ("value", "name")
    VALUE_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    value: str
    name: str
    def __init__(self, value: _Optional[str] = ..., name: _Optional[str] = ...) -> None: ...

class ReferralData(_message.Message):
    __slots__ = ("email", "status", "referralCode", "pointsBalance", "totalReferralCount", "referrals", "member_attributes", "validState", "validAge", "country")
    EMAIL_FIELD_NUMBER: _ClassVar[int]
    STATUS_FIELD_NUMBER: _ClassVar[int]
    REFERRALCODE_FIELD_NUMBER: _ClassVar[int]
    POINTSBALANCE_FIELD_NUMBER: _ClassVar[int]
    TOTALREFERRALCOUNT_FIELD_NUMBER: _ClassVar[int]
    REFERRALS_FIELD_NUMBER: _ClassVar[int]
    MEMBER_ATTRIBUTES_FIELD_NUMBER: _ClassVar[int]
    VALIDSTATE_FIELD_NUMBER: _ClassVar[int]
    VALIDAGE_FIELD_NUMBER: _ClassVar[int]
    COUNTRY_FIELD_NUMBER: _ClassVar[int]
    email: str
    status: str
    referralCode: str
    pointsBalance: int
    totalReferralCount: int
    referrals: _containers.RepeatedCompositeFieldContainer[ReferralHistory]
    member_attributes: _containers.RepeatedCompositeFieldContainer[MemberAttributes]
    validState: bool
    validAge: bool
    country: str
    def __init__(self, email: _Optional[str] = ..., status: _Optional[str] = ..., referralCode: _Optional[str] = ..., pointsBalance: _Optional[int] = ..., totalReferralCount: _Optional[int] = ..., referrals: _Optional[_Iterable[_Union[ReferralHistory, _Mapping]]] = ..., member_attributes: _Optional[_Iterable[_Union[MemberAttributes, _Mapping]]] = ..., validState: bool = ..., validAge: bool = ..., country: _Optional[str] = ...) -> None: ...

class ReferralHistoryRequest(_message.Message):
    __slots__ = ("email",)
    EMAIL_FIELD_NUMBER: _ClassVar[int]
    email: str
    def __init__(self, email: _Optional[str] = ...) -> None: ...

class ReferralHistoryResponse(_message.Message):
    __slots__ = ("statusCode", "message", "data")
    STATUSCODE_FIELD_NUMBER: _ClassVar[int]
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    DATA_FIELD_NUMBER: _ClassVar[int]
    statusCode: int
    message: str
    data: ReferralData
    def __init__(self, statusCode: _Optional[int] = ..., message: _Optional[str] = ..., data: _Optional[_Union[ReferralData, _Mapping]] = ...) -> None: ...

class ServiceAppointment(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class GetServiceAppointmentsV1Request(_message.Message):
    __slots__ = ("customer_id",)
    CUSTOMER_ID_FIELD_NUMBER: _ClassVar[int]
    customer_id: str
    def __init__(self, customer_id: _Optional[str] = ...) -> None: ...

class GetServiceAppointmentsV1Response(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class GetServiceAppointmentSlotsRequest(_message.Message):
    __slots__ = ("location_id",)
    LOCATION_ID_FIELD_NUMBER: _ClassVar[int]
    location_id: str
    def __init__(self, location_id: _Optional[str] = ...) -> None: ...

class GetServiceAppointmentSlotsResponse(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...
