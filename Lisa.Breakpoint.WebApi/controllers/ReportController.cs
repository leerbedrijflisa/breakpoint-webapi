using Lisa.Breakpoint.WebApi.database;
using Microsoft.AspNet.Mvc;
using Lisa.Breakpoint.WebApi.Models;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNet.Authorization;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Lisa.Breakpoint.WebApi
{
    [Route("reports")]
    public class ReportController : Controller
    {
        public ReportController(RavenDB db)
        {
            _db = db;
        }


        [HttpGet("{organizationSlug}/{projectSlug}/{filter?}/{value?}")]
        [Authorize("Bearer")]
        public IActionResult Get(string organizationSlug, string projectSlug, string filter = "", string value = "", [FromQuery] string reported = null)
        {
            _user = HttpContext.User.Identity;
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
            if (_db.GetUser(_user.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            //if project isn't found it'll return an error 404
            if (_db.GetProject(organizationSlug, projectSlug, _user.Name) == null)
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

                reports = _db.GetAllReports(organizationSlug, projectSlug, _user.Name, dateTimeObject, f);
            } else { 
                reports = _db.GetAllReports(organizationSlug, projectSlug, _user.Name);
            }
            
            if (reports == null)
            {
                return new HttpNotFoundResult();
            }
            return new HttpOkObjectResult(reports);
        }

        [HttpGet("{id}", Name = "report")]
        [Authorize("Bearer")]
        public IActionResult Get(int id)
        {
            Report report = _db.GetReport(id);

            if (report == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(report);
        }

        [HttpPost("{organizationSlug}/{projectSlug}")]
        [Authorize("Bearer")]
        public IActionResult Post(string organizationSlug, string projectSlug, [FromBody] Report report)
        {
            if (report == null)
            {
                return new BadRequestResult();
            }

            if (report.Platforms != null && report.Platforms.Count == 0)
            {
                report.Platforms.Add("Not specified");
            }

            // Set organization and project slug's 
            report.Organization = organizationSlug;
            report.Project = projectSlug;

            _db.PostReport(report);

            string location = Url.RouteUrl("report", new { id = report.Number }, Request.Scheme);
            return new CreatedResult(location, report);
        }
            
        [HttpPatch("{id}")]
        [Authorize("Bearer")]
        public IActionResult Patch(int id, [FromBody] Patch[] patches)
        {
            _user = HttpContext.User.Identity;

            // use statuscheck.ContainKey(report.Status) when it is put in the general value file
            if (patches == null)
            {
                return new BadRequestResult();
            }

            var patchList = patches.Distinct().ToList();
            var patchFields = patchList.Select(p => p.Field);
            
            Report report = _db.GetReport(id);

            if (report == null)
            {
                return new HttpNotFoundResult();
            }

            Project checkProject = _db.GetProjectByReport(id, _user.Name);

            // Check if user is in project
            if (!checkProject.Members.Select(m => m.UserName).Contains(_user.Name))
            {
                // Not authenticated
                return new HttpStatusCodeResult(401);
            }

            // If the status is attempted to be patched, run permission checks
            if (patchFields.Contains("status"))
            {
                var statusPatch = patchList.Single(p => p.Field == "status");

                if (!statusCheck.Contains(statusPatch.Value))
                {
                    return new BadRequestResult();
                }

                // If the status is patching to Won't Fix (approved), require the user to be a project manager
                if (statusPatch.Value.Equals(statusCheck[3]) && !checkProject.Members.Single(m => m.UserName.Equals(_user.Name)).Role.Equals("manager"))
                {
                    // 422 Unprocessable Entity : The request was well-formed but was unable to be followed due to semantic errors
                    return new HttpStatusCodeResult(422);
                }

                // Is the status is patching to Closed, check if the user is either a manager, tester, or the developer who reported the problem.
                // Effectively, you're only checking whether the user is a developer, and if that's the case, if the developer has created the report.
                // It is already tested that the user is indeed part of the project, and if it's not a developer, it's implied he's either a manager or tester.
                if (statusPatch.Value.Equals(statusCheck[4]))
                {
                    var member = checkProject.Members.Single(m => m.UserName.Equals(_user.Name));

                    checkProject.Members
                            .Single(m => m.UserName.Equals(_user.Name))
                            .Role.Equals("developer");

                    if (!report.Reporter.Equals(member.UserName))
                    {
                        // Not authenticated
                        return new HttpStatusCodeResult(401);
                    }
                }
            }

            // Do not patch the date it was reported
            if (patchFields.Contains("Reported"))
            {
                patchList.Remove(patchList.Single(p => p.Field.Equals("Reported")));
            }
            
            // Patch Report to database
            try
            {
                // 422 Unprocessable Entity : The request was well-formed but was unable to be followed due to semantic errors
                if (_db.Patch<Report>(id, patches))
                {
                    return new HttpOkObjectResult(_db.GetReport(id));
                }
                else
                {
                    return new HttpStatusCodeResult(422);
                }
            }
            catch(Exception)
            {
                // Internal server error if RavenDB throws exceptions
                return new HttpStatusCodeResult(500);
            }
        }

        [HttpDelete("{id}")]
        [Authorize("Bearer")]
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
        private IIdentity _user;


        private readonly IList<string> _monthNames = new string[12] { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };

        private readonly IList<string> statusCheck = new string[5] { "Open", "Fixed", "Won't Fix", "Won't Fix (Approved)", "Closed" };
        
    }
}