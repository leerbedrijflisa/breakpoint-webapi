using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Microsoft.AspNet.Mvc;
using System.Security.Principal;

namespace Lisa.Breakpoint.WebApi.controllers
{
    [Route("users")]
    public class UserController : Controller
    {
        public UserController(RavenDB db)
        {
            _db = db;
            _user = HttpContext.User.Identity;
        }

        [HttpGet("", Name = "users")]
        public IActionResult Get()
        {
            var users = _db.GetAllUsers();

            if (users == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(users);
        }

        [HttpGet("{organizationslug}/{projectSlug}/{userName}")]
        public IActionResult GetGroupFromUser(string organizationSlug, string projectSlug, string userName)
        {
            var role = _db.GetGroupFromUser(organizationSlug, projectSlug, userName);

            if (role == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(role);
        }

        [HttpPost]
        public IActionResult Post([FromBody] User user)
        {
            if (user == null)
            {
                return new BadRequestResult();

            }

            var postedUser = _db.PostUser(user);

            string location = Url.RouteUrl("users", new {  }, Request.Scheme);
            return new CreatedResult(location, postedUser);
        }

        [HttpGet("groups", Name = "groups")]
        public IActionResult GetGroups()
        {
            var groups = _db.GetAllGroups();

            if (groups == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(groups);
        }

        [HttpPost("groups")]
        public IActionResult PostGroup([FromBody] Group group)
        {
            if (group == null)
            {
                return new BadRequestResult();
            }

            var postedGroup = _db.PostGroup(group);

            string location = Url.RouteUrl("groups", new {  }, Request.Scheme);
            return new CreatedResult(location, postedGroup);
        }

        private readonly RavenDB _db;
        private readonly IIdentity _user;
    }
}
