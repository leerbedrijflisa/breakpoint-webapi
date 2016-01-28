using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi.controllers
{
    [Route("users")]
    public class UserController : BaseController
    {
        public UserController(RavenDB db)
            : base (db)
        {

        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = Db.GetAllUsers();

            return new HttpOkObjectResult(users);
        }

        [HttpGet("{userName}", Name = "SingleUser")]
        public IActionResult Get(string userName)
        {
            var user = Db.GetUser(userName);

            if (user == null)
            {
                return new HttpNotFoundResult();
            }

            return new HttpOkObjectResult(user);
        }

        [HttpGet("{organizationslug}/{projectslug}/{userName}")]
        public IActionResult GetGroupFromUser(string organizationSlug, string projectSlug, string userName)
        {
            var role = Db.GetGroupFromUser(organizationSlug, projectSlug, userName);

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
                    return new BadRequestObjectResult(ErrorHandler.FatalErrors);
                }

                return new UnprocessableEntityObjectResult(ErrorHandler.Errors);
            }

            var postedUser = Db.PostUser(user);

            if (postedUser != null)
            {
                string location = Url.RouteUrl("SingleUser", new { userName = postedUser.UserName }, Request.Scheme);
                return new CreatedResult(location, postedUser);
            }
            
            return new UnprocessableEntityResult();
        }
    };
}
