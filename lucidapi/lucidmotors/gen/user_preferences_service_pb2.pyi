from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class Empty(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class UserPreferences(_message.Message):
    __slots__ = ("first_name", "last_name", "photo_url", "updated_ns", "email")
    FIRST_NAME_FIELD_NUMBER: _ClassVar[int]
    LAST_NAME_FIELD_NUMBER: _ClassVar[int]
    PHOTO_URL_FIELD_NUMBER: _ClassVar[int]
    UPDATED_NS_FIELD_NUMBER: _ClassVar[int]
    EMAIL_FIELD_NUMBER: _ClassVar[int]
    first_name: str
    last_name: str
    photo_url: str
    updated_ns: int
    email: str
    def __init__(self, first_name: _Optional[str] = ..., last_name: _Optional[str] = ..., photo_url: _Optional[str] = ..., updated_ns: _Optional[int] = ..., email: _Optional[str] = ...) -> None: ...

class GetUserPreferencesResponse(_message.Message):
    __slots__ = ("preferences", "commit_ns")
    PREFERENCES_FIELD_NUMBER: _ClassVar[int]
    COMMIT_NS_FIELD_NUMBER: _ClassVar[int]
    preferences: UserPreferences
    commit_ns: int
    def __init__(self, preferences: _Optional[_Union[UserPreferences, _Mapping]] = ..., commit_ns: _Optional[int] = ...) -> None: ...

class CreateUserPreferencesRequest(_message.Message):
    __slots__ = ("preferences",)
    PREFERENCES_FIELD_NUMBER: _ClassVar[int]
    preferences: UserPreferences
    def __init__(self, preferences: _Optional[_Union[UserPreferences, _Mapping]] = ...) -> None: ...

class GetUserPreferencesCommitIDResponse(_message.Message):
    __slots__ = ("commit_ns",)
    COMMIT_NS_FIELD_NUMBER: _ClassVar[int]
    commit_ns: int
    def __init__(self, commit_ns: _Optional[int] = ...) -> None: ...
