# FellowshipSim

A combat simulator for *Fellowship*. It models each hero's abilities, talents and gear from the game's datamine,
and checks itself against real combat logs: any logged fight can be replayed through the engine, and a review page
shows step by step how closely the engine matches what happened.

Supported heroes: **Rime**.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A FellowshipLogs API client (client ID and secret) from your [FellowshipLogs](https://www.fellowshiplogs.com) account,
  to download fights

The game data (the Codex datamine) is downloaded automatically on first run and cached in `~/.fellowsim/cache/codex`.

## Replaying a fight

```
dotnet run --project src/FellowshipSim.Server
```

Open the address it prints (e.g. `http://localhost:5000`), then:

1. Paste your FellowshipLogs API client ID and secret (asked once; saved in `data/settings.json` on your computer).
2. Paste a FellowshipLogs report link (or code) and load it.
3. Click the player you want to replay. The fight is downloaded, replayed, and its review appears in the list.

You can also set the credentials with the environment variables `FELLOWSHIPLOGS_CLIENT_ID` and
`FELLOWSHIPLOGS_CLIENT_SECRET` instead of entering them on the page.

## Tests

```
dotnet test
```

The tests run sims with every Rime talent and replay every fight you have downloaded (`data/fights`), rewriting their
review pages.

## Layout

| Folder | What it holds |
|---|---|
| `src/FellowshipSim.Core` | The engine: simulation kernel, heroes, talents, gear, game data, the replay driver and review pages |
| `src/FellowshipSim.Logs` | FellowshipLogs API client, and the builder that turns a logged fight into a replay script |
| `src/FellowshipSim.Server` | Local web server: the page in `wwwroot` and its JSON API |
| `tests/FellowshipSim.Tests` | Tests |
| `data/` | Your local data (not committed): downloaded fights, replay scripts, review pages, settings |
