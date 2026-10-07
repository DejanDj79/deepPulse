# Trade Pulse Research Plan v1.1

## Purpose

Trade Pulse is a **post-entry scalp quality monitor**, not an entry signal generator.

The research question is:

> After entry, does market behavior during the first 1–15 seconds resemble trades that historically continue or trades that fail?

No 0–100 Pulse score is allowed until the data supports one.

## First controlled experiment: IVB / ORB breakout

The first serious experiment is an objective RTH opening-range breakout.

Initial defaults:

- NQ / MNQ
- exchange-time RTH start configurable in the indicator
- prototype default: 08:30 exchange time
- opening range: 30 minutes
- breakout confirmation: 1 tick beyond OR high/low
- first breakout only
- virtual entry on the confirming trade
- snapshots: 1s, 3s, 5s, 10s, 15s

The detector reproduces only the objective opening-range breakout core. It does **not** attempt to clone Deep-M IVB proprietary bias, projection, protection or reaction-zone logic.

## Core feature families

1. Price response
   - current favorable/adverse ticks
   - MFE
   - MAE
   - stagnation / time since new MFE

2. Aggressive order flow
   - buy volume
   - sell volume
   - raw delta
   - direction-normalized delta
   - delta per second
   - delta slope

3. Effort vs result
   - favorable price displacement relative to favorable executed volume
   - this is a candidate feature, not a fixed score formula

4. Time / progress
   - snapshots at 1/3/5/10/15 seconds
   - later evaluate future 10/20/30 second outcomes

DOM/order-book inputs remain experimental and are not required in the first dataset.

## Planned outcome labels

Primary candidate:

```text
+20 favorable ticks before -10 adverse ticks
```

Secondary outcomes should later include:

- +10 before -10
- +20 before -15
- +30 before -15
- future MFE / MAE
- time to favorable/adverse thresholds
- IVB-specific: return inside opening range and time to return

## Dataset methodology

Do not randomly split rows.

Use chronological partitions:

```text
TRAIN → VALIDATION → TEST
```

All snapshots from the same trade/session remain in the same partition.

Initial targets:

- 500–1,000 events: logger validation
- 5,000+ events: development
- 10,000+ across multiple setup families if practical

Do not inflate sample count with highly correlated pseudo-trades.

## Second controlled experiment

Deep Effort first retest, **if the proprietary zone/event can be accessed objectively through the platform**.

If not, do not reverse engineer or pretend to reproduce Deep Effort internals.

## Later setup families

- generic breakout/retest
- trend pullback
- reversal/rejection

If the same post-entry relationships survive across several setup families, that supports a general Trade Pulse model rather than an IVB-specific trade manager.

## Technical finding: historical ticks are available

For research, the indicator can use:

```csharp
OnTickCall = CallHandler.HistRT;
```

DeepCharts documents this as desktop-only behavior and replays historical trades to `OnTick`.

That enables objective IVB detection and post-entry microstructure collection without real or SIM orders.

## Technical finding: direct CSV writing is not allowed

DeepCharts validates custom indicator DLLs and rejects forbidden APIs including:

```text
System.IO.File
files
network
processes
threads/tasks
```

Therefore the indicator must not directly write `trades.csv` or `snapshots.csv`.

The first prototype keeps research records in memory and can optionally write compact diagnostic rows through `VAn.AddToLog`.

A scalable dataset extraction method must be validated separately before large-sample collection.

## Current development sequence

1. Build/load the DLL in DeepCharts.
2. Verify historical `OnTick(HistRT)` behavior.
3. Verify the configured RTH start on NQ/MNQ.
4. Validate 30-minute OR high/low visually.
5. Validate first breakout timestamp and direction.
6. Validate 1/3/5/10/15s snapshots.
7. Solve scalable dataset extraction.
8. Add future-outcome labeling.
9. Run pilot IVB dataset.
10. Analyze features offline.
11. Only then design the Pulse score.

## Success criterion

The project only succeeds if unseen historical data shows that post-entry measurements materially separate favorable from deteriorating trades.

A good UI without out-of-sample separation is not considered a successful Trade Pulse model.
