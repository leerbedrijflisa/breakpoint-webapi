﻿using Microsoft.AspNet.Mvc;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNet.Authorization;
using System.Security.Principal;
using Microsoft.AspNet.Http;

namespace Lisa.Breakpoint.WebApi
{
    [Route("reports")]
    [Authorize("Bearer")]
    public class ReportController : Controller
    {
        public ReportController(RavenDB db)
        {
            _db = db;
            ErrorHandler.Clear();
        }

        [HttpGet("{organizationSlug}/{projectSlug}/")]
        public IActionResult Get(string organizationSlug, string projectSlug,
            [FromQuery] string title, 
            [FromQuery] string reporter, 
            [FromQuery] string reported, 
            [FromQuery] string status, 
            [FromQuery] string priority, 
            [FromQuery] string version,
            [FromQuery] string assignedTo)
        {

            if (_db.GetProject(organizationSlug, projectSlug, _user.Name) == null)
            {
                return new HttpNotFoundResult();
            }
            
            IEnumerable<Report> reports;
            var filters = new List<Filter>();

            // Add all filters (yeah it's a lot)
            if (!string.IsNullOrWhiteSpace(title))
            {
                filters.Add(new Filter(FilterTypes.Title, title));
            }
            if (!string.IsNullOrWhiteSpace(reporter))
            {
                filters.Add(new Filter(FilterTypes.Reporter, reporter));
            }
            if (!string.IsNullOrWhiteSpace(reported))
            {
                filters.Add(new Filter(FilterTypes.Reported, reported));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                filters.Add(new Filter(FilterTypes.Status, status));
            }
            if (!string.IsNullOrWhiteSpace(priority))
            {
                filters.Add(new Filter(FilterTypes.Priority, priority));
            }
            if (!string.IsNullOrWhiteSpace(version))
            {
                filters.Add(new Filter(FilterTypes.Version, version));
            }
            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                filters.Add(new Filter(FilterTypes.AssignedTo, assignedTo));
            }
            
            reports = _db.GetAllReports(organizationSlug, projectSlug, filters);
            
            if (ErrorHandler.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
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

        [HttpPost("{organizationSlug}/{projectSlug}")]
        public IActionResult Post(string organizationSlug, string projectSlug, [FromBody] ReportPost report)
        {
            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new UnprocessableEntityObjectResult(ErrorHandler.FatalErrors);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            if (report == null)
            {
                return new BadRequestResult();
            }

            if (!_db.ProjectExists(organizationSlug, projectSlug))
            {
                return new HttpNotFoundResult();
            }

            IList<Member> projectMembers = _db.GetProject(organizationSlug, projectSlug, _user.Name).Members;
            if (!projectMembers.AsQueryable().Select(m => m.Username).Contains(_user.Name))
            {
                return new HttpStatusCodeResult(403);
            }

            var postedReport = _db.PostReport(report, organizationSlug, projectSlug);

            if (ErrorHandler.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            string location = Url.RouteUrl("report", new { id = postedReport.Number }, Request.Scheme);
            return new CreatedResult(location, postedReport);
        }
            
        [HttpPatch("{id}")]
        public IActionResult Patch(int id, [FromBody] Patch[] patches)
        {
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
            if (!checkProject.Members.Select(m => m.Username).Contains(_user.Name))
            {
                // Forbidden
                return new HttpStatusCodeResult(403);
            }

            // If the status is attempted to be patched, run permission checks
            if (patchFields.Contains("status"))
            {
                var statusPatch = patchList.Single(p => p.Field == "status");

                if (!Statuses.List.Contains(statusPatch.Value))
                {
                    return new BadRequestResult();
                }

                // If the status is patching to Won't Fix (approved), require the user to be a project manager
                if (statusPatch.Value.ToString() == Statuses.WontFixApproved && !checkProject.Members.Single(m => m.Username.Equals(_user.Name)).Role.Equals("manager"))
                {
                    // 422 Unprocessable Entity : The request was well-formed but was unable to be followed due to semantic errors
                    return new HttpStatusCodeResult(422);
                }

                // Is the status is patching to Closed, check if the user is either a manager, tester, or the developer who reported the problem.
                // Effectively, you're only checking whether the user is a developer, and if that's the case, if the developer has created the report.
                // It is already tested that the user is indeed part of the project, and if it's not a developer, it's implied he's either a manager or tester.
                if (statusPatch.Value.ToString() == Statuses.Closed)
                {
                    var member = checkProject.Members.Single(m => m.Username.Equals(_user.Name));

                    checkProject.Members
                            .Single(m => m.Username.Equals(_user.Name))
                            .Role.Equals("developer");

                    if (!report.Reporter.Equals(member.Username))
                    {
                        // Not authenticated
                        return new HttpStatusCodeResult(401);
                    }
                }
            }

            // Do not patch the date it was reported
            if (patchFields.Contains("Reported"))
            {
                ErrorHandler.Add(new Error(1205, new { field = "Reported" }));
                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }
            
            // Patch Report to database
            if (_db.Patch<Report>(id, patches))
            {
                return new HttpOkObjectResult(_db.GetReport(id));
            }

            // TODO: Add error message once Patch Authorization / Validation is finished.
            return new HttpStatusCodeResult(422);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            Report report = _db.GetReport(id);

            if (report == null)
            {
                return new HttpNotFoundResult();
            }

            Project checkProject = _db.GetProjectByReport(id, _user.Name);

            // Check if user is in project
            if (!checkProject.Members.Select(m => m.Username).Contains(_user.Name))
            {
                // Forbidden
                return new HttpStatusCodeResult(403);
            }

            _db.DeleteReport(id);

            return new HttpStatusCodeResult(204);
        }

        private readonly RavenDB _db;
        private IIdentity _user { get { return HttpContext.User.Identity; } }
    }
}