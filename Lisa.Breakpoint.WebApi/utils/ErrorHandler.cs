using Microsoft.AspNet.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lisa.Breakpoint.WebApi
{
    public class ErrorHandler
    {
        public ErrorHandler()
        {
            _errors = new List<Error>();
        }


        public IEnumerable<Error> Errors
        {
            get
            {
                return _errors;
            }
        }

        // REVIEW: What's the purpose of FatalError? When would you use it?
        public IEnumerable<string> FatalErrors
        {
            get
            {
                return _fatalErrors;
            }
        }

        public bool HasErrors
        {
            get
            {
                return _errors.Any();
            }
        }

        public void Add(Error item)
        {
            _errors.Add(item);
        }

        public void FromValidator(IEnumerable<Error> errors)
        {
            _errors.AddRange(errors);
        }

        /// <summary>
        /// Gets and adds all modelstate errors to the error list. Fatal errors are saved in the FatalError property.
        /// </summary>
        /// <returns>True when a fatal error has occured, false otherwise.</returns>
        public bool FromModelState(ModelStateDictionary modelState)
        {
            bool fatalError = false;
            _errors = new List<Error>();
            
            foreach (var property in modelState)
            {
                foreach (var error in property.Value.Errors)
                {
                    if (error.Exception == null)
                    {
                        _errors.Add(new Error(1101, new { field = property.Key }));
                    }
                    else
                    {
                        if (Regex.IsMatch(error.Exception.Message, @"^Could not find member"))
                        {
                            _errors.Add(new Error(1103, new { field = property.Key }));
                        }
                        else
                        {
                            fatalError = true;
                            _fatalErrors.Add(JsonConvert.SerializeObject(error.Exception.Message));
                        }
                    }
                }
            }
            return (fatalError);
        }

        private List<Error> _errors;

        private List<string> _fatalErrors = new List<string>();
    }
}
