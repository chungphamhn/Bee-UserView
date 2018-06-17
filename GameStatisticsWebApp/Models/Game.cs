using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GameStatisticsWebApp.Models
{
    //viewmodel has a user, a list of dates and a list of weeks
    //weeks and dates have games
    //games have statistics
    //all models have their own needed attributes
    public class Game
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public List<Statistics> Statistics { get; set; }
    }
}