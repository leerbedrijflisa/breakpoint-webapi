using Microsoft.AspNet.Mvc;

namespace Lisa.Breakpoint.WebApi
{
    public class UnprocessableEntityObjectResult : ObjectResult
    {
        public UnprocessableEntityObjectResult(object error) : base(error)
        {
            StatusCode = 422;
        }
    }

    public class UnprocessableEntityResult : HttpStatusCodeResult
    {
        public UnprocessableEntityResult() : base(422)
        {

        }
    }
}
