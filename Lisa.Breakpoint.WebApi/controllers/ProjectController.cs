using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi
{
    [Route("projects")]
    public class ProjectController : Controller
    {
        public ProjectController(RavenDB db)
        {
            _db = db;            
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

            if (projectSlug == null)
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

        [HttpPost]
        [Authorize("Bearer")]
        public IActionResult Post([FromBody]Project project)
        {
            if (project == null)
            {
                return new BadRequestResult();
            }

            project.Slug = RavenDB._toUrlSlug(project.Name);

            var postedProject = _db.PostProject(project);

            if (postedProject != null)
            {
                string location = Url.RouteUrl("project", new { organizationSlug = project.Organization, projectSlug = project.Slug }, Request.Scheme);
                return new CreatedResult(location, postedProject);
            }
            else
            {
                return new NoContentResult();
            }
        }

        [HttpPatch("{id}")]
        [Authorize("Bearer")]
        public IActionResult Patch(int id, string organization, [FromBody] Project project)
        {
            var patchedProject = _db.PatchProject(id, project);

            return new HttpOkObjectResult(patchedProject);
        }

        [HttpPatch("{organizationSlug}/{projectSlug}/members")]
        [Authorize("Bearer")]
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