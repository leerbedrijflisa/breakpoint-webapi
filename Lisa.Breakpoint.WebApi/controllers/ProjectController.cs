using Microsoft.AspNet.Authorization;
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

            return new HttpOkObjectResult(projects);
        }

        [HttpGet("{organizationSlug}/{projectSlug}/{includeAllGroups?}", Name = "project")]
        // REVIEW: What does includeAllGroups do? Is it still relevant now that we have default groups?
        // REVIEWFEEDBACK: Already removed in the default groups branch.
        public IActionResult Get(string organizationSlug, string projectSlug, string includeAllGroups = "false")
        {
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
            if (!_db.OrganizationExists(organizationSlug))
            {
                return new HttpNotFoundResult();
            }

            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new BadRequestObjectResult(ErrorHandler.FatalErrors);
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

            int projectNumber = int.Parse(project.Number);
            
            if (_db.Patch<Project>(projectNumber, patches))
            {
                return new HttpOkObjectResult(_db.GetProject(organizationSlug, projectSlug, _user.Name));
            }

            // TODO: Add error message once Patch Authorization / Validation is finished.
            return new HttpStatusCodeResult(422);
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

            return new UnprocessableEntityResult();
        }

        [HttpDelete("{organizationSlug}/{projectSlug}")]
        public IActionResult Delete(string organizationSlug, string projectSlug)
        {
            if (_db.GetProject(organizationSlug, projectSlug, _user.Name) == null)
            {
                return new HttpNotFoundResult();
            }

            _db.DeleteReportsFromProjectsByOrganization(organizationSlug, projectSlug);

            _db.DeleteProject(organizationSlug, projectSlug);

            return new HttpStatusCodeResult(204);
        }

        private readonly RavenDB _db;
        private IIdentity _user { get { return HttpContext.User.Identity; } }
    }
}