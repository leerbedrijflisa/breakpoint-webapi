﻿namespace Lisa.Breakpoint.WebApi
{
    public class Error
    {
        public Error(int code, object values = null)
        {
            Code = code;
            Message = ErrorMessages.Get(code, values);
            if (values != null)
            {
                Values = values;
            }
        }

        public int Code { get; set; }
        public string Message { get; set; }
        public object Values { get; set; }
    }
}