using System;
using System.Collections;
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
    public class StatisticsDate
    {
        public DateTime Date { get; set; }
        public List<Game> Game { get; set; }
    }
}