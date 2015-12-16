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
            if (projects == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(projects);
        }

        [HttpGet("{organizationSlug}/{projectSlug}/{includeAllGroups?}", Name = "project")]
        [Authorize("Bearer")]
        public IActionResult Get(string organizationSlug, string projectSlug, string includeAllGroups = "false")
        {
            _user = HttpContext.User.Identity;

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
            if (project == null || string.IsNullOrWhiteSpace(organizationSlug))
            {
                return new BadRequestResult();
            }

            if (!_db.OrganizationExists(organizationSlug))
            {
                return new HttpNotFoundResult();
            }

            if (!ModelState.IsValid)
            {
                return (ErrorHandler.FromModelState(ModelState)) ? new BadRequestObjectResult(ErrorHandler.FatalError) : new BadRequestObjectResult(ErrorHandler.Errors);
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

            var project = _db.GetProject(organizationSlug, projectSlug, _user.Name);

            if (project == null)
            {
                return new HttpNotFoundResult();
            }

            int projectNumber;
            if (!int.TryParse(project.Number, out projectNumber))
            {
                return new HttpStatusCodeResult(500);
            }

            // Patch Report to database
            try
            {
                if (_db.Patch<Organization>(projectNumber, patches))
                {
                    return new HttpOkObjectResult(_db.GetProject(organizationSlug, projectSlug, _user.Name));
                }
                else
                {
                    return new HttpStatusCodeResult(422);
                }
            }
            catch (Exception)
            {
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
        private IIdentity _user;
    }
}