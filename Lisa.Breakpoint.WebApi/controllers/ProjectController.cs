using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;
using System;
using System.Collections.Generic;

namespace Lisa.Breakpoint.WebApi
{
    [Route("projects")]
    public class ProjectController : Controller
    {
        public ProjectController(RavenDB db)
        {
            _db = db;
        }

        [HttpGet("{organization}/{username}")]
        public IActionResult GetAll(string organization, string userName)
        {
            if (_db.GetOrganization(organization) == null)
            {
                return new HttpNotFoundResult();
            }

            if (_db.GetUser(userName) == null)
            {
                return new HttpNotFoundResult();
            }

            var organizations = _db.GetAllProjects(organization, userName);
            if (organizations == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(organizations);
        }

        [HttpGet("{organizationSlug}/{projectSlug}/{userName}/{includeAllGroups?}", Name = "project")]
        public IActionResult Get(string organizationSlug, string projectSlug, string userName, string includeAllGroups = "false")
        {
            if (projectSlug == null || userName == null)
            {
                return new HttpNotFoundResult();
            }

            var project = _db.GetProject(organizationSlug, projectSlug, userName, includeAllGroups);

            if (project == null)
            {
                return new HttpNotFoundResult();
            }
            return new HttpOkObjectResult(project);
        }

        [HttpPost("{userName}")]
        public IActionResult Post([FromBody]Project project, string userName)
        {
            if (project == null)
            {
                return new BadRequestResult();
            }

            var postedProject = _db.PostProject(project);

            if (postedProject != null)
            {
                string location = Url.RouteUrl("project", new { organizationSlug = project.Organization, projectSlug = project.Slug, userName = userName }, Request.Scheme);
                return new CreatedResult(location, postedProject);
            } else
            {
                return new NoContentResult();
            }
        }

        [HttpPatch("{organizationSlug}/{projectSlug}")]
        public IActionResult Patch(string organizationSlug, string projectSlug, IEnumerable<Patch> patches)
        {
            //var project = _db.GetProject(organizationSlug, projectSlug);
            var project = new Project();

            if (project == null || patches == null)
            {
                return new HttpNotFoundResult();
            }

            // 422 Unprocessable Entity : The request was well-formed but was unable to be followed due to semantic errors
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
                    //return new HttpOkObjectResult(_db.GetProject(projectSlug));
                    return new HttpOkObjectResult(new Project());
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

        [HttpPatch("{organization}/{projectSlug}/members")]
        public IActionResult PatchMembers(string organization, string projectSlug, [FromBody] TempMemberPatch patch)
        {
            if (organization == null || projectSlug == null || patch == null)
            {
                return new BadRequestResult();
            }

            var patchedProjectMembers = _db.PatchProjectMembers(organization, projectSlug, patch);

            if (patchedProjectMembers != null)
            {
                string location = Url.RouteUrl("project", new { organizationSlug = organization, projectSlug = projectSlug, userName = patch.Sender }, Request.Scheme);
                return new CreatedResult(location, patchedProjectMembers);
            }
            else
            {
                return new NoContentResult();
            }
        }

        [HttpDelete("{organization}/{project}/{userName}")]
        public IActionResult Delete(string organizationSlug, string project, string userName)
        {
            if (_db.GetProject(organizationSlug, project, userName) == null)
            {
                return new HttpNotFoundResult();
            }

            _db.DeleteProject(project);

            return new HttpStatusCodeResult(204);
        }

        private readonly RavenDB _db;
    }
}