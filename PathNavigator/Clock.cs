using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript
{
    partial class Program
    {
        class Clock
        {
            public int Runtime
            {
                get; private set;
            }
            public static double SecondsPerTick
            {
                get; private set;
            }

            public static void Initialize (UpdateFrequency frequency)
            {
                switch (frequency)
                {
                    case (UpdateFrequency.Update1):
                        SecondsPerTick = 1.0 / 60;
                        break;
                    case (UpdateFrequency.Update10):
                        SecondsPerTick = 1.0 / 6;
                        break;
                    case (UpdateFrequency.Update100):
                        SecondsPerTick = 5.0 / 3;
                        break;
                }
            }

            public void Start ()
            {
                Runtime = 0;
            }

            public void Update ()
            {
                Runtime++;
                if (Runtime >= int.MaxValue)
                    Runtime = 0;
            }

            public double GetSeconds (int start)
            {
                int diff = Runtime - start;
                return diff * SecondsPerTick;
            }

            public int GetTick (double seconds)
            {
                double ticks = seconds / SecondsPerTick;
                int roundedTicks = (int)Math.Ceiling(ticks);
                return Runtime + roundedTicks;
            }
        }

    }
}
