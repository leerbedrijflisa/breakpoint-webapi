﻿using Microsoft.AspNet.Mvc;
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
            if (user == null)
            {
                return new BadRequestResult();
            }

            if (!ModelState.IsValid)
            {
                if (ErrorHandler.FromModelState(ModelState))
                {
                    return new BadRequestObjectResult(ErrorHandler.FatalError);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            var postedUser = _db.PostUser(user);

            if (postedUser != null)
            {
                string location = Url.RouteUrl("users", new { }, Request.Scheme);
                return new CreatedResult(location, postedUser);
            }
            
            return new UnprocessableEntityResult();
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

            if (postedGroup != null)
            {
                string location = Url.RouteUrl("groups", new { }, Request.Scheme);
                return new CreatedResult(location, postedGroup);
            }
            else
            {
                return new UnprocessableEntityResult();
            }
        }

        private readonly RavenDB _db;
        private IIdentity _user { get { return HttpContext.User.Identity; } }
    };
}
