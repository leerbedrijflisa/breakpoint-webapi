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
        public IActionResult Get(string organizationSlug, string projectSlug, string userName, [FromQuery] string reported, string filter = "", string value = "")
        {
            
            IList<Report> reports;

            DateTime filterDay = DateTime.Today;

            IList<string> monthNames = new string[12] { "januari", "februari", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
            int date = 0;
            if (Regex.Replace(reported, @"[^\d]|\s+", string.Empty) != "")
            {
                date = Int32.Parse(Regex.Replace(reported, @"[^\d]", string.Empty));
            }

            reported = Regex.Replace(reported, @"[\d]|\s+", string.Empty).ToLower();

            bool monthYear = false;

            if (monthNames.Any(reported.Contains) && date >= 1970 && date <= 2199)
            {
                monthYear = true;
                System.Diagnostics.Debug.WriteLine("check year");
            }
            else
            {
                reported = Regex.Replace(reported, @"[\d]|\s+", string.Empty);
            }

            if (reported == "today")
            {

            }
            else if (reported == "yesterday")
            {
                filterDay.AddDays(-1);
                System.Diagnostics.Debug.WriteLine(filterDay);
            }
            else if (reported == "daysago")
            {
                filterDay.AddDays(-date);
                System.Diagnostics.Debug.WriteLine(filterDay);
            }
            else if (reported == "lastdays")
            {
                //tussen morge en de dagen dat ingevuld is
                System.Diagnostics.Debug.WriteLine(filterDay);
            }
            else if (monthYear)
            {
                System.Diagnostics.Debug.WriteLine(monthNames.IndexOf(reported) + 1);
                System.Diagnostics.Debug.WriteLine(DateTime.Today.Month);

                if ((monthNames.IndexOf(reported) + 1) <= DateTime.Today.Month)
                {
                    date = Int32.Parse((monthNames.IndexOf(reported) + 1).ToString("00"));
                    var tempyear = DateTime.Today.Year;
                    var meep = date.ToString("00") + ' ' + tempyear;
                    var meep2 = Int32.Parse(meep);
                    System.Diagnostics.Debug.WriteLine(date);
                    System.Diagnostics.Debug.WriteLine(tempyear);
                }
                else
                {

                }
                System.Diagnostics.Debug.WriteLine(filterDay);
            }
            else if (monthNames.Contains(reported))
            {

            }


            //die shit van bas
            if (_db.GetProject(organizationSlug, projectSlug, userName) == null)
            {
                return new HttpNotFoundResult();
            }

            if (_db.GetUser(userName) == null)
            {
                return new HttpNotFoundResult();
            }

            
            if (filter != "")
            {
                Filter f = new Filter(filter, value);

                reports = _db.GetAllReports(organizationSlug, projectSlug, userName, f);
            } else { 
                reports = _db.GetAllReports(organizationSlug, projectSlug, userName);
            }

            // einde die shit van bas
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

            if (report.Platform.Count == 0)
            {
                report.Platform.Add("Not specified");
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