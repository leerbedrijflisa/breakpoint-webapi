using Lisa.Breakpoint.WebApi.database;
using Microsoft.AspNet.Mvc;
using Lisa.Breakpoint.WebApi.Models;
using System.Collections.Generic;
using Microsoft.AspNet.Authorization;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    [Route("reports")]
    public class ReportController : Controller
    {
        public ReportController(RavenDB db)
        {
            _db = db;
            _user = HttpContext.User.Identity;
        }

        [HttpGet("{organizationSlug}/{projectSlug}/{filter?}/{value?}")]
        [Authorize("Bearer")]
        public IActionResult Get(string organizationSlug, string projectSlug, string filter = "", string value = "")
        {
            if (_db.GetProject(organizationSlug, projectSlug, _user.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            if (_db.GetUser(_user.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            IList<Report> reports;
            if (filter != "")
            {
                Filter f = new Filter(filter, value);

                reports = _db.GetAllReports(organizationSlug, projectSlug, _user.Name, f);
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

            if (report.Platform != null && report.Platform.Count == 0)
            {
                report.Platform.Add("Not specified");
            }

            _db.PostReport(report);

            string location = Url.RouteUrl("report", new { id = report.Number }, Request.Scheme);
            return new CreatedResult(location, report);
        }

        [HttpPatch("{id}")]
        [Authorize("Bearer")]
        public IActionResult Patch(int id, [FromBody] Report report)
        {
            // use statuscheck.ContainKey(report.Status) when it is put in the general value file
            if (!statusCheck.Contains(report.Status))
            {
                return new BadRequestResult();
            }

            Report checkReport = _db.GetReport(id);

            Project checkProject = _db.GetProject(checkReport.Organization, checkReport.Project, _user.Name);
            
            //If the status is Won't fix (approved) than it will check if the user is a manager, if that is not the case then return badrequestresult.
            if (report.Status == statusCheck[3])
            {
                foreach (var members in checkProject.Members)
                {
                    if (members.UserName == _user.Name && members.Role != "manager")
                    {
                        return new BadRequestResult();
                    }
                    if (members.UserName == _user.Name && members.Role == "manager")
                    {
                        break;
                    }
                }
            }
            if (report.Status == statusCheck[4])
            {
                foreach (var members in checkProject.Members)
                {
                    if (members.UserName == _user.Name && members.Role == "developer" && report.Reporter == _user.Name)
                    {
                        break;
                    }
                    else if (members.UserName == _user.Name && members.Role == "manager" || members.Role == "tester")
                    {
                        break;
                    }
                    else if (members.UserName != _user.Name && members.Role == "developer")
                    {
                        return new BadRequestResult();
                    }
                }
            }

            Report patchedReport = _db.PatchReport(id, report);

            return new HttpOkObjectResult(patchedReport);
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

        private readonly RavenDB _db;
        private readonly IIdentity _user;
        private readonly IList<string> statusCheck = new string[] { "Open", "Fixed", "Won't Fix", "Won't Fix (Approved)", "Closed" };

    }
}