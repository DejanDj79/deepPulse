namespace DeepPulse.Research
{
    /// <summary>
    /// Objective RTH opening-range breakout detector used only to create
    /// repeatable research entry events. It does not implement Deep-M IVB's
    /// proprietary projection/protection logic.
    /// </summary>
    public sealed class IvbOrbDetector
    {
        private readonly TimeSpan openingRangeDuration;
        private readonly int confirmationTicks;
        private readonly bool firstBreakoutOnly;

        private DateTime? sessionStart;
        private int? openingRangeHighPinT;
        private int? openingRangeLowPinT;
        private bool longTriggered;
        private bool shortTriggered;
        private bool anyTriggered;

        public IvbOrbDetector(TimeSpan openingRangeDuration, int confirmationTicks, bool firstBreakoutOnly)
        {
            this.openingRangeDuration = openingRangeDuration;
            this.confirmationTicks = Math.Max(1, confirmationTicks);
            this.firstBreakoutOnly = firstBreakoutOnly;
        }

        public DateTime? SessionStart => sessionStart;
        public int? OpeningRangeHighPinT => openingRangeHighPinT;
        public int? OpeningRangeLowPinT => openingRangeLowPinT;

        public void Reset()
        {
            sessionStart = null;
            openingRangeHighPinT = null;
            openingRangeLowPinT = null;
            longTriggered = false;
            shortTriggered = false;
            anyTriggered = false;
        }

        public bool ProcessTick(DateTime exchangeTime, DateTime rthSessionStart, int tradePinT, out IvbBreakoutEvent breakout)
        {
            breakout = null;

            if (exchangeTime < rthSessionStart)
                return false;

            EnsureSession(rthSessionStart);

            DateTime rangeEnd = rthSessionStart.Add(openingRangeDuration);

            if (exchangeTime < rangeEnd)
            {
                UpdateOpeningRange(tradePinT);
                return false;
            }

            if (!openingRangeHighPinT.HasValue || !openingRangeLowPinT.HasValue)
                return false;

            if (firstBreakoutOnly && anyTriggered)
                return false;

            int longTriggerPinT = openingRangeHighPinT.Value + confirmationTicks;
            int shortTriggerPinT = openingRangeLowPinT.Value - confirmationTicks;

            if (!longTriggered && tradePinT >= longTriggerPinT)
            {
                longTriggered = true;
                anyTriggered = true;
                breakout = CreateBreakout(exchangeTime, tradePinT, TradeSide.Long);
                return true;
            }

            if (!shortTriggered && tradePinT <= shortTriggerPinT)
            {
                shortTriggered = true;
                anyTriggered = true;
                breakout = CreateBreakout(exchangeTime, tradePinT, TradeSide.Short);
                return true;
            }

            return false;
        }

        private void EnsureSession(DateTime rthSessionStart)
        {
            if (sessionStart.HasValue && sessionStart.Value == rthSessionStart)
                return;

            sessionStart = rthSessionStart;
            openingRangeHighPinT = null;
            openingRangeLowPinT = null;
            longTriggered = false;
            shortTriggered = false;
            anyTriggered = false;
        }

        private void UpdateOpeningRange(int tradePinT)
        {
            if (!openingRangeHighPinT.HasValue || tradePinT > openingRangeHighPinT.Value)
                openingRangeHighPinT = tradePinT;

            if (!openingRangeLowPinT.HasValue || tradePinT < openingRangeLowPinT.Value)
                openingRangeLowPinT = tradePinT;
        }

        private IvbBreakoutEvent CreateBreakout(DateTime exchangeTime, int tradePinT, TradeSide side)
        {
            return new IvbBreakoutEvent
            {
                SessionStart = sessionStart.Value,
                EntryTime = exchangeTime,
                EntryPinT = tradePinT,
                OpeningRangeHighPinT = openingRangeHighPinT.Value,
                OpeningRangeLowPinT = openingRangeLowPinT.Value,
                Side = side,
                Setup = ResearchSetup.IvbOrbBreakout
            };
        }
    }
}
