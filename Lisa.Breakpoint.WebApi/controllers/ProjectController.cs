using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Lisa.Breakpoint.WebApi.utils;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    [Route("projects")]
    public class ProjectController : Controller
    {
        public ProjectController(RavenDB db)
        {
            _db = db;
            ErrorHandler.Clear();
        }

        [HttpGet("{organizationSlug}")]
        [Authorize("Bearer")]
        public IActionResult GetAll(string organizationSlug)
        {
            _user = HttpContext.User.Identity;

            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            var projects = _db.GetAllProjects(organizationSlug, _user.Name);
            // REVIEW: Does GetAllProjects() ever return null? Doesn't it just return an empty list? What would a return value of null mean?
            if (projects == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(projects);
        }

        [HttpGet("{organizationSlug}/{projectSlug}/{includeAllGroups?}", Name = "project")]
        [Authorize("Bearer")]
        // REVIEW: What does includeAllGroups do? Is it still relevant now that we have default groups?
        // REVIEWFEEDBACK: Already removed in the default groups branch.
        public IActionResult Get(string organizationSlug, string projectSlug, string includeAllGroups = "false")
        {
            _user = HttpContext.User.Identity;

            // REVIEW: Is it necessary to check for this explicitly? Doesn't GetProject return null in these cases?
            // REVIEWFEEDBACK: Won't this be a bad request instead of not found?
            if (string.IsNullOrWhiteSpace(organizationSlug) || string.IsNullOrWhiteSpace(projectSlug))
            {
                return new HttpNotFoundResult();
            }

            var project = _db.GetProject(organizationSlug, projectSlug, _user.Name, includeAllGroups);

            if (project == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(project);
        }

        [HttpPost("{organizationSlug}")]
        [Authorize("Bearer")]
        public IActionResult Post(string organizationSlug, [FromBody] ProjectPost project)
        {
            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new BadRequestObjectResult(ErrorHandler.FatalError);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            if (project == null || string.IsNullOrWhiteSpace(organizationSlug))
            {
                // REVIEW: Shouldn't this be a 404 for the IsNullOrWhiteSpace case? Doesn't OrganizationExists (line 85) take care of that check?
                // REVIEWFEEDBACK: Shouldn't a missing required parameter trigger a bad request?
                return new BadRequestResult();
            }

            if (!_db.OrganizationExists(organizationSlug))
            {
                return new HttpNotFoundResult();
            }

            var postedProject = _db.PostProject(project, organizationSlug);

            if (ErrorHandler.HasErrors)
            {
                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            string location = Url.RouteUrl("project", new { organizationSlug = postedProject.Organization, projectSlug = postedProject.Slug }, Request.Scheme);
            return new CreatedResult(location, postedProject);
        }

        [HttpPatch("{organizationSlug}/{projectSlug}")]
        [Authorize("Bearer")]
        public IActionResult Patch(string organizationSlug, string projectSlug, [FromBody] IEnumerable<Patch> patches)
        {
            if (patches == null)
            {
                return new BadRequestResult();
            }

            // TODO: Initialize _user before using it.
            var project = _db.GetProject(organizationSlug, projectSlug, _user.Name);

            if (project == null)
            {
                return new HttpNotFoundResult();
            }

            int projectNumber;
            if (!int.TryParse(project.Number, out projectNumber))
            {
                // TODO: Return a 422 with an error message.
                // REVIEWFEEDBACK: This error gets triggered when the automatically entered field 'number' is not a number in the DB. Should this be a 422 when there is an issue in automatic database values?
                return new HttpStatusCodeResult(500);
            }

            // Patch Report to database
            try
            {
                // REVIEW: Shouldn't this be _db.Patch<Project>? Why patch the organization?
                // REVIEWFEEDBACK: Known and fixed, not merged.
                if (_db.Patch<Organization>(projectNumber, patches))
                {
                    return new HttpOkObjectResult(_db.GetProject(organizationSlug, projectSlug, _user.Name));
                }
                else
                {
                    // TODO: Add error message.
                    return new HttpStatusCodeResult(422);
                }
            }
            catch (Exception)
            {
                // REVIEW: Isn't this what ASP.NET does automatically if you don't catch the exception?
                // Internal server error if RavenDB throws exceptions
                return new HttpStatusCodeResult(500);
            }
        }

        
        [HttpPatch("{organizationSlug}/{projectSlug}/members")]
        [Authorize("Bearer")]
        public IActionResult PatchMembers(string organizationSlug, string projectSlug, [FromBody] TempMemberPatch patch)
        {
            if (organizationSlug == null || projectSlug == null || patch == null)
            {
                return new BadRequestResult();
            }

            var patchedProjectMembers = _db.PatchProjectMembers(organizationSlug, projectSlug, patch);

            if (patchedProjectMembers != null)
            {
                string location = Url.RouteUrl("project", new { organizationSlug = organizationSlug, projectSlug = projectSlug, userName = patch.Sender }, Request.Scheme);
                return new CreatedResult(location, patchedProjectMembers);
            }
            else
            {
                // TODO: Return the correct status code. Depending on what went wrong, it could be a 401 or a 422.
                return new NoContentResult();
            }
        }

        [HttpDelete("{organizationSlug}/{project}/")]
        [Authorize("Bearer")]
        public IActionResult Delete(string organizationSlug, string project)
        {
            _user = HttpContext.User.Identity;

            if (_db.GetProject(organizationSlug, project, _user.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            _db.DeleteProject(project);

            return new HttpStatusCodeResult(204);
        }

        private readonly RavenDB _db;
        // REVIEW: Why is this an instance variable if every method initializes it separately? Either make it a local variable of each method or initialize the instance variable in a central spot.
        private IIdentity _user;
    }
}