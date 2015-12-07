﻿using Lisa.Breakpoint.WebApi.database;
using Microsoft.AspNet.Mvc;
using Lisa.Breakpoint.WebApi.Models;
using System.Collections.Generic;
using System;
using System.Linq;
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

        [HttpPost("{organizationslug}/{projectslug}")]
        [Authorize("Bearer")]
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
            
        [HttpPatch("{id}")]
        [Authorize("Bearer")]
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
            if (!checkProject.Members.Select(m => m.UserName).Contains(_user.Name))
            {
                // Not authenticated
                return new HttpStatusCodeResult(401);
            }

            // If the status is attmpted to be patched, run permission checks
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

        private readonly RavenDB _db;
        private readonly IIdentity _user;
        private readonly IList<string> statusCheck = new string[] { "Open", "Fixed", "Won't Fix", "Won't Fix (Approved)", "Closed" };
    }
}