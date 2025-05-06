from pytest_cases import parametrize, parametrize_with_cases
from typing import Any
from pathlib import Path

import json

from lucidmotors import LoginResponse

TEST_DIR = Path(__file__).parent
TEST_DATA_DIR = TEST_DIR / 'data'


class LoginResponseCasesValid:
    @parametrize(filename=["l2_charging", "idle", "dcfc"])
    def case_from_file(self, filename: str) -> Any:
        subdir = TEST_DATA_DIR / 'login_response'
        with open(subdir / f"{filename}.json", 'r') as fi:
            return json.load(fi)


@parametrize_with_cases("raw", cases=LoginResponseCasesValid)
def test_valid_login_response(raw):
    resp = LoginResponse(**raw)
    assert isinstance(resp, LoginResponse)
