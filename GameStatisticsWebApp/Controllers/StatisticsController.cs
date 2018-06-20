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
        XPathNodeIterator NodeGame;
        String strExpression;
        XPathDocument xpathDoc;
        String sessionTime;
        TimeSpan totalPlayingTime;
        private TimeSpan totalPlayingTimeResult;
        private Double totalPlayingTimeDouble;
        String repetitions;
        int totalRepetitions;
        public string valueString;
        public int valueInt;
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
        public String gameString;
        String dateInput;
        private DateTime startTime;
        private DateTime endTime;
        DateTime tempDate;
        DateTime parsedDate;
        DateTime lastDate;
        List<StatisticsDate> statisticsDateList;
        List<StatisticsWeek> statisticsWeekList;
        List<Game> gameList;
        List<Statistics> statisticsList;
        public Game game;
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
        public int gameID;
        public string gameName;
        Dictionary<int, string> games = new Dictionary<int, string>();


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

        //action result to show user statistics (default call of user statistics)
        //redirects to daily user statistics
        public ActionResult ShowUserStatistics(FormCollection fc)
        {
            var userId = Int16.Parse(fc[0].ToString());

            return RedirectToAction("ShowDailyUserStatistics", "Statistics", new { userId });
        }

        //action result to show physio statistics
        //redirects to daily physio statistics
        public ActionResult ShowPhysioStatistics(FormCollection fc)
        {
            var userId = Int16.Parse(fc[0].ToString());

            return RedirectToAction("ShowDailyPhysioStatistics", "Statistics", new { userId });
        }

        //show daily user statistics, returns viewmodel to view
        public ActionResult ShowDailyUserStatistics(int userId)
        {
            //get the daily statistics and store them in list
            statisticsDateList = GetDailyUserStatistics(userId);

            var viewModel = new ViewModel();

            //add the statistics to the viewmodel
            viewModel.StatisticsDate = statisticsDateList;

            //create user from given userId
            var user = new User() { UserId = userId };

            //add user to viewmodel
            viewModel.User = user;

            return View(viewModel);

        }

        //show weekly user statistics, returns viewmodel to view
        public ActionResult ShowWeeklyUserStatistics(int userId)
        {
            //get the weekly statistics
            statisticsWeekList = GetWeeklyUserStatistics(userId);

            var viewModel = new ViewModel();

            //add statistics to viewmodel
            viewModel.StatisticsWeek = statisticsWeekList;

            //create user
            var user = new User() { UserId = userId };

            //add user to viewmodel
            viewModel.User = user;

            return View(viewModel);

        }

        //show daily physio statistics
        public ActionResult ShowDailyPhysioStatistics(int userId)
        {
            //get the daily physio data
            statisticsDateList = GetDailyPhysioStatistics(userId);

            var viewModel = new ViewModel();

            //add data to viewmodel
            viewModel.StatisticsDate = statisticsDateList;
            var user = new User() { UserId = userId };

            viewModel.User = user;

            return View(viewModel);

        }

        //show weekly physio statistics
        public ActionResult ShowWeeklyPhysioStatistics(int userId)
        {
            //get weekly statistics
            statisticsWeekList = GetWeeklyPhysioStatistics(userId);

            var viewModel = new ViewModel();

            //add statistics to viewmodel
            viewModel.StatisticsWeek = statisticsWeekList;
            var user = new User() { UserId = userId };

            viewModel.User = user;

            return View(viewModel);

        }

        //ajax user check to check if user_id that is given in the user_id field is existing in the database
        public ActionResult AjaxUserCheck()
        {
            //connect to database
            SqlConnection conn = new SqlConnection(
                WebConfigurationManager.ConnectionStrings["GameDataConnectionString"].ConnectionString);
            conn.Open();

            //query
            SqlCommand command = new SqlCommand("Select Distinct user_id from dbo.session_data", conn);

            User[] allUsers = null;

            //store users in array while reading the results of the query
            using (var reader = command.ExecuteReader())
            {
                var user_ids = new List<User>();
                while (reader.Read())
                    user_ids.Add(new User { UserId = reader.GetInt16(0) });
                allUsers = user_ids.ToArray();
            }

            conn.Close();

            //return user array in Json format
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
            gameString = "./gameID";

            //Node iterator for all xml
            NodeIter = nav.Select(strExpression);

            //create new empty lists
            statisticsDateList = new List<StatisticsDate>();
            gameList = new List<Game>();
            statisticsList = new List<Statistics>();

            //get the existing games with id's and names
            games = GetGames();

            //for each session iterate through:
            foreach (XPathNavigator item in NodeIter)
            {
                //sets last date to lastDate
                lastDate = parsedDate;
                Node = item.Select(dateString);
                dateInput = GetStringValue(Node);
                parsedDate = DateTime.Parse(dateInput);

                //create only new statisticsDate when then last date was not the same date as the new one -> otherwise it would create a new date for each session, even when it's the same date
                if (lastDate.Date != parsedDate.Date)
                {
                    statisticsDate = new StatisticsDate() { Date = parsedDate.Date };
                    gameList = new List<Game>();
                }

                NodeDate = nav.Select(strExpression + "[time='" + dateInput + "']");

                //for each date iterate through and save statistics for date
                foreach (XPathNavigator dateItem in NodeDate)
                {   
                    //select node for game
                    Node = dateItem.Select(gameString);
                    gameID = GetIntValue(Node);
                    gameName = GetGameName(games, gameID);

                    //check if game already is in gameList
                    bool containsItem = gameList.Any(i => i.Id == gameID);

                    //if game is not there yet, create a new game, otherwise add to existing game statistics
                    if (containsItem == false)
                    {
                        game = new Game();
                        game.Name = gameName;
                        game.Id = gameID;
                        statisticsList = new List<Statistics>();
                    }

                    //get the existing game from the list
                    else
                    {
                        game = gameList.Find(x => x.Id == gameID);
                    }

                    
                    NodeGame = dateItem.Select(strExpression + "[gameID='" + gameID + "']" + "[time='" + dateInput + "']");

                    //for each date iterate through and save statistics for game
                    foreach (XPathNavigator gameItem in NodeGame)
                    {
                        statistics = new Statistics() { Name = "User Statistics" };
                        //select repetitions nodes
                        Node = gameItem.Select(repetitions);
                        //calculate total repetitions with selected node
                        statistics.Repetitions = GetTotalRepetitions(Node);

                        //select sessionTime node
                        Node = gameItem.Select(sessionTime);
                        statistics.TotalPlayingTime = GetTotalPlayingTime(Node);

                        //select node
                        Node = gameItem.Select(scoreString);
                        statistics.GameScore = GetValue(Node);

                        //add statistics to list
                        statisticsList.Add(statistics);    
                    }

                    //add statistics to game
                    game.Statistics = statisticsList;

                    //add game to the list if it not exists
                    if (containsItem == false)
                        gameList.Add(game);

                    //add gamelist to date
                    statisticsDate.Game = gameList;

                }

                //add statisticsDate if it's not the same as the last date (otherwise continues adding to the date)
                if (lastDate.Date != parsedDate.Date)
                    statisticsDateList.Add(statisticsDate);
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
            gameString = "./gameID";

            //Node iterator for user
            NodeIter = nav.Select(strExpression + "[playerID=" + userId + "]");

            statisticsDateList = new List<StatisticsDate>();
            gameList = new List<Game>();
            statisticsList = new List<Statistics>();
            games = GetGames();

            //for user session iterate through:
            foreach (XPathNavigator item in NodeIter)
            {
                lastDate = parsedDate;
                Node = item.Select(dateString);
                dateInput = GetStringValue(Node);
                parsedDate = DateTime.Parse(dateInput);

                //create only new statisticsDate when then last date was not the same date as the new one -> otherwise it would create a new date for each session, even when it's the same date
                if (lastDate.Date != parsedDate.Date)
                {
                    statisticsDate = new StatisticsDate() { Date = parsedDate.Date };
                    gameList = new List<Game>();
                }

                NodeDate = nav.Select(strExpression + "[time='" + dateInput + "']");

                //for each date iterate through and save statistics for date
                foreach (XPathNavigator dateItem in NodeDate)
                {
                    Node = dateItem.Select(gameString);
                    gameID = GetIntValue(Node);
                    gameName = GetGameName(games, gameID);

                    //check if game already is in gameList
                    bool containsItem = gameList.Any(i => i.Id == gameID);

                    //if game is not there yet, create a new game, otherwise add to existing game statistics
                    if (containsItem == false)
                    {
                        game = new Game();
                        game.Name = gameName;
                        game.Id = gameID;
                        statisticsList = new List<Statistics>();
                    }

                    else
                    {
                        game = gameList.Find(x => x.Id == gameID);
                    }

                    //X Path expression to select nodes
                    NodeGame = dateItem.Select(strExpression + "[gameID='" + gameID + "']" + "[time='" + dateInput + "']");

                    //for each date iterate through and save statistics for game
                    foreach (XPathNavigator gameItem in NodeGame)
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

                        statisticsList.Add(statistics);

                    }
                    game.Statistics = statisticsList;

                    //add game to list if it's not there yet
                    if (containsItem == false)
                        gameList.Add(game);

                    statisticsDate.Game = gameList;

                }

                if (lastDate.Date != parsedDate.Date)
                    statisticsDateList.Add(statisticsDate);
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
            sessionTime = "./sessionTime";
            repetitions = "./repetitions";
            scoreString = "./gameScore";
            gameString = "./CompletedSession/gameID";

            statisticsWeekList = new List<StatisticsWeek>();
            games = GetGames();


            for (var counter = 1; counter <= weekCount; counter++)
            {
                conn.Open();
                SqlCommand command = new SqlCommand("Select xml from dbo.session_data where user_id = @userId and (date Between CONVERT(datetime,@startingDate, 0) AND CONVERT(datetime,@endingDate,0)) FOR XML RAW, ELEMENTS", conn);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.Add("@startingDate", SqlDbType.DateTime).Value = startingDate.ToShortDateString();
                command.Parameters.Add("@endingDate", SqlDbType.DateTime).Value = endingDate.ToShortDateString();

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
                    EndingDate = endingDate.AddDays(-1)
                };

                gameList = new List<Game>();
                statisticsList = new List<Statistics>();
                statistics = new Statistics() {};
                totalPlayingTimeResult = default(TimeSpan);

                //for session iterate through:
                foreach (XPathNavigator item in NodeIter)
                {
                    Node = item.Select(gameString);
                    gameID = GetIntValue(Node);
                    gameName = GetGameName(games, gameID);

                    //if game is not there yet, create a new game, otherwise add to existing game statistics
                    bool containsItem = gameList.Any(i => i.Id == gameID);

                    //check if game already is in gameList
                    if (containsItem == false)
                    {
                        game = new Game();
                        game.Name = gameName;
                        game.Id = gameID;
                        statisticsList = new List<Statistics>();
                    }

                    else
                    {
                        game = gameList.Find(x => x.Id == gameID);
                    }

                    statistics = new Statistics() { Name = "User Statistics" };
                    totalPlayingTimeResult = default(TimeSpan);

                    NodeGame = item.Select(strExpression + "/CompletedSession" + "[gameID='" + gameID + "']");

                    //for each date iterate through and save statistics for game
                    foreach (XPathNavigator gameItem in NodeGame)
                    {                       
                        //select repetitions nodes
                        Node = gameItem.Select(repetitions);
                        //calculate total repetitions with selected node
                        statistics.Repetitions += GetTotalRepetitions(Node);

                        //select sessionTime node
                        Node = gameItem.Select(sessionTime);
                        totalPlayingTimeResult = totalPlayingTimeResult.Add(GetTotalPlayingTime(Node));
                        statistics.TotalPlayingTime = totalPlayingTimeResult;

                        //select node
                        Node = gameItem.Select(scoreString);
                        statistics.GameScore += GetValue(Node);
                       
                    }

                    //check if statisticslist hasn't any statistics
                    if (!statisticsList.Any())
                    {
                        statisticsList.Add(statistics);
                        game.Statistics = statisticsList;
                    }

                    //add game to list if it's not there yet
                    if (containsItem == false)
                    {
                        gameList.Add(game);
                    }
                        
                }
                
                statisticsWeek.Game = gameList;
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

            //calculate amount of weeks
            var weekCount = CalculateWeeks(conn, userId);

            //calculate the dates for the week
            DateTime startingDate = CalculateStartingDate(conn, userId);
            DateTime endingDate = startingDate.AddDays(7);

            //create a navigator to iterate through nodes
            //String variables are the different nodes to iterate through

            strExpression = "//ArrayOfCompletedSession";
            sessionTime = "./sessionTime";
            repetitions = "./repetitions";
            scoreString = "./CompletedSession/gameScore";
            moveRangeMaximumString = "./moveRangeMaximum";
            moveRangeMinimumString = "./moveRangeMinimum";
            moveRangeAverageString = "./moveRangeAverage";
            moveVelocityAverageString = "./moveVelocityAverage";
            gamePausesString = "./gamePauses";
            sessionPauseString = "./gamePauses/SessionPause";
            startTimeString = "./startTime";
            endTimeString = "./endTime";
            gameString = "./CompletedSession/gameID";

            statisticsWeekList = new List<StatisticsWeek>();

            //get list of games
            games = GetGames();

            for (var counter = 1; counter <= weekCount; counter++)
            {

                conn.Open();
                SqlCommand command = new SqlCommand("Select xml from dbo.session_data where user_id = @userId and (date Between CONVERT(datetime,@startingDate, 0) AND CONVERT(datetime,@endingDate,0)) FOR XML RAW, ELEMENTS", conn);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.Add("@startingDate", SqlDbType.DateTime).Value = startingDate.ToShortDateString();
                command.Parameters.Add("@endingDate", SqlDbType.DateTime).Value = endingDate.ToShortDateString();

                //ReadXML(command);

                //create XML reader and XPathDocument to read the XML
                XmlReader reader = command.ExecuteXmlReader();
                xpathDoc = new XPathDocument(reader, XmlSpace.Preserve);

                nav = xpathDoc.CreateNavigator();

                //Node iterator for iterating through xml nodes
                NodeIter = nav.Select(strExpression);

                //create new week
                statisticsWeek = new StatisticsWeek()
                {
                    Week = counter,
                    StartingDate = startingDate,
                    EndingDate = endingDate.AddDays(-1)
                };

                //create new lists
                gameList = new List<Game>();
                statisticsList = new List<Statistics>();
                statistics = new Statistics() { };

                //set all variables to 0
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
                    Node = item.Select(gameString);
                    gameID = GetIntValue(Node);
                    gameName = GetGameName(games, gameID);

                    //if game is not there yet, create a new game, otherwise add to existing game statistics
                    bool containsItem = gameList.Any(i => i.Id == gameID);


                    if (containsItem == false)
                    {
                        game = new Game();
                        game.Name = gameName;
                        game.Id = gameID;
                        statisticsList = new List<Statistics>();
                    }

                    //get game id from the list
                    else
                    {
                        game = gameList.Find(x => x.Id == gameID);
                    }

                    statistics = new Statistics() { Name = "User Statistics" };
                    totalPlayingTimeResult = default(TimeSpan);

                    NodeGame = item.Select(strExpression + "/CompletedSession" + "[gameID='" + gameID + "']");

                    foreach (XPathNavigator gameItem in NodeGame)
                    {
                        //select repetitions nodes
                        Node = gameItem.Select(repetitions);
                        //calculate total repetitions with selected node
                        statistics.Repetitions += GetTotalRepetitions(Node);

                        //select sessionTime node
                        Node = gameItem.Select(sessionTime);
                        totalPlayingTimeResult = totalPlayingTimeResult.Add(GetTotalPlayingTime(Node));
                        statistics.TotalPlayingTime = totalPlayingTimeResult;

                        //select node
                        Node = gameItem.Select(scoreString);
                        statistics.GameScore += GetValue(Node);

                        //select node
                        Node = gameItem.Select(moveRangeMaximumString);
                        statistics.MoveRangeMaximum = GetWeeklyMaximumValue(Node);

                        //select node
                        Node = gameItem.Select(moveRangeMinimumString);
                        statistics.MoveRangeMinimum = GetWeeklyMinimumValue(Node);

                        //select node
                        Node = gameItem.Select(moveRangeAverageString);
                        statistics.MoveRangeAverage = GetWeeklyMoveRangeAverageValue(Node);

                        //select node
                        Node = gameItem.Select(moveVelocityAverageString);
                        statistics.MoveVelocityAverage = GetWeeklyMoveVelocityAverageValue(Node);

                        //select node
                        Node = gameItem.Select(gamePausesString);
                        statistics.NumberOfPauses += CalculateGamePauses(Node);

                        Node = gameItem.Select(sessionPauseString);
                        statistics.PauseLength += CalculatePauseLength(Node);
                    }

                    //add statistics to list if it hasn't any (so it only carries out 1 time)
                    if (!statisticsList.Any())
                    {
                        statisticsList.Add(statistics);
                        game.Statistics = statisticsList;
                    }

                    //add game to list if it isn't in there yet
                    if (containsItem == false)
                    {
                        gameList.Add(game);
                    }

                }

                statisticsWeek.Game = gameList;
                statisticsWeekList.Add(statisticsWeek);

                startingDate = startingDate.AddDays(7);
                endingDate = endingDate.AddDays(7);

                //disconnect from server
                conn.Close();

            }

            return statisticsWeekList;

        }

        //get the connection for the database
        public SqlConnection GetSqlConnection()
        {
            SqlConnection conn = new SqlConnection(
                WebConfigurationManager.ConnectionStrings["GameDataConnectionString"].ConnectionString);

            return conn;
        }

        //get query for daily stats
        public SqlCommand GetSqlDailyCommand(SqlConnection conn, int userId)
        {
            SqlCommand command = new SqlCommand("Select xml from dbo.session_data where user_id = @userId Order by date DESC FOR XML RAW, ELEMENTS", conn);
            command.Parameters.AddWithValue("@userId", userId);

            return command;
        }

        //calculate amount of weeks
        public int CalculateWeeks(SqlConnection conn, int userId)
        {
            conn.Open();

            SqlCommand dates = new SqlCommand("Select min(date) as minimum, max(date) as maximum from dbo.session_data where user_id = @UserId", conn);
            dates.Parameters.AddWithValue("@UserId", userId);

            DateTime minDate = default(DateTime);
            DateTime maxDate = default(DateTime);

            SqlDataReader datesReader = dates.ExecuteReader();

            //set max and min data and calculate the difference between days to know how many weeks
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

        //calculates how many nodes there are for pauses
        public int CalculateGamePauses(XPathNodeIterator Node)
        {
            var pauses = 0;

            pauses = Node.Count;

            return pauses;
        }

        //calculates the length of the pauses by calculating between start and end of nodes
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

        //calculate average of move range
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

        //get the weekly movement velocity average
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

        //gets the string value from the node
        public string GetStringValue(XPathNodeIterator Node)
        {
            //for every node
            foreach (XPathNavigator i in Node)
            {
                valueString = i.Value;
            }

            return valueString;
        }

       //gets the int value of the node
        public int GetIntValue(XPathNodeIterator Node)
        {
            //for every node
            foreach (XPathNavigator i in Node)
            {
                valueInt = Int32.Parse(i.Value);
            }

            return valueInt;
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

        //this method gets all the games by the id (which is in the xml), 0 = Cave Game
        public Dictionary<int, string> GetGames()
        {
            games.Add(0, "Cave Game");
            games.Add(1, "Bubble Runner");
            //add one more game 
            games.Add(2, "Squat Pong");

            return games;

        }

        //function retunns game name by ID
        public string GetGameName(Dictionary<int,string> games, int gameID)
        {
            gameName = games[gameID];

            return gameName;
        }

    }
}
