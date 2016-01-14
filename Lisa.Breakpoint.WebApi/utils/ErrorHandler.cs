using Microsoft.AspNet.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lisa.Breakpoint.WebApi.utils
{
    // TODO: Make the class non-static
    // REVIEWFEEDBACK: Needs to be discussed thoroughly, making it non-static requires either passing it to all DB methods, making the DB return response objects, or making the DB return dynamics (either an error list or a result).
    public static class ErrorHandler
    {
        public static IEnumerable<Error> Errors
        {
            get
            {
                return _errors;
            }
        }

        // REVIEW: What's the purpose of FatalError? When would you use it?
        public static string FatalError
        {
            get
            {
                return _fatalError;
            }
        }

        public static bool HasErrors
        {
            get
            {
                return _errors.Any();
            }
        }

        public static void Add(Error item)
        {
            _errors.Add(item);
        }

        /// <summary>
        /// Gets and adds all modelstate errors to the error list. Fatal errors are saved in the FatalError property.
        /// </summary>
        /// <returns>True when a fatal error has occured, false otherwise.</returns>
        public static bool FromModelState(ModelStateDictionary modelState)
        {
            bool fatalError = false;
            _errors = new List<Error>();
            string fatalErrorMessage = string.Empty;

            // REVIEW: What's the purpose of Select(m => m)? Doesn't that effectively do nothing?
            // REVIEW: Is it necessary to filter the model state errors? Doesn't the second foreach loop below take care of that?
            var modelStateErrors = modelState.Select(m => m).Where(x => x.Value.Errors.Count > 0);
            foreach (var property in modelStateErrors)
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
                            // REVIEW: What if there are multiple fatal errors? Shouldn't _fatalError be a list?
                            fatalError = true;
                            _fatalError = JsonConvert.SerializeObject(error.Exception.Message);
                        }
                    }
                }
            }
            return (fatalError);
        }

        /// <summary>
        /// Clears the error list.
        /// </summary>
        // REVIEW: Is this still necessary if the class is non-static?
        // REVIEWFEEDBACK: No.
        public static void Clear()
        {
            _errors = new List<Error>();
        }

        private static List<Error> _errors { get; set; }

        private static string _fatalError { get; set; }
    }
}
