import sys
import pathlib
import asyncio
import argparse
import rich
import logging

# Allow running straight out of the repo
sys.path.insert(0, str(pathlib.Path(__file__).parent.parent.absolute()))

from lucidmotors import LucidAPI, Region  # noqa: E402

logging.basicConfig(level=logging.ERROR)


async def main(username, password):
    async with LucidAPI(region=region) as lucid:
        await lucid.login(username, password)
        # rich.print(lucid.user)
        await lucid.fetch_vehicles()
        rich.print(lucid.vehicles)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Fahrzeuginfo abrufen via LucidAPI")
    parser.add_argument("--username", required=True, help="Dein Lucid Account Username")
    parser.add_argument("--password", required=True, help="Dein Lucid Account Passwort")
    parser.add_argument("--region", default="EU", help="Region, z.B. EU, US, ...")
    args = parser.parse_args()

    region = getattr(Region, args.region.upper(), Region.US)
    
    asyncio.run(main(args.username, args.password))
