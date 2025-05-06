import sys
import pathlib
import asyncio
import getpass
import rich
import time
import logging

# Allow running straight out of the repo
sys.path.insert(0, str(pathlib.Path(__file__).parent.parent.absolute()))

from lucidmotors import LucidAPI  # noqa: E402

logging.basicConfig(level=logging.DEBUG)

print("Please enter your Lucid account credentials.")

username = input("Username: ")
password = getpass.getpass()


async def main():
    async with LucidAPI() as lucid:
        await lucid.login(username, password)
        print("Logged in. User profile:")
        rich.print(lucid.user)

        print("Vehicles:")
        rich.print(lucid.vehicles)

        time.sleep(1)

        print("Waking up vehicle")
        await lucid.wakeup_vehicle(lucid.vehicles[0])

        print("... Sleeping 30s give the car a chance to wake up ...")
        print("... first. If any fail, just run the script again. ...")
        time.sleep(30)

        print("Opening charge port door")
        await lucid.charge_port_open(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Turning on defrost")
        await lucid.defrost_on(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Unlocking doors")
        await lucid.doors_unlock(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Opening frunk")
        await lucid.frunk_open(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Opening trunk")
        await lucid.trunk_open(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Honking horn")
        await lucid.honk_horn(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Flashing lights")
        await lucid.lights_flash(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Closing trunk")
        await lucid.trunk_close(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Closing frunk")
        await lucid.frunk_close(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Locking doors")
        await lucid.doors_lock(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Turning off defrost")
        await lucid.defrost_off(lucid.vehicles[0])

        print("... Sleeping 5s to be nice ...")
        time.sleep(5)

        print("Closing charge port door")
        await lucid.charge_port_close(lucid.vehicles[0])

        print("Then refreshing vehicle info")
        await lucid.fetch_vehicles()
        rich.print(lucid.vehicles)


asyncio.run(main())
