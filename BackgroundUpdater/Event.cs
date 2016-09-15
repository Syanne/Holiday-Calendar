using System;

namespace BackgroundUpdater
{
    public sealed class Event
    {
        public string Value { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
