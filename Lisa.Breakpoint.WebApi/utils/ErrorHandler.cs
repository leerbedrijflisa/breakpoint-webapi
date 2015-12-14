﻿using Microsoft.AspNet.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

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
        /// Gets and adds all modelstate errors to the error list. Fatal errors are saved in the FatalError property
        /// </summary>
        /// <returns>True when a fatal error has occured, false otherwise.</returns>
        public static bool FromModelState(ModelStateDictionary modelState)
        {
            bool fatalError = false;
            _errors = new List<Error>();
            string fatalErrorMessage = string.Empty;
            var modelStateErrors = modelState.Select(M => M).Where(X => X.Value.Errors.Count > 0);
            foreach (var property in modelStateErrors)
            {
                var propertyName = property.Key;
                foreach (var error in property.Value.Errors)
                {
                    if (error.Exception == null)
                    {
                        _errors.Add(new Error(1101, new { field = propertyName }));
                    }
                    else
                    {
                        fatalError = true;
                        _fatalError = JsonConvert.SerializeObject(error.Exception.Message);
                    }
                }
            }
            return (fatalError);
        }

        /// <summary>
        /// Clears the error list
        /// </summary>
        public static void Clear()
        {
            _errors = new List<Error>();
        }

        private static List<Error> _errors { get; set; }

        private static string _fatalError { get; set; }
    }
}
