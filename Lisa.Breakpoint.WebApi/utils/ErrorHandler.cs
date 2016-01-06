using Microsoft.AspNet.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lisa.Breakpoint.WebApi.utils
{
    public static class ErrorHandler
    {
        public static IEnumerable<Error> Errors
        {
            get
            {
                return _errors;
            }
        }

        public static string FatalError
        {
            get
            {
                return _fatalError;
            }
        }

        public static bool HasErrors
        {
            get { return _errors.Any(); }
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
        public static void Clear()
        {
            _errors = new List<Error>();
        }

        private static List<Error> _errors { get; set; }

        private static string _fatalError { get; set; }
    }
}
