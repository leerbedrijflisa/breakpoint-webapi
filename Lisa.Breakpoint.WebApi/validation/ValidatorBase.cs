using Newtonsoft.Json.Linq;
using Raven.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lisa.Breakpoint.WebApi
{
    abstract class ValidatorBase<T>
    {
        protected ValidatorBase(RavenDB db)
        {
            Db = db;
        }

        public abstract IEnumerable<Error> ValidatePatches(ResourceParameters resource, IEnumerable<Patch> patch);

        public abstract IEnumerable<Error> ValidatePost(ResourceParameters resource, T post);

        /// <summary>
        /// Validates a Patch object or add errors when the patch in not allowed to be processed.
        /// </summary>
        /// <param name="patch">The Patch object to be validated.</param>
        /// <param name="action">The action which needs to be validated for. Either add, replace or remove.</param>
        /// <param name="regex">The regex expression to validate the field name with.</param>
        /// <param name="validateValue">A list of functions to validate the value that's being patched in. Example usage is for constrained values.</param>
        /// <param name="validateField">A list of functions to validate the field that's being patched. Example usage is for checking whether the document being patched actually exists.</param>
        /// <returns></returns>
        protected IEnumerable<Error> Allow<T>(Patch patch, string action, Regex regex, IEnumerable<Action<T, object>> validateValue, IEnumerable<Action<dynamic>> validateField)
        {
            var match = regex.Match(patch.Field.ToLower());
            if (match.Success)
            {
                if (patch.Action.ToLower() == action)
                {
                    if (patch.Value == null)
                    {
                        Errors.Add(new Error(1101, new { Field = "Value" }));
                    }

                    var fieldParams = new Dictionary<string, object>();
                    foreach (string groupName in regex.GetGroupNames())
                    {
                        fieldParams.Add(groupName, match.Groups[groupName].Value);
                    }
                    fieldParams.Add("Field", patch.Field);
                    fieldParams.Add("Value", patch.Value?.ToString());

                    if (!fieldParams.ContainsKey("Id"))
                    {
                        fieldParams.Add("Id", patch.Value.ToString());
                    }

                    if (validateField != null)
                    {
                        foreach (var func in validateField)
                        {
                            func(fieldParams);
                        }
                    }

                    if (validateValue != null)
                    {
                        foreach (var func in validateValue)
                        {
                            try
                            {
                                Type type = typeof(T);
                                var value = JToken.FromObject(patch.Value);
                                func((T)value.ToObject(type), fieldParams);
                            }
                            catch (Exception e)
                            {
                                Errors.Add(new Error(1500, new { Exception = e.Message }));
                            }
                        }
                    }
                    // Patch is validated when action and field are valid.
                    patch.IsValidated = true;
                }
                patch.IsValidField = true;
            }
            return Errors;
        }

        protected IEnumerable<Error> Allow<T>(string field, dynamic value, IEnumerable<Action<T, object>> validateValue, bool optional = false)
        {
            if (value == null && !optional)
            {
                Errors.Add(new Error(1101, new { Field = field }));
            }
            else if (value == null && optional)
            {
                return null;
            }

            var fieldParams = new Dictionary<string, object>();
            fieldParams.Add("Field", field);
            fieldParams.Add("Value", value.ToString());

            foreach(var func in validateValue)
            {
                try
                {
                    func(value, fieldParams);
                }
                catch (Exception e)
                {
                    Errors.Add(new Error(1500, new { Exception = e.Message }));
                }
            }

            return Errors;
        }

        protected IEnumerable<Error> SetRemainingPatchError(IEnumerable<Patch> patches)
        {
            foreach (var patch in patches)
            {
                if (!patch.IsValidated)
                {
                    if (!patch.IsValidField)
                    {
                        Errors.Add(new Error(1209, new { Field = patch.Field }));
                    }
                    else if (patch.IsValidField)
                    {
                        Errors.Add(new Error(1303, new { Action = patch.Action }));
                    }
                }
            }
            return (Errors.Any()) ? Errors : null;
        }

        protected List<Error> Errors = new List<Error>();
        protected ResourceParameters ResourceParams { get; set; }
        protected IDocumentSession Session { get; set; }

        protected RavenDB Db;
    }

    public class ResourceParameters
    {
        public string OrganizationSlug { get { return _organizationSlug ?? ""; } set { _organizationSlug = value; } }
        public string ProjectSlug { get { return _projectSlug ?? ""; } set { _projectSlug = value; } }
        public string ReportId { get { return _reportId ?? ""; } set { _reportId = value; } }
        public string UserName { get { return _userName ?? ""; } set { _userName = value; } }

        private string _organizationSlug;
        private string _projectSlug;
        private string _reportId;
        private string _userName;
    }
}
