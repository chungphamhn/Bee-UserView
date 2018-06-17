using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace GameStatisticsWebApp.Models
{
    //viewmodel has a user, a list of dates and a list of weeks
    //weeks and dates have games
    //games have statistics
    //all models have their own needed attributes
    public class ViewModel
    {
        public User User { get; set; }
        public List<StatisticsDate> StatisticsDate { get; set; }
        public List<StatisticsWeek> StatisticsWeek { get; set; }
    }

}