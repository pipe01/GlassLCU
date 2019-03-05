# GlassLCU
[![Build status](https://ci.appveyor.com/api/projects/status/urvum2o9ikibdv6d?svg=true)](https://ci.appveyor.com/project/pipe01/glasslcu)

Automatically-generating LCU API for C#. This uses my [LCU API Generator](https://github.com/pipe01/lcu-api-generator) for generating the classes necessary for interacting between the LCU API and C#.

To setup, simply run the `generate.ps1` script in the `GlassLCU/API/Generator` folder.

To initalise the connection, create a `LeagueClient` instance (make sure to reuse it throughout your program) and call `Init` or `BeginTryInit`, the latter being used to periodically try to connect to the client in the asynchronously.
