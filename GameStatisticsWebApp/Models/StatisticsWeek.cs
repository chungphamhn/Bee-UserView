using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameStatisticsWebApp.Models
{
    public class StatisticsWeek
    {
        public Statistics Statistics { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public int Week { get; set; }    
    }
}