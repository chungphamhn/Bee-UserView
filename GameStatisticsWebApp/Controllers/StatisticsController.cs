using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GameStatisticsWebApp.Models;
using System.Web.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.XPath;

namespace GameStatisticsWebApp.Controllers
{


    public class StatisticsController : Controller
    {
        XPathNavigator nav;
        XPathNodeIterator NodeIter;
        XPathNodeIterator Node;
        private XPathNodeIterator NewNode;
        XPathNodeIterator NodeDate;
        String strExpression;
        XPathDocument xpathDoc;
        String sessionTime;
        TimeSpan totalPlayingTime;
        private TimeSpan totalPlayingTimeResult;
        private Double totalPlayingTimeDouble;
        String repetitions;
        String measurements;
        Double velocity;
        Double delta = 0;
        Double measurementsPerSecond = 10;
        int totalRepetitions;
        public string valueString;
        public String moveRangeMaximumString;
        public String moveRangeMinimumString;
        public String moveRangeAverageString;
        public String moveVelocityAverageString;
        public String scoreString;
        public String dateString;
        public String gamePausesString;
        public String sessionPauseString;
        public String startTimeString;
        public String endTimeString;
        String dateInput;
        private DateTime startTime;
        private DateTime endTime;
        DateTime tempDate;
        DateTime parsedDate;
        List<StatisticsDate> statisticsDateList;
        List<StatisticsWeek> statisticsWeekList;
        public StatisticsDate statisticsDate;
        public Statistics statistics;
        public StatisticsWeek statisticsWeek;
        private float maximumValue;
        float minimumValue = 999999;
        private int averageCounterMR;
        private int averageCounterMV;
        private float averageMR;
        private float averageMV;
        private float averageTotalMR;
        private float averageTotalMV;
        public float valueFloat;
        public float valueFloatMR;
        public float valueFloatMV;


        // GET: Statistics
        public ActionResult Index()
        {
            var statistics = new Statistics() { Name = "Game statistics" };
            return View(statistics);

        }

        public ActionResult Physiotherapist()
        {
            var statistics = new Statistics() { Name = "Physiotherapist Statistics" };
            return View(statistics);
        }

        public ActionResult User()
        {
            var statistics = new Statistics() { Name = "User Statistics" };

            return View(statistics);
        }

        public ActionResult ShowUserStatistics(FormCollection fc)
        {
            var userId = Int16.Parse(fc[0].ToString());

            return RedirectToAction("ShowDailyUserStatistics", "Statistics", new { userId });
        }

        public ActionResult ShowPhysioStatistics(FormCollection fc)
        {
            var userId = Int16.Parse(fc[0].ToString());

            return RedirectToAction("ShowDailyPhysioStatistics", "Statistics", new { userId });
        }

        public ActionResult ShowDailyUserStatistics(int userId)
        {
            statisticsDateList = GetDailyUserStatistics(userId);

            var viewModel = new ViewModel();

            viewModel.StatisticsDate = statisticsDateList;
            var user = new User() { UserId = userId };

            viewModel.User = user;

            return View(viewModel);

        }

        public ActionResult ShowWeeklyUserStatistics(int userId)
        {
            statisticsWeekList = GetWeeklyUserStatistics(userId);

            var viewModel = new ViewModel();

            viewModel.StatisticsWeek = statisticsWeekList;
            var user = new User() { UserId = userId };

            viewModel.User = user;

            return View(viewModel);

        }

        public ActionResult ShowDailyPhysioStatistics(int userId)
        {
            statisticsDateList = GetDailyPhysioStatistics(userId);

            var viewModel = new ViewModel();

            viewModel.StatisticsDate = statisticsDateList;
            var user = new User() { UserId = userId };

            viewModel.User = user;

            return View(viewModel);

        }

        public ActionResult ShowWeeklyPhysioStatistics(int userId)
        {
            statisticsWeekList = GetWeeklyPhysioStatistics(userId);

            var viewModel = new ViewModel();

            viewModel.StatisticsWeek = statisticsWeekList;
            var user = new User() { UserId = userId };

            viewModel.User = user;

            return View(viewModel);

        }

        public ActionResult AjaxUserCheck()
        {

            SqlConnection conn = new SqlConnection(
                WebConfigurationManager.ConnectionStrings["GameDataConnectionString"].ConnectionString);
            conn.Open();

            SqlCommand command = new SqlCommand("Select Distinct user_id from dbo.session_data", conn);

            User[] allUsers = null;

            using (var reader = command.ExecuteReader())
            {
                var user_ids = new List<User>();
                while (reader.Read())
                    user_ids.Add(new User { UserId = reader.GetInt16(0) });
                allUsers = user_ids.ToArray();
            }

            conn.Close();

            return Json(allUsers, JsonRequestBehavior.AllowGet);
        }

        //get the daily statistics for one user
        public List<StatisticsDate> GetDailyUserStatistics(int userId)
        {
            SqlConnection conn = GetSqlConnection();
            conn.Open();

            SqlCommand command = GetSqlDailyCommand(conn, userId);

            //create XML reader and XPathDocument to read the XML
            XmlReader reader = command.ExecuteXmlReader();
            xpathDoc = new XPathDocument(reader, XmlSpace.Preserve);

            //create a navigator to iterate through nodes
            //String variables are the different nodes to iterate through
            nav = xpathDoc.CreateNavigator();
            strExpression = "//ArrayOfCompletedSession/CompletedSession";
            sessionTime = "./sessionTime";
            repetitions = "./repetitions";
            scoreString = "./gameScore";
            dateString = "./time";

            //Node iterator for all xml
            NodeIter = nav.Select(strExpression);

            statisticsDateList = new List<StatisticsDate>();

            //for each session iterate through:
            foreach (XPathNavigator item in NodeIter)
            {
                Node = item.Select(dateString);
                dateInput = GetStringValue(Node);
                parsedDate = DateTime.Parse(dateInput);

                statisticsDate = new StatisticsDate() { Date = parsedDate };

                NodeDate = nav.Select(strExpression + "[time='" + dateInput + "']");

                //for each date iterate through and save statistics for date
                foreach (XPathNavigator dateItem in NodeDate)
                {
                    statistics = new Statistics() { Name = "User Statistics" };
                    //select repetitions nodes
                    Node = dateItem.Select(repetitions);
                    //calculate total repetitions with selected node
                    statistics.Repetitions = GetTotalRepetitions(Node);

                    //select sessionTime node
                    Node = dateItem.Select(sessionTime);
                    statistics.TotalPlayingTime = GetTotalPlayingTime(Node);

                    //select node
                    Node = dateItem.Select(scoreString);
                    statistics.GameScore = GetValue(Node);

                    statisticsDate.Statistics = statistics;

                    statisticsDateList.Add(statisticsDate);
                }
            }

            //disconnect from server
            conn.Close();

            return statisticsDateList;

        }

        public List<StatisticsDate> GetDailyPhysioStatistics(int userId)
        {
            SqlConnection conn = GetSqlConnection();
            conn.Open();

            SqlCommand command = GetSqlDailyCommand(conn, userId);

            //create XML reader and XPathDocument to read the XML
            XmlReader reader = command.ExecuteXmlReader();
            xpathDoc = new XPathDocument(reader, XmlSpace.Preserve);

            //create a navigator to iterate through nodes
            //String variables are the different nodes to iterate through
            nav = xpathDoc.CreateNavigator();

            strExpression = "//ArrayOfCompletedSession/CompletedSession";
            sessionTime = "./sessionTime";
            repetitions = "./repetitions";
            scoreString = "./gameScore";
            dateString = "./time";
            moveRangeMaximumString = "./moveRangeMaximum";
            moveRangeMinimumString = "./moveRangeMinimum";
            moveRangeAverageString = "./moveRangeAverage";
            moveVelocityAverageString = "./moveVelocityAverage";
            gamePausesString = "./gamePauses";
            sessionPauseString = "./gamePauses/SessionPause";
            startTimeString = "./startTime";
            endTimeString = "./endTime";

            //Node iterator for user
            NodeIter = nav.Select(strExpression + "[playerID=" + userId + "]");

            statisticsDateList = new List<StatisticsDate>();

            //for user session iterate through:
            foreach (XPathNavigator item in NodeIter)
            {
                Node = item.Select(dateString);
                dateInput = GetStringValue(Node);
                parsedDate = DateTime.Parse(dateInput);

                statisticsDate = new StatisticsDate() { Date = parsedDate };

                NodeDate = nav.Select(strExpression + "[time='" + dateInput + "']");

                //for each date iterate through and save statistics for date
                foreach (XPathNavigator dateItem in NodeDate)
                {
                    statistics = new Statistics() { Name = "User Statistics" };

                    //select repetitions nodes
                    Node = dateItem.Select(repetitions);
                    //calculate total repetitions with selected node
                    statistics.Repetitions = GetTotalRepetitions(Node);

                    //select sessionTime node
                    Node = dateItem.Select(sessionTime);
                    statistics.TotalPlayingTime = GetTotalPlayingTime(Node);

                    //select node
                    Node = dateItem.Select(moveRangeMaximumString);
                    statistics.MoveRangeMaximum = GetValue(Node);

                    //select node
                    Node = dateItem.Select(moveRangeMinimumString);
                    statistics.MoveRangeMinimum = GetValue(Node);

                    //select node
                    Node = dateItem.Select(moveRangeAverageString);
                    statistics.MoveRangeAverage = GetValue(Node);

                    //select node
                    Node = dateItem.Select(moveVelocityAverageString);
                    statistics.MoveVelocityAverage = GetValue(Node);

                    //select node
                    Node = dateItem.Select(scoreString);
                    statistics.GameScore = GetValue(Node);

                    //select node
                    Node = dateItem.Select(gamePausesString);
                    statistics.NumberOfPauses = CalculateGamePauses(Node);

                    Node = dateItem.Select(sessionPauseString);
                    statistics.PauseLength = CalculatePauseLength(Node);

                    statisticsDate.Statistics = statistics;

                    statisticsDateList.Add(statisticsDate);
                }
            }

            //disconnect from server
            conn.Close();

            return statisticsDateList;
        }

        //get the weekly statistics for one user
        public List<StatisticsWeek> GetWeeklyUserStatistics(int userId)
        {
            SqlConnection conn = GetSqlConnection();

            var weekCount = CalculateWeeks(conn, userId);

            DateTime startingDate = CalculateStartingDate(conn, userId);
            DateTime endingDate = startingDate.AddDays(7);

            //create a navigator to iterate through nodes
            //String variables are the different nodes to iterate through
            
            strExpression = "//ArrayOfCompletedSession";
            sessionTime = "./CompletedSession/sessionTime";
            repetitions = "./CompletedSession/repetitions";
            scoreString = "./CompletedSession/gameScore";

            statisticsWeekList = new List<StatisticsWeek>();
            

            for (var counter = 1; counter <= weekCount; counter++)
            {

                conn.Open();
                SqlCommand command = new SqlCommand("Select xml from dbo.session_data where user_id = @userId and (date Between CONVERT(datetime,@startingDate, 0) AND CONVERT(datetime,@endingDate,0)) FOR XML RAW, ELEMENTS", conn);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.Add("@startingDate", SqlDbType.DateTime).Value = startingDate;
                command.Parameters.Add("@endingDate", SqlDbType.DateTime).Value = endingDate;

                //ReadXML(command);

                //create XML reader and XPathDocument to read the XML
                XmlReader reader = command.ExecuteXmlReader();
                xpathDoc = new XPathDocument(reader, XmlSpace.Preserve);

                nav = xpathDoc.CreateNavigator();

                //Node iterator for iterating through xml nodes
                NodeIter = nav.Select(strExpression);

                statisticsWeek = new StatisticsWeek()
                {
                    Week = counter,
                    StartingDate = startingDate,
                    EndingDate = endingDate
                };

                statistics = new Statistics() {};
                totalPlayingTimeResult = default(TimeSpan);

                //for session iterate through:
                foreach (XPathNavigator item in NodeIter)
                {
                    statistics = new Statistics() { Name = "User Statistics" };
                    //select repetitions nodes
                    Node = item.Select(repetitions);
                    //calculate total repetitions with selected node
                    statistics.Repetitions += GetTotalRepetitions(Node);

                    //select sessionTime node
                    Node = item.Select(sessionTime);
                    totalPlayingTimeResult = totalPlayingTimeResult.Add(GetTotalPlayingTime(Node));
                    statistics.TotalPlayingTime += totalPlayingTimeResult;

                    //select node
                    Node = item.Select(scoreString);
                    statistics.GameScore += GetValue(Node);
                }

                statisticsWeek.Statistics = statistics;
                statisticsWeekList.Add(statisticsWeek);

                startingDate = startingDate.AddDays(7);
                endingDate = endingDate.AddDays(7);

                //disconnect from server
                conn.Close();

            }

            return statisticsWeekList;

        }

        public List<StatisticsWeek> GetWeeklyPhysioStatistics(int userId)
        {
            SqlConnection conn = GetSqlConnection();

            var weekCount = CalculateWeeks(conn, userId);

            DateTime startingDate = CalculateStartingDate(conn, userId);
            DateTime endingDate = startingDate.AddDays(7);

            //create a navigator to iterate through nodes
            //String variables are the different nodes to iterate through

            strExpression = "//ArrayOfCompletedSession";
            sessionTime = "./CompletedSession/sessionTime";
            repetitions = "./CompletedSession/repetitions";
            scoreString = "./CompletedSession/gameScore";
            moveRangeMaximumString = "./CompletedSession/moveRangeMaximum";
            moveRangeMinimumString = "./CompletedSession/moveRangeMinimum";
            moveRangeAverageString = "./CompletedSession/moveRangeAverage";
            moveVelocityAverageString = "./CompletedSession/moveVelocityAverage";
            gamePausesString = "./CompletedSession/gamePauses";
            sessionPauseString = "./CompletedSession/gamePauses/SessionPause";
            startTimeString = "./startTime";
            endTimeString = "./endTime";

            statisticsWeekList = new List<StatisticsWeek>();

            for (var counter = 1; counter <= weekCount; counter++)
            {

                conn.Open();
                SqlCommand command = new SqlCommand("Select xml from dbo.session_data where user_id = @userId and (date Between CONVERT(datetime,@startingDate, 0) AND CONVERT(datetime,@endingDate,0)) FOR XML RAW, ELEMENTS", conn);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.Add("@startingDate", SqlDbType.DateTime).Value = startingDate;
                command.Parameters.Add("@endingDate", SqlDbType.DateTime).Value = endingDate;

                //ReadXML(command);

                //create XML reader and XPathDocument to read the XML
                XmlReader reader = command.ExecuteXmlReader();
                xpathDoc = new XPathDocument(reader, XmlSpace.Preserve);

                nav = xpathDoc.CreateNavigator();

                //Node iterator for iterating through xml nodes
                NodeIter = nav.Select(strExpression);

                statisticsWeek = new StatisticsWeek()
                {
                    Week = counter,
                    StartingDate = startingDate,
                    EndingDate = endingDate
                };

                statistics = new Statistics() { };

                averageMR = 0;
                averageMV = 0;
                averageTotalMR = 0;
                averageTotalMV = 0;
                averageCounterMR = 0;
                averageCounterMV = 0;
                totalPlayingTimeResult = default(TimeSpan);

                //for session iterate through:
                foreach (XPathNavigator item in NodeIter)
                {
                    //select repetitions nodes
                    Node = item.Select(repetitions);
                    //calculate total repetitions with selected node
                    statistics.Repetitions += GetTotalRepetitions(Node);

                    //select sessionTime node
                    Node = item.Select(sessionTime);
                    totalPlayingTimeResult = totalPlayingTimeResult.Add(GetTotalPlayingTime(Node));
                    statistics.TotalPlayingTime = totalPlayingTimeResult;

                    //select node
                    Node = item.Select(scoreString);
                    statistics.GameScore += GetValue(Node);

                    //select node
                    Node = item.Select(moveRangeMaximumString);
                    statistics.MoveRangeMaximum = GetWeeklyMaximumValue(Node);

                    //select node
                    Node = item.Select(moveRangeMinimumString);
                    statistics.MoveRangeMinimum = GetWeeklyMinimumValue(Node);

                    //select node
                    Node = item.Select(moveRangeAverageString);
                    statistics.MoveRangeAverage = GetWeeklyMoveRangeAverageValue(Node);

                    //select node
                    Node = item.Select(moveVelocityAverageString);
                    statistics.MoveVelocityAverage = GetWeeklyMoveVelocityAverageValue(Node);

                    //select node
                    Node = item.Select(gamePausesString);
                    statistics.NumberOfPauses += CalculateGamePauses(Node);

                    Node = item.Select(sessionPauseString);
                    statistics.PauseLength += CalculatePauseLength(Node);
                }

                statisticsWeek.Statistics = statistics;
                statisticsWeekList.Add(statisticsWeek);

                startingDate = startingDate.AddDays(7);
                endingDate = endingDate.AddDays(7);

                //disconnect from server
                conn.Close();
            }

            return statisticsWeekList;

        }

        public SqlConnection GetSqlConnection()
        {
            SqlConnection conn = new SqlConnection(
                WebConfigurationManager.ConnectionStrings["GameDataConnectionString"].ConnectionString);

            return conn;
        }

        public SqlCommand GetSqlDailyCommand(SqlConnection conn, int userId)
        {
            SqlCommand command = new SqlCommand("Select xml from dbo.session_data where user_id = @userId Order by date DESC FOR XML RAW, ELEMENTS", conn);
            command.Parameters.AddWithValue("@userId", userId);

            return command;
        }

        public int CalculateWeeks(SqlConnection conn, int userId)
        {
            conn.Open();

            SqlCommand dates = new SqlCommand("Select min(date) as minimum, max(date) as maximum from dbo.session_data where user_id = @UserId", conn);
            dates.Parameters.AddWithValue("@UserId", userId);

            DateTime minDate = default(DateTime);
            DateTime maxDate = default(DateTime);

            SqlDataReader datesReader = dates.ExecuteReader();
            while (datesReader.Read())
            {
                minDate = datesReader.GetDateTime(0);
                maxDate = datesReader.GetDateTime(1);
            }

            TimeSpan span = maxDate - minDate;
            int days = span.Days;

            var weekCount = (int)Math.Ceiling((double)days / 7);

            conn.Close();

            return weekCount;
        }

        public DateTime CalculateStartingDate(SqlConnection conn, int userId)
        {
            conn.Open();

            SqlCommand command = new SqlCommand("Select xml, date from dbo.session_data where user_id = @UserId Order by date DESC FOR XML RAW, ELEMENTS", conn);
            command.Parameters.AddWithValue("@UserId", userId);

            //create XML reader and XPathDocument to read the XML
            XmlReader reader = command.ExecuteXmlReader();
            xpathDoc = new XPathDocument(reader, XmlSpace.Preserve);

            //create a navigator to iterate through nodes
            //String variables are the different nodes to iterate through
            nav = xpathDoc.CreateNavigator();
            strExpression = "//ArrayOfCompletedSession/CompletedSession";
            dateString = "./time";

            NodeIter = nav.Select(strExpression);

            foreach (XPathNavigator item in NodeIter)
            {
                Node = item.Select(dateString);
                dateInput = GetStringValue(Node);
                tempDate = DateTime.Parse(dateInput);
            }

            conn.Close();

            return tempDate;
        }

        public int CalculateGamePauses(XPathNodeIterator Node)
        {
            var pauses = 0;

            pauses = Node.Count;

            return pauses;
        }

        public TimeSpan CalculatePauseLength(XPathNodeIterator Node)
        {
            TimeSpan time = default(TimeSpan);

            foreach (XPathNavigator i in Node)
            {
                NewNode = i.Select(startTimeString);

                foreach (XPathNavigator x in NewNode)
                {
                    dateInput = x.Value;
                    
                }
                startTime = DateTime.Parse(dateInput);

                NewNode = i.Select(endTimeString);

                foreach (XPathNavigator x in NewNode)
                {
                    dateInput = x.Value;        
                }

                endTime = DateTime.Parse(dateInput);
                time += endTime.Subtract(startTime);

            }

            return time;
        }

        //method to read the XML with as input the SQL command
        public void ReadXML(SqlCommand command)
        {
            XmlReader reader = command.ExecuteXmlReader();

            while (reader.Read())
            {
                Debug.WriteLine(reader.ReadOuterXml());
            }

        }

        //method returns MoveRange Maximum, Minimum, Average
        public float GetValue(XPathNodeIterator Node)
        {
            //for every node
            foreach (XPathNavigator i in Node)
            {
                valueFloat = float.Parse(i.Value, System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            return valueFloat;
        }

        //method returns MoveRange Maximum, Minimum, Average
        public float GetWeeklyMinimumValue(XPathNodeIterator Node)
        {
            //for every node
            foreach (XPathNavigator i in Node)
            {
                valueFloat = float.Parse(i.Value, System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.NumberFormatInfo.InvariantInfo);

                if (valueFloat < minimumValue)
                {
                    minimumValue = valueFloat;
                }

            }

            return minimumValue;
        }

        public float GetWeeklyMaximumValue(XPathNodeIterator Node)
        {
            
            //for every node
            foreach (XPathNavigator i in Node)
            {
                valueFloat = float.Parse(i.Value, System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.NumberFormatInfo.InvariantInfo);

                if (valueFloat > maximumValue)
                {
                    maximumValue = valueFloat;
                }

            }

            return maximumValue;
        }

        public float GetWeeklyMoveRangeAverageValue(XPathNodeIterator Node)
        {
            //for every node
            foreach (XPathNavigator i in Node)
            {
                valueFloatMR = float.Parse(i.Value, System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.NumberFormatInfo.InvariantInfo);

                averageTotalMR = averageTotalMR + valueFloatMR;

                averageCounterMR++;
            }


            averageMR = averageTotalMR/ averageCounterMR;

            return averageMR;
        }

        public float GetWeeklyMoveVelocityAverageValue(XPathNodeIterator Node)
        {
            //for every node
            foreach (XPathNavigator i in Node)
            {
                valueFloatMV = float.Parse(i.Value, System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.NumberFormatInfo.InvariantInfo);

                averageTotalMV = averageTotalMV + valueFloatMV;

                averageCounterMV++;
            }

            averageMV = averageTotalMV / averageCounterMV;

            return averageMV;
        }



        public string GetStringValue(XPathNodeIterator Node)
        {
            //for every node
            foreach (XPathNavigator i in Node)
            {
                valueString = i.Value;
            }

            return valueString;
        }

        //method to calculate total session time

        public TimeSpan GetTotalPlayingTime(XPathNodeIterator Node)
        {
            totalPlayingTime = default(TimeSpan);
            //for each node in navigator
            foreach (XPathNavigator i in Node)
            {
                //get the value and add it to the session time
                totalPlayingTimeDouble = Double.Parse(i.Value, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo);
                totalPlayingTime = totalPlayingTime + TimeSpan.FromSeconds(totalPlayingTimeDouble);
            }
            //return session time
            return totalPlayingTime;
        }

        //method to calculate the total amount of repetitions
        public int GetTotalRepetitions(XPathNodeIterator Node)
        {
            totalRepetitions = 0;
            //for each node in navigator
            foreach (XPathNavigator i in Node)
            {
                //add value of repetition to the totalRepetitions
                totalRepetitions = totalRepetitions + Int32.Parse(i.Value);
            }
            //return totalRepetitions
            return totalRepetitions;
        }

    }
}
