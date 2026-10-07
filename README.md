# deepPulse

Research-first DeepCharts custom indicator project for **Trade Pulse**: post-entry scalp quality analysis for NQ/MNQ.

## Current goal

The first controlled experiment is a **30-minute RTH IVB/ORB breakout**.

The codebase intentionally does **not** contain a 0-100 Pulse score yet. The development order is:

```text
objective entry event
→ tick-by-tick post-entry data
→ 1s / 3s / 5s / 10s / 15s snapshots
→ historical outcomes
→ offline analysis
→ out-of-sample validation
→ final Pulse model
```

## First prototype

The initial DeepCharts indicator:

- runs as a desktop-only research indicator;
- requests historical + realtime trades with `OnTickCall = HistRT`;
- builds the opening range from exchange-time tick data;
- detects the first confirmed breakout;
- creates a virtual research entry;
- tracks price response and aggressive order flow;
- creates in-memory snapshots at 1, 3, 5, 10 and 15 seconds.

DeepCharts custom indicators are sandboxed and cannot use `System.IO.File`, so direct CSV writing from the indicator is deliberately **not** implemented. Dataset extraction will be solved separately after the first runtime/API validation.

## Build

Requirements:

- DeepCharts installed on Windows
- .NET 10 SDK
- DeepCharts Developer ID

Default DeepCharts path:

```text
C:\Program Files\Volumetrica Trading\Deepchart\
```

Build:

```powershell
dotnet build src/DeepPulse.csproj -p:DeepchartDevId=YOUR-DEVELOPER-ID
```

If DeepCharts is installed elsewhere:

```powershell
dotnet build src/DeepPulse.csproj `
  -p:DeepchartDevId=YOUR-DEVELOPER-ID `
  -p:DeepchartDir="D:\Apps\Deepchart\"
```

The DLL is written to:

```text
%USERPROFILE%\Documents\Deepchart\Indicators\
```

## Research defaults

- Market: NQ / MNQ
- RTH start: configurable exchange time; prototype default 08:30
- Opening range: 30 minutes
- Breakout confirmation: 1 tick beyond the range
- First valid breakout only
- Snapshots: 1s, 3s, 5s, 10s, 15s
- No Pulse score until data supports one

See `docs/TradePulse-Research-v1.1.md`.
