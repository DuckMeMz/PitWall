# PitWall

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF-0C54C2)
![Status](https://img.shields.io/badge/Status-Work%20In%20Progress-F59E0B)

PitWall is a work-in-progress Windows desktop application that turns [OpenF1](https://openf1.org/docs/) session data into an interactive Formula 1 replay.

Load a session, move through its timeline, follow the cars on the circuit map, and inspect timing and telemetry for each driver.

I started PitWall as a practical way to deepen my C# skills and improve my ability to handle, model, and present real API data that is not always complete or consistent.

![PitWall replay interface](docs/images/pitwall-replay.png)

## Current features

- Load the latest session or a specific OpenF1 session key.
- Play, pause, stop, seek, and change the speed of a session replay.
- Generate a circuit outline from recorded location data.
- Follow team-coloured driver markers around the circuit.
- View position, speed, gear, DRS, gaps, lap number, and coordinates for every driver.
- Select a driver to inspect throttle, brake, RPM, speed, and position.

## Technical highlights

- Builds one replay timeline from separate location, telemetry, timing, position, and lap feeds.
- Uses a Polly resilience pipeline to rate-limit requests and retry `429` responses with exponential backoff.
- Handles missing API data, unexpected JSON values, and updates that arrive late or more than once.
- Interpolates location, speed, throttle, brake, RPM, and timing gaps for smoother playback.
- Uses MVVM to keep the WPF interface separate from data loading and playback.

## How it works

OpenF1 provides each type of session data separately, with its own timestamps and update rate. PitWall:

1. Downloads the session, drivers, locations, telemetry, positions, intervals, laps, meeting information, and race-control messages.
2. Ignores unusable entries, sorts each feed by time, and removes duplicate samples.
3. Groups the data by driver and builds a replay timeline.
4. Works out each driver's state at the current playback time, filling the gaps between nearby updates where possible.
5. Sends that state to the WPF interface through the MVVM layer.

This keeps the raw API data and replay logic separate from the interface.

## Getting started

### Requirements

- Windows
- An internet connection
- For the **Requires .NET** release: [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- For running from source: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

PitWall uses OpenF1's free access to historical data from 2023 onward. Because the app does not use a paid OpenF1 account, it cannot load data while an F1 session is live, even if you select an older session. Wait until the live session has ended and OpenF1 has made the data public before loading a replay.

### Option 1: Download a release

Download the latest Windows build from the [Releases](https://github.com/DuckMeMz/PitWall/releases) page.

Two builds are available:

- **Requires .NET**: Smaller download, requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
- **Self-contained**: Larger download, includes the .NET runtime.

### Option 2: Run from source

```powershell
git clone https://github.com/DuckMeMz/PitWall.git
cd PitWall
dotnet restore
dotnet run --project PitWall/PitWall.csproj
```

### Load a replay

1. Enter `latest` or a numeric OpenF1 `session_key`.
2. Select **Load** and wait for the session data to be processed. See [current limitations](#current-limitations).
3. Use the playback controls and timeline to explore the session.
4. Select a driver in the timing table to focus their marker and telemetry.

Until the session finder is added, use `latest` or one of the example session keys below.

### Sessions to try

| Session key | Session | Why explore it? |
| --- | --- | --- |
| `latest` | Latest available OpenF1 session | See whichever practice, qualifying, sprint, or race session OpenF1 currently returns. |
| `9636` | [2024 São Paulo Grand Prix](https://www.formula1.com/en/latest/article/verstappen-wins-chaotic-sao-paulo-grand-prix-after-stunning-recovery-from.1DIc8pzRmGbC3jHJmvtBi1) | Verstappen's recovery from P17 to pole and an Alpine double podium. |
| `9165` | [2023 Singapore Grand Prix](https://www.formula1.com/en/latest/article/sainz-holds-off-norris-and-fast-charging-mercedes-pair-to-take-sensational.16sNsRUz2MAFyXxSE3RdwX) | Sainz using Norris's DRS to defend from both Mercedes. |
| `9507` | [2024 Miami Grand Prix](https://www.formula1.com/en/latest/article/norris-beats-verstappen-for-breakthrough-maiden-f1-victory-in-action-packed.7f9W6X9L3kPQILyO3NljL1) | Lando Norris's first Formula 1 victory. |
| `9558` | [2024 British Grand Prix](https://www.formula1.com/en/latest/article/hamilton-beats-verstappen-to-first-win-since-2021-with-record-breaking-9th.3teU9bznaWJlC2TGAYh0Vl.3teU9bznaWJlC2TGAYh0Vl) | Lewis Hamilton's ninth British Grand Prix win. |


## Project structure

| Area | Responsibility |
| --- | --- |
| `PitWall/Models/OpenF1Api` | Models the OpenF1 endpoint responses |
| `PitWall/Models/Replay` | Represents normalised, time-addressable replay state |
| `PitWall/Services/OpenF1` | Builds queries and handles HTTP, rate limiting, retries, and deserialisation |
| `PitWall/Services/Sessions` | Finds sessions and aggregates their data streams |
| `PitWall/Services/Replay` | Builds the replay timeline and projects location data onto the map |
| `PitWall/ViewModels` | Coordinates loading, playback, driver selection, telemetry, and map state |
| `PitWall/Views` | Contains the WPF interface |

## Current limitations

- A session is downloaded and held in memory before playback begins, so large sessions can take time to load (Up to around 80 seconds)
- There is no local cache yet; loading the same session repeats the API requests.
- Sessions must be selected using `latest` or a raw session key.
- Visual accuracy depends on the completeness and timing of the source data.
- A session displays all of the data including before and after the race, where many cars may be stationary.
- The interface and error feedback are still evolving.

## Roadmap

- [ ] Add buffered and chunked loading to reduce startup time and memory usage.
- [ ] Add a SQLite cache for downloaded session data.
- [ ] Build a session finder so users can browse events instead of looking up session keys.
- [ ] Display race-control messages on the replay timeline.
- [ ] Add synchronised team-radio playback.
- [ ] Create a custom graph data section, so users can combine and view chosen data.
- [ ] Continue improving the UI, loading feedback, and error handling.