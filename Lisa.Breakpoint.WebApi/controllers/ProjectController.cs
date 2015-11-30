using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi
{
    [Route("projects")]
    public class ProjectController : Controller
    {
        public ProjectController(RavenDB db)
        {
            _db = db;
        }

        [HttpGet("{organizationSlug}/{userName}")]
        public IActionResult GetAll(string organizationSlug, string userName)
        {
            if (_db.GetOrganization(organizationSlug) == null)
            {
                return new HttpNotFoundResult();
            }

            if (_db.GetUser(userName) == null)
            {
                return new HttpNotFoundResult();
            }

            var projects = _db.GetAllProjects(organizationSlug, userName);
            if (projects == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(projects);
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

        [HttpPatch("{id}")]
        public IActionResult Patch(int id, string organization, Project project)
        {
            var patchedProject = _db.PatchProject(id, project);

            return new HttpOkObjectResult(patchedProject);
        }

        [HttpPatch("{organizationSlug}/{projectSlug}/members")]
        public IActionResult PatchMembers(string organizationSlug, string projectSlug, [FromBody] Patch patch)
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