namespace DeepPulse.Research
{
    public enum TradeSide
    {
        Long,
        Short
    }

    public enum ResearchSetup
    {
        IvbOrbBreakout,
        DeepEffortFirstRetest,
        BreakoutRetest,
        TrendPullback,
        Reversal
    }

    public sealed class IvbBreakoutEvent
    {
        public DateTime SessionStart { get; set; }
        public DateTime EntryTime { get; set; }
        public int EntryPinT { get; set; }
        public int OpeningRangeHighPinT { get; set; }
        public int OpeningRangeLowPinT { get; set; }
        public TradeSide Side { get; set; }
        public ResearchSetup Setup { get; set; } = ResearchSetup.IvbOrbBreakout;
    }

    public sealed class TradePulseSnapshot
    {
        public int TargetElapsedSeconds { get; set; }
        public double ObservedElapsedSeconds { get; set; }

        public int CurrentTicks { get; set; }
        public int MfeTicks { get; set; }
        public int MaeTicks { get; set; }

        public long BuyVolume { get; set; }
        public long SellVolume { get; set; }
        public long RawDelta { get; set; }
        public long DirectionalDelta { get; set; }
        public long FavorableVolume { get; set; }
        public long OpposingVolume { get; set; }

        public double DeltaPerSecond { get; set; }
        public double DeltaSlopePerSecond { get; set; }
        public double CandidateFlowEfficiency { get; set; }
        public double SecondsSinceNewMfe { get; set; }
    }
}
