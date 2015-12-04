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

        [HttpGet("{organizationSlug}/{projectSlug}/{userName}")]
        public IActionResult Get(string organizationSlug, string projectSlug, string userName, [FromQuery] string reported = null, string filter = "", string value = "")
        {
            IList<Report> reports;

            bool filterDate = false;
            bool monthYear = false;

            DateTime filterDay = DateTime.Today;
            DateTime filterDayTwo = DateTime.Today.AddDays(1);

            IList<string> monthNames = new string[12] { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };

            if (reported != null)
            {
                int date = 0;
                if (Regex.Replace(reported, @"[^\d]|\s+", string.Empty) != "")
                {
                    date = Int32.Parse(Regex.Replace(reported, @"[^\d]", string.Empty));
                }

                reported = Regex.Replace(reported, @"[\d]|\s+", string.Empty).ToLower();

                if (monthNames.Any(reported.Contains) && date >= 1970 && date <= 2199)
                {
                    monthYear = true;
                }
                else
                {
                    reported = Regex.Replace(reported, @"[\d]|\s+", string.Empty);
                }

                if (reported == "today")
                {
                    filterDate = true;
                }
                else if (reported == "yesterday")
                {
                    filterDay = filterDay.AddDays(-1);
                    filterDayTwo = filterDayTwo.AddDays(-1);
                    filterDate = true;
                }
                else if (reported == "daysago")
                {
                    filterDay = filterDay.AddDays(-date);
                    filterDayTwo = filterDayTwo.AddDays(-date);
                    filterDate = true;
                }
                else if (reported == "lastdays")
                {
                    filterDay = filterDay.AddDays(-date);
                    filterDate = true;
                }
                else if (monthYear)
                {
                    filterDay = new DateTime(date, monthNames.IndexOf(reported) + 1, 1);
                    filterDayTwo = new DateTime(date, monthNames.IndexOf(reported) + 2, 1);
                    filterDate = true;
                }
                else if (monthNames.Contains(reported))
                {
                    if ((monthNames.IndexOf(reported) + 1) <= DateTime.Today.Month)
                    {
                        filterDay = new DateTime(filterDay.Year, monthNames.IndexOf(reported) + 1, 1);
                        filterDayTwo = new DateTime(filterDay.Year, monthNames.IndexOf(reported) + 2, 1);
                    }
                    else
                    {
                        filterDay = new DateTime(filterDay.AddYears(-1).Year, monthNames.IndexOf(reported) + 1, 1);
                        filterDayTwo = new DateTime(filterDay.Year, monthNames.IndexOf(reported) + 2, 1);
                    }
                    filterDate = true;
                }
            }

            //die dingen van bas
            if (_db.GetProject(organizationSlug, projectSlug, userName) == null)
            {
                return new HttpNotFoundResult();
            }

            if (_db.GetUser(userName) == null)
            {
                return new HttpNotFoundResult();
            }
            
            if (filter != "" || filterDate )
            {
                DateTime[] dateTimes = new DateTime[2];

                Filter f = null;

                if (filterDate)
                {
                    dateTimes[0] = filterDay;
                    dateTimes[1] = filterDayTwo;
                }

                if (filter != "")
                {
                    f = new Filter(filter, value);
                }

                reports = _db.GetAllReports(organizationSlug, projectSlug, userName, dateTimes, f);
            } else { 
                reports = _db.GetAllReports(organizationSlug, projectSlug, userName);
            }

            // einde die dingen van bas
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

        private readonly RavenDB _db;

        private readonly IList<string> statusCheck = new string[] { "Open", "Fixed", "Won't Fix", "Won't Fix (Approved)", "Closed" };

    }
}