using Lisa.Breakpoint.WebApi.database;
using Microsoft.AspNet.Mvc;
using Lisa.Breakpoint.WebApi.Models;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace Lisa.Breakpoint.WebApi
{
    [Route("reports")]
    public class ReportController : Controller
    {
        public ReportController(RavenDB db)
        {
            _db = db;
        }

        [HttpGet("{organizationSlug}/{projectSlug}/{userName}/{filter?}/{value?}")]
        public IActionResult Get(string organizationSlug, string projectSlug, string userName, string filter = "", string value = "", [FromQuery] string reported = null)
        {
            IList<Report> reports;
            IList<DateTime> dateTimes = new DateTime[2];

            if (reported != null)
            {
                //Calls a function to determine if reported has a correct specific value
                dateTimes = this._checkReported(reported);
                //When the specific value is invalid it will return a unprocessable entity status code
                if (dateTimes[0] == DateTime.MinValue.AddDays(1))
                {
                    return new HttpStatusCodeResult(422);
                }
            }

            //if the user not exist it'll return a 404
            if (_db.GetUser(userName) == null)
            {
                return new HttpNotFoundResult();
            }

            //if project isn't found it'll return an error 404
            if (_db.GetProject(organizationSlug, projectSlug, userName) == null)
            {
                return new HttpNotFoundResult();
            }
            
            if (filter != "" || dateTimes[0] != DateTime.MinValue)
            {
                //If the dateTimes is filled with not the standard value, overwrite the dateTimeObject for use in the database class
                DateTime[] dateTimeObject = new DateTime[2];
                if (dateTimes[0] != DateTime.MinValue)
                {
                    dateTimeObject[0] = dateTimes[0];
                    dateTimeObject[1] = dateTimes[1];
                }

                //If filter is filled set the filter and value for use in the database class
                Filter f = null;
                
                if (filter != "")
                {
                    f = new Filter(filter, value);
                }

                reports = _db.GetAllReports(organizationSlug, projectSlug, userName, dateTimeObject, f);
            } else { 
                reports = _db.GetAllReports(organizationSlug, projectSlug, userName);
            }
            
            if (reports == null)
            {
                return new HttpNotFoundResult();
            }
            return new HttpOkObjectResult(reports);
        }

        [HttpGet("{id}", Name = "report")]
        public IActionResult Get(int id)
        {
            Report report = _db.GetReport(id);

            if (report == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(report);
        }

        [HttpPost("{organizationslug}/{projectslug}")]
        public IActionResult Post([FromBody] Report report, string organizationSlug, string projectSlug)
        {

            if (report == null)
            {
                return new BadRequestResult();
            }

            if (report.Platforms.Count == 0)
            {
                report.Platforms.Add("Not specified");
            }

            _db.PostReport(report);

            string location = Url.RouteUrl("report", new { id = report.Number }, Request.Scheme);
            return new CreatedResult(location, report);
        }

        [HttpPatch("{id}/{userName}")]
        public IActionResult Patch(int id, string userName, [FromBody] Report report)
        {
            // use statuscheck.ContainKey(report.Status) when it is put in the general value file
            if (!statusCheck.Contains(report.Status))
            {
                return new BadRequestResult();
            }

            Report checkReport = _db.GetReport(id);

            Project checkProject = _db.GetProject(checkReport.Organization, checkReport.Project, userName);
            
            //If the status is Won't fix (approved) than it will check if the user is a manager, if that is not the case then return badrequestresult.
            if (report.Status == statusCheck[3])
            {
                foreach (var members in checkProject.Members)
                {
                    if (members.UserName == userName && members.Role != "manager")
                    {
                        return new BadRequestResult();
                    }
                    if (members.UserName == userName && members.Role == "manager")
                    {
                        break;
                    }
                }
            }
            if (report.Status == statusCheck[4])
            {
                foreach (var members in checkProject.Members)
                {
                    if (members.UserName == userName && members.Role == "developer" && report.Reporter == userName)
                    {
                        break;
                    }
                    else if (members.UserName == userName && members.Role == "manager" || members.Role == "tester")
                    {
                        break;
                    }
                    else if (members.UserName != userName && members.Role == "developer")
                    {
                        return new BadRequestResult();
                    }
                }
            }

            Report patchedReport = _db.PatchReport(id, report);

            return new HttpOkObjectResult(patchedReport);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            if (_db.GetReport(id) == null)
            {
                return new HttpNotFoundResult();
            }

            _db.DeleteReport(id);

            return new HttpStatusCodeResult(204);
        }

        private IList<DateTime> _checkReported(string reported)
        {
            reported = reported.ToLower();
            int date = 0;
            DateTime filterDay = DateTime.Today;
            DateTime filterDayTwo = DateTime.Today.AddDays(1);
            
            bool monthYear = false;

            //Filters out all the characters and white spaces
            string unparsedDate = Regex.Match(reported, @"\d+").Value;
            if (unparsedDate != "")
            {
                date = int.Parse(unparsedDate);
            }

            //Checks if monthNames contains a month and if the date is between a certain amount
            if (_monthNames.Any(reported.Contains) && date >= 1970 && date <= 2199)
            {
                monthYear = true;
            }

            if (reported == "today")
            {
                //Breaks the if so it won't give errors
            }
            else if (reported == "yesterday")
            {
                //Distracts 1 day to get the date of yesterday
                filterDay = filterDay.AddDays(-1);
                filterDayTwo = filterDayTwo.AddDays(-1);
            }
            else if (Regex.Match(reported, @"\d+\W+days\W+ago").Success)
            {
                //Distracts the amount of days you entered on both so you get 1 day
                filterDay = filterDay.AddDays(-date);
                filterDayTwo = filterDayTwo.AddDays(-date);
            }
            else if (Regex.Match(reported, @"last\W+\d+\W+days").Success)
            {
                //Distracts the amount of days so you can filter between 25 days ago and tomorrow
                filterDay = filterDay.AddDays(-date);
            }
            else if (monthYear) //Gets the date of a certain year
            {
                //Replaces the numbers in the string so it won't give errors
                reported = Regex.Replace(reported, @"[\d+]|\s+", string.Empty);
                filterDay = new DateTime(date, _monthNames.IndexOf(reported) + 1, 1);
                filterDayTwo = _calculateFilterDayTwo(reported, filterDayTwo);
            }
            else if (_monthNames.Contains(reported))
            {
                //If the month is below or equal to the current month, get the month of this year
                if ((_monthNames.IndexOf(reported) + 1) <= DateTime.Today.Month)
                {
                    filterDay = new DateTime(filterDay.Year, _monthNames.IndexOf(reported) + 1, 1);
                    filterDayTwo = _calculateFilterDayTwo(reported, filterDayTwo);
                }
                else
                {
                    filterDay = new DateTime(filterDay.AddYears(-1).Year, _monthNames.IndexOf(reported) + 1, 1);
                    filterDayTwo = _calculateFilterDayTwo(reported, filterDayTwo);
                }
            }
            else
            {
                filterDay = DateTime.MinValue.AddDays(1);
            }
            IList<DateTime> dateTimes = new DateTime[2] { filterDay, filterDayTwo };
            return dateTimes;
        } 

        private DateTime _calculateFilterDayTwo(string reported, DateTime filterDayTwo)
        {
            if (reported == _monthNames[11])
            {
                filterDayTwo = new DateTime(filterDayTwo.AddYears(1).Year, 1, 1);
            }
            else
            {
                filterDayTwo = new DateTime(filterDayTwo.Year, _monthNames.IndexOf(reported) + 2, 1);
            };

            return filterDayTwo;
        }
        private readonly RavenDB _db;

        private readonly IList<string> _monthNames = new string[12] { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };

        private readonly IList<string> statusCheck = new string[] { "Open", "Fixed", "Won't Fix", "Won't Fix (Approved)", "Closed" };

    }
}