using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameStatisticsWebApp.Models
{
    public class Statistics
    {
        public string Name { get; set; }
        public int Repetitions { get; set; }
        public TimeSpan TotalPlayingTime { get; set; }
        public float GameScore { get; set; }
        public float MoveVelocityAverage { get; set; }
        public float MoveRangeAverage { get; set; }
        public float MoveRangeMinimum { get; set; }
        public float MoveRangeMaximum { get; set; }
        public TimeSpan PauseLength { get; set; }
        public int NumberOfPauses { get; set; }

    }
}