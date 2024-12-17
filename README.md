METAR Telegram Bot

This Telegram bot, written in C#, allows users to retrieve and decode METAR weather reports. METAR is a standard format for reporting weather conditions, primarily used in aviation for airports and weather stations.
Features

    Retrieve METAR Reports: The bot can fetch the latest METAR reports for any airport by its ICAO code.
    Decode METAR Reports: The bot decodes METAR reports into a human-readable format, explaining weather conditions, visibility, temperature, wind speed, and more.
    User-friendly: Users can easily interact with the bot via Telegram to get METAR reports in seconds.

Prerequisites

Before running the bot, ensure you have the following installed:

    .NET SDK (any version compatible with C# 7.0 or later)
    A Telegram bot token from BotFather
	
Usage

    Start the Bot: Open Telegram, search for your bot's username, and click "Start".
    Request METAR Report: Send the bot the ICAO code of an airport (e.g., KJFK, EDDM).

The bot will send back a decoded version of the METAR, explaining the weather data in simple terms.

    Bot:
    METAR KJFK 171351Z 24011KT 7SM BKN013 BKN055 OVC075 12/10 A3013 RMK AO2 SLP201 T01220100
    Report Type: METAR
    ICAO: KJFK 
    Date: 17 December 2024
    Time: 13:51 UTC | Last updated: 53 minutes ago
    Wind: 240° at 11 knot(s)
    Visibility: 7 Statute Mile(s)
    Ceilings: Broken Clouds at 1300 ft., Broken Clouds at 5500 ft., Overcast at 7500 ft.
    Temperature: 12°C, Dew point: 10°C
    Pressure: 3013 inHg

Acknowledgments

    METAR data is sourced from public meteorological APIs.
    Thanks to Telegram.Bot for the excellent library powering this Telegram bot.
