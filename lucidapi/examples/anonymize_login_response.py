import logging
import pathlib
import asyncio
import getpass
import sys
import json
import jwt
import time

from hashlib import sha1

# Allow running straight out of the repo
sys.path.insert(0, str(pathlib.Path(__file__).parent.parent.absolute()))

from lucidmotors import LucidAPI  # noqa: E402

logging.basicConfig(level=logging.DEBUG)


id_data = {
    "iat": int(time.time()) + 300,
    "iss": "https://fidm.gigya.com/jwt/this_is_an_api_key/",
    "sub": "0123456789abcdef0123456789abcdef",
    "vid": "",
    "email": "",
    "email_verified": False,
}

jwt_data = {
    "iat": int(time.time()) + 300,
    "exp": int(time.time()) + 600,
    "iss": "https://fidm.gigya.com/jwt/this_is_an_api_key/",
    "apiKey": "this_is_an_api_key",
    "sub": "0123456789abcdef0123456789abcdef",
    "vid": "",
    "email": "",
    "email_verified": False,
}

print("Please enter your Lucid account credentials.")

username = input("Username: ")
password = getpass.getpass()


async def main():
    async with LucidAPI() as lucid:
        raw = await lucid._login_request(username, password)

    raw['uid'] = '0123456789abcdef01234567'
    raw['sessionInfo']['idToken'] = jwt.encode(id_data, "secret", algorithm="HS256")
    raw['sessionInfo']['jwtToken'] = jwt.encode(jwt_data, "secret", algorithm="HS256")
    raw['sessionInfo']['expiryTimeSec'] = time.time() + 300

    raw['userProfile']['email'] = 'mail@example.com'
    raw['userProfile']['username'] = 'mail@example.com'
    raw['userProfile']['firstName'] = 'Lucid'
    raw['userProfile']['lastName'] = 'Owner'
    raw['userProfile']['emaId'] = ''

    for i in range(len(raw['userVehicleData'])):
        raw['userVehicleData'][i]['vehicleId'] = '0123456789abcdef01234567'
        raw['userVehicleData'][i]['vehicleConfig']['vin'] = '0123456789ABCDEFG'
        raw['userVehicleData'][i]['vehicleConfig']['emaId'] = 'USLCD123456789'
        raw['userVehicleData'][i]['vehicleConfig']['chargingAccounts'][0][
            'emaid'
        ] = 'USLCD123456789'
        raw['userVehicleData'][i]['vehicleConfig']['chargingAccounts'][0][
            'vehicleId'
        ] = '0123456789abcdef01234567'
        raw['userVehicleData'][i]['vehicleState']['gps']['location'][
            'latitude'
        ] = 36.47830261552359
        raw['userVehicleData'][i]['vehicleState']['gps']['location'][
            'longitude'
        ] = -118.83961697631453

    raw_json = json.dumps(raw)
    digest = sha1(raw_json.encode()).digest().hex()

    output_file = f"login_response_{digest}.json"
    with open(output_file, "w") as fi:
        fi.write(raw_json)

    print(f"Saved anonymized reply to {output_file}")
    print(
        "Please validate that there is no identifying information in there"
        " before publishing it."
    )


asyncio.run(main())
