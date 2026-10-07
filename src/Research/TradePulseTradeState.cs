namespace DeepPulse.Research
{
    /// <summary>
    /// Collects direction-normalized post-entry measurements.
    /// It deliberately produces raw research features, not a Pulse score.
    /// </summary>
    public sealed class TradePulseTradeState
    {
        private readonly int[] snapshotSeconds = new[] { 1, 3, 5, 10, 15 };
        private int nextSnapshotIndex;

        private long buyVolume;
        private long sellVolume;
        private int currentTicks;
        private int mfeTicks;
        private int maeTicks;
        private DateTime lastMfeTime;

        private long previousSnapshotDirectionalDelta;
        private double previousSnapshotElapsedSeconds;

        public TradePulseTradeState(IvbBreakoutEvent entry)
        {
            Entry = entry;
            lastMfeTime = entry.EntryTime;
        }

        public IvbBreakoutEvent Entry { get; }
        public List<TradePulseSnapshot> Snapshots { get; } = new List<TradePulseSnapshot>();

        public bool SnapshotCollectionComplete => nextSnapshotIndex >= snapshotSeconds.Length;

        public int AddTrade(
            DateTime exchangeTime,
            int tradePinT,
            long volume,
            bool aggressiveBuy,
            bool aggressiveSell)
        {
            if (exchangeTime <= Entry.EntryTime)
                return 0;

            if (aggressiveBuy)
                buyVolume += volume;
            else if (aggressiveSell)
                sellVolume += volume;

            currentTicks = Entry.Side == TradeSide.Long
                ? tradePinT - Entry.EntryPinT
                : Entry.EntryPinT - tradePinT;

            if (currentTicks > mfeTicks)
            {
                mfeTicks = currentTicks;
                lastMfeTime = exchangeTime;
            }

            if (-currentTicks > maeTicks)
                maeTicks = -currentTicks;

            int before = Snapshots.Count;
            double elapsed = (exchangeTime - Entry.EntryTime).TotalSeconds;

            while (nextSnapshotIndex < snapshotSeconds.Length &&
                   elapsed >= snapshotSeconds[nextSnapshotIndex])
            {
                CreateSnapshot(exchangeTime, elapsed, snapshotSeconds[nextSnapshotIndex]);
                nextSnapshotIndex++;
            }

            return Snapshots.Count - before;
        }

        private void CreateSnapshot(DateTime exchangeTime, double observedElapsed, int targetElapsed)
        {
            long rawDelta = buyVolume - sellVolume;
            long directionalDelta = Entry.Side == TradeSide.Long ? rawDelta : -rawDelta;
            long favorableVolume = Entry.Side == TradeSide.Long ? buyVolume : sellVolume;
            long opposingVolume = Entry.Side == TradeSide.Long ? sellVolume : buyVolume;

            double deltaPerSecond = observedElapsed > 0
                ? directionalDelta / observedElapsed
                : 0;

            double deltaSlope = 0;
            if (Snapshots.Count > 0)
            {
                double dt = observedElapsed - previousSnapshotElapsedSeconds;
                if (dt > 0)
                    deltaSlope = (directionalDelta - previousSnapshotDirectionalDelta) / dt;
            }

            // Candidate research feature only. It is NOT the final Pulse formula.
            double flowEfficiency = favorableVolume > 0
                ? (double)currentTicks / favorableVolume
                : 0;

            Snapshots.Add(new TradePulseSnapshot
            {
                TargetElapsedSeconds = targetElapsed,
                ObservedElapsedSeconds = observedElapsed,
                CurrentTicks = currentTicks,
                MfeTicks = Math.Max(0, mfeTicks),
                MaeTicks = Math.Max(0, maeTicks),
                BuyVolume = buyVolume,
                SellVolume = sellVolume,
                RawDelta = rawDelta,
                DirectionalDelta = directionalDelta,
                FavorableVolume = favorableVolume,
                OpposingVolume = opposingVolume,
                DeltaPerSecond = deltaPerSecond,
                DeltaSlopePerSecond = deltaSlope,
                CandidateFlowEfficiency = flowEfficiency,
                SecondsSinceNewMfe = Math.Max(0, (exchangeTime - lastMfeTime).TotalSeconds)
            });

            previousSnapshotDirectionalDelta = directionalDelta;
            previousSnapshotElapsedSeconds = observedElapsed;
        }
    }
}
