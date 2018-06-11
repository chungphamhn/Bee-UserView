using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace GameStatisticsWebApp.Models
{
    public class ViewModel
    {
        public User User { get; set; }
        public List<StatisticsDate> StatisticsDate { get; set; }
        public List<StatisticsWeek> StatisticsWeek { get; set; }
    }

}