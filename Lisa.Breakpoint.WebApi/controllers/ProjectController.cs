﻿using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System.Collections.Generic;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    [Route("projects")]
    [Authorize("Bearer")]
    public class ProjectController : Controller
    {
        public ProjectController(RavenDB db)
        {
            _db = db;
            _user = HttpContext.User.Identity;
            ErrorHandler.Clear();
        }

        [HttpGet("{organizationSlug}")]
        public IActionResult GetAll(string organizationSlug)
        {
            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            var projects = _db.GetAllProjects(organizationSlug);
            // REVIEW: Does GetAllProjects() ever return null? Doesn't it just return an empty list? What would a return value of null mean?
            if (projects == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(projects);
        }

        [HttpGet("{organizationSlug}/{projectSlug}/{includeAllGroups?}", Name = "project")]
        public IActionResult Get(string organizationSlug, string projectSlug, string includeAllGroups = "false")
        {
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
        public IActionResult Post(string organizationSlug, [FromBody] ProjectPost project)
        {
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

            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new BadRequestObjectResult(ErrorHandler.FatalError);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
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
                // TODO: Return a 422 with an error message.
                // REVIEWFEEDBACK: This error gets triggered when the automatically entered field 'number' is not a number in the DB. Should this be a 422 when there is an issue in automatic database values?
                return new HttpStatusCodeResult(500);
            }
            
            if (_db.Patch<Project>(projectNumber, patches))
            {
                return new HttpOkObjectResult(_db.GetProject(organizationSlug, projectSlug, _user.Name));
            }
            else
            {
                // TODO: Add error message.
                // REVIEWFEEDBACK: Waiting for proper patch authorization / validation
                return new HttpStatusCodeResult(422);
            }
        }

        
        [HttpPatch("{organizationSlug}/{projectSlug}/members")]
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

            // TODO: Return the correct status code. Depending on what went wrong, it could be a 401 or a 422.
            // Will be removed in the future in favor of regular patches.
            return new UnprocessableEntityResult();
        }

        [HttpDelete("{organizationSlug}/{project}/")]
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