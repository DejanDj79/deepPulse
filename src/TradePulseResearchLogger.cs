using System.ComponentModel;
using DeepPulse.Research;
using VolSysAPI;
using VolSysAPI.Indicators;
using VolumetricaControls;
using VolumetricaCore;
using Feed = VolumetricaAPI.Connection.Structure;
using static VolSysAPI.ExternalStructure;
using static VolSysAPI.Structure;

namespace DeepPulse
{
    /// <summary>
    /// Research harness for the first Trade Pulse experiment.
    /// It creates a virtual entry on an objective IVB/ORB breakout and then
    /// records raw post-entry features at fixed time snapshots.
    /// </summary>
    public class TradePulseResearchLogger : Indicator
    {
        public static IndicatorDescriptionBase Register()
        {
            return new IndicatorDescriptionBase
            {
                Name = "Deep Pulse - Research Logger",
                Description = "IVB/ORB virtual-entry research logger for post-entry Trade Pulse development.",
                Tags = new List<string> { "Research", "Order flow", "Scalping", "IVB", "ORB" }
            };
        }

        [Category("IVB / ORB")]
        [DisplayName("RTH start (exchange time)")]
        [VolCustom(CategoryIndex = 0, PropertyIndex = 0)]
        public TimeSpan RthStartExchangeTime { get; set; } = new TimeSpan(8, 30, 0);

        [Category("IVB / ORB")]
        [DisplayName("Opening range minutes")]
        [VolCustom(CategoryIndex = 0, PropertyIndex = 1, MinValue = 1, MaxValue = 180, IncrementValue = 1)]
        public int OpeningRangeMinutes { get; set; } = 30;

        [Category("IVB / ORB")]
        [DisplayName("Breakout confirmation ticks")]
        [VolCustom(CategoryIndex = 0, PropertyIndex = 2, MinValue = 1, MaxValue = 100, IncrementValue = 1)]
        public int BreakoutConfirmationTicks { get; set; } = 1;

        [Category("IVB / ORB")]
        [DisplayName("First breakout only")]
        [VolCustom(CategoryIndex = 0, PropertyIndex = 3)]
        public bool FirstBreakoutOnly { get; set; } = true;

        [Category("Diagnostics")]
        [DisplayName("Write research events to app log")]
        [VolCustom(CategoryIndex = 1, PropertyIndex = 0)]
        public bool DiagnosticLogging { get; set; } = false;

        private IvbOrbDetector detector;
        private TradePulseTradeState activeTrade;
        private int loggedSnapshotCount;

        public override void OnSet(bool setDefault, bool themeOverride)
        {
            // Historical trade replay is desktop-only, which is intentional for research.
            OnTickCall = CallHandler.HistRT;
            Description = $"IVB {OpeningRangeMinutes}m · confirm {BreakoutConfirmationTicks}t";
        }

        public override void OnLoad()
        {
            detector = new IvbOrbDetector(
                TimeSpan.FromMinutes(OpeningRangeMinutes),
                BreakoutConfirmationTicks,
                FirstBreakoutOnly);

            activeTrade = null;
            loggedSnapshotCount = 0;
            StatusMessage = "Research mode: no Pulse score. IVB virtual entries + raw snapshots only.";
        }

        public override void OnTick(Feed.TickByTick tick, bool isRt, AggrInfo aggrInfo)
        {
            DateTime exchangeTime = tick.exDt;
            DateTime rthStart = exchangeTime.Date.Add(RthStartExchangeTime);

            // Premarket/overnight data is intentionally ignored by this first experiment.
            if (exchangeTime < rthStart)
                return;

            if (detector.ProcessTick(exchangeTime, rthStart, tick.PinT, out var breakout))
            {
                activeTrade = new TradePulseTradeState(breakout);
                loggedSnapshotCount = 0;

                if (DiagnosticLogging)
                {
                    VAn.AddToLog(
                        $"DeepPulse IVB_ENTRY {breakout.EntryTime:O} {breakout.Side} " +
                        $"entryPinT={breakout.EntryPinT} orHigh={breakout.OpeningRangeHighPinT} " +
                        $"orLow={breakout.OpeningRangeLowPinT}",
                        VolumetricaAPI.LogLevelEnum.Information);
                }

                // The confirming breakout tick defines the virtual entry.
                // Post-entry measurements start with the next trade.
                return;
            }

            if (activeTrade == null)
                return;

            bool aggressiveBuy = tick.AggrSide == Feed.AggressorSideEnum.Ask;
            bool aggressiveSell = tick.AggrSide == Feed.AggressorSideEnum.Bid;

            int added = activeTrade.AddTrade(
                exchangeTime,
                tick.PinT,
                tick.Vol,
                aggressiveBuy,
                aggressiveSell);

            if (!DiagnosticLogging || added <= 0)
                return;

            while (loggedSnapshotCount < activeTrade.Snapshots.Count)
            {
                var s = activeTrade.Snapshots[loggedSnapshotCount++];
                VAn.AddToLog(
                    $"DeepPulse SNAPSHOT t={s.TargetElapsedSeconds}s " +
                    $"side={activeTrade.Entry.Side} pxTicks={s.CurrentTicks} " +
                    $"mfe={s.MfeTicks} mae={s.MaeTicks} " +
                    $"buyVol={s.BuyVolume} sellVol={s.SellVolume} " +
                    $"dirDelta={s.DirectionalDelta} deltaSlope={s.DeltaSlopePerSecond:F2} " +
                    $"flowEff={s.CandidateFlowEfficiency:F6}",
                    VolumetricaAPI.LogLevelEnum.Information);
            }
        }
    }
}
