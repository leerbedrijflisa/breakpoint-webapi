using Microsoft.AspNet.Mvc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Authorization;

namespace Lisa.Breakpoint.WebApi
{
    [Route("reports")]
    [Authorize("Bearer")]
    public class ReportController : BaseController
    {
        public ReportController(RavenDB db)
            :base (db)
        {
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
            var validator = new FilterValidator();

            if (Db.GetProject(organizationSlug, projectSlug, CurrentUser.Name) == null)
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

            ErrorList.FromValidator(validator.ValidateFilters(filters));

            if (ErrorList.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            reports = Db.GetAllReports(organizationSlug, projectSlug, filters);

            return new HttpOkObjectResult(reports);
        }
        
        [HttpGet("{organizationSlug}/{projectSlug}/{id}", Name = "SingleReport")]
        public IActionResult Get(string organizationSlug, string projectSlug, int id)
        {
            Report report = Db.GetReport(id);

            if (Db.GetProject(organizationSlug, projectSlug, CurrentUser.Name) == null || report == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(report);
        }

        [HttpPost("{organizationSlug}/{projectSlug}")]
        public IActionResult Post(string organizationSlug, string projectSlug, [FromBody] ReportPost report)
        {
            var validator = new ReportValidator(Db);

            if (report == null)
            {
                return new BadRequestResult();
            }

            if (!Db.ProjectExists(organizationSlug, projectSlug))
            {
                return new HttpNotFoundResult();
            }

            if (!ModelState.IsValid)
            {
                if (ErrorList.FromModelState(ModelState))
                {
                    return new UnprocessableEntityObjectResult(ErrorList.FatalErrors);
                }

                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            var parameters = new ResourceParameters
            {
                OrganizationSlug = organizationSlug,
                ProjectSlug = projectSlug,
                UserName = CurrentUser.Name
            };

            ErrorList.FromValidator(validator.ValidatePost(parameters, report));

            if (ErrorList.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            var postedReport = Db.PostReport(report, organizationSlug, projectSlug);

            string location = Url.RouteUrl("SingleReport", new { id = postedReport.Number }, Request.Scheme);
            return new CreatedResult(location, postedReport);
        }
        
        [HttpPatch("{organizationSlug}/{projectSlug}/{id}")]
        public IActionResult Patch(string organizationSlug, string projectSlug, int id, [FromBody] Patch[] patches)
        {
            var validator = new ReportValidator(Db);

            if (patches == null)
            {
                return new BadRequestResult();
            }

            var patchList = patches.Distinct().ToList();
            var patchFields = patchList.Select(p => p.Field);
            
            Report report = Db.GetReport(id);

            if (report == null)
            {
                return new HttpNotFoundResult();
            }

            var parameters = new ResourceParameters
            {
                OrganizationSlug = organizationSlug,
                ProjectSlug = projectSlug,
                ReportId = id.ToString(),
                UserName = CurrentUser.Name
            };

            ErrorList.FromValidator(validator.ValidatePatches(parameters, patches));

            if (ErrorList.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorList.Errors);
            }

            Project checkProject = Db.GetProjectByReport(id, CurrentUser.Name);

            // Check if user is in project
            if (!checkProject.Members.Select(m => m.UserName).Contains(CurrentUser.Name))
            {
                return new HttpStatusCodeResult(403);
            }

            // If the status is attempted to be patched, run permission checks
            if (patchFields.Contains("status"))
            {
                var statusPatch = patchList.Single(p => p.Field == "status");

                // If the status is patching to Won't Fix (approved), require the user to be a project manager
                if (statusPatch.Value.ToString() == Statuses.WontFixApproved && !checkProject.Members.Single(m => m.UserName.Equals(CurrentUser.Name)).Role.Equals("manager"))
                {
                    return new HttpStatusCodeResult(422);
                }

                // Is the status is patching to Closed, check if the user is either a manager, tester, or the developer who reported the problem.
                // Effectively, you're only checking whether the user is a developer, and if that's the case, if the developer has created the report.
                // It is already tested that the user is indeed part of the project, and if it's not a developer, it's implied he's either a manager or tester.
                if (statusPatch.Value.ToString() == Statuses.Closed)
                {
                    var member = checkProject.Members.Single(m => m.UserName.Equals(CurrentUser.Name));

                    checkProject.Members
                            .Single(m => m.UserName.Equals(CurrentUser.Name))
                            .Role.Equals("developer");

                    if (!report.Reporter.Equals(member.UserName))
                    {
                        return new HttpStatusCodeResult(403);
                    }
                }
            }

            Db.Patch<Report>(id, patches);
            return new HttpOkObjectResult(Db.GetReport(id));
            
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            if (Db.GetReport(id) == null)
            {
                return new HttpNotFoundResult();
            }

            Db.DeleteReport(id);

            return new HttpStatusCodeResult(204);
        }
    }
}