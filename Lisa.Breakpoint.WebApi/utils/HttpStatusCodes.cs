﻿using Microsoft.AspNet.Mvc;

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

    public class DuplicateEntityObjectResult : ObjectResult
    {
        public DuplicateEntityObjectResult(object error) : base(error)
        {
            StatusCode = 409;
        }
    }

    public class DuplicateEntityResult : HttpStatusCodeResult
    {
        public DuplicateEntityResult() : base(409)
        {

        }
    }
}
