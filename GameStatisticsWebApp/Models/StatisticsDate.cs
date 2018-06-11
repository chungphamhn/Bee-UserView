using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace GameStatisticsWebApp.Models
{
    public class StatisticsDate
    {
        public DateTime Date { get; set; }
        public Statistics Statistics { get; set; }

    }
}