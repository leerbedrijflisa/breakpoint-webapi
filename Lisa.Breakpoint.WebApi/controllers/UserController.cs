using Lisa.Breakpoint.WebApi.database;
using Lisa.Breakpoint.WebApi.Models;
using Lisa.Breakpoint.WebApi.utils;
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

        [HttpGet("{organizationslug}/{projectslug}/{userName}")]
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
        public IActionResult Post([FromBody] UserPost user)
        {
            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new BadRequestObjectResult(ErrorHandler.FatalError);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            if (user == null)
            {
                return new BadRequestResult();
            }

            var postedUser = _db.PostUser(user);

            if (postedUser != null)
            {
                string location = Url.RouteUrl("users", new { }, Request.Scheme);
                return new CreatedResult(location, postedUser);
            }

            return new DuplicateEntityResult();
        }

        private readonly RavenDB _db;
        private IIdentity _user;
    }
}
