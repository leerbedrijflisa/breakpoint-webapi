using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lisa.Breakpoint.WebApi
{
    class ReportValidator : ValidatorBase<ReportPost>
    {
        public ReportValidator(RavenDB db)
            :base(db)
        {

        }

        public override IEnumerable<Error> ValidatePatches(ResourceParameters resource, IEnumerable<Patch> patches)
        {
            Errors = new List<Error>();
            ResourceParams = resource;

            foreach (var patch in patches)
            {
                Allow<string>(patch, "replace", new Regex(@"^status$"), new Action<string, object>[] { ValueIsStatus }, new Action<dynamic>[] { });
                Allow<string>(patch, "replace", new Regex(@"^priority$"), new Action<string, object>[] { ValueIsPriority }, new Action<dynamic>[] { });
                Allow<AssignedTo>(patch, "replace", new Regex(@"^assignedTo$"), new Action<AssignedTo, object>[] { ValueIsAssignment }, new Action<dynamic>[] { });
            }

            SetRemainingPatchError(patches);
            ResourceParams = null;
            return Errors;
        }

        public override IEnumerable<Error> ValidatePost(ResourceParameters resource, ReportPost report)
        {
            Errors = new List<Error>();

            Allow<string>("reporter", report.Reporter, new Action<string, object>[] { ReporterExists, ReporterIsInProject });
            Allow<string>("status", report.Status, new Action<string, object>[] { ValueIsStatus });
            Allow<string>("priority", report.Priority, new Action<string, object>[] { ValueIsPriority });
            Allow<AssignedTo>("assignedTo", report.AssignedTo, new Action<AssignedTo, object>[] { ValueIsAssignment });

            return Errors;
        }

        #region General validation
        private void ValueIsStatus(string value, dynamic parameters)
        {
            if (!Statuses.List.Contains(value))
            {
                Errors.Add(Statuses.InvalidValueError);
            }
        }

        private void ValueIsPriority(string value, dynamic parameters)
        {
            if (!Priorities.List.Contains(value))
            {
                Errors.Add(Priorities.InvalidValueError);
            }
        }

        private void ValueIsAssignment(AssignedTo value, dynamic parameters)
        {
            if (value.Type == "group" && !ProjectRoles.List.Contains(value.Value))
            {
                // TODO: Make error not use default role error.
                Errors.Add(ProjectRoles.InvalidValueError);
            }
            // If assigned to a person, check if the person is in the project.
            else if (value.Type == "person" && !Db.GetProject(ResourceParams.OrganizationSlug, ResourceParams.ProjectSlug, ResourceParams.UserName).Members.Any(m => m.UserName == value.Value))
            {
                //TODO: Add error user not in project.
                Errors.Add(new Error(1));
            }
            else
            {
                //TODO: Add error type must be group / person.
                Errors.Add(new Error(1));
            }
        }
        #endregion

        #region Post Validation
        private void ReporterExists(string value, dynamic parameters)
        {
            if (!Db.UserExists(value))
            {
                //TODO: Add error user doesn't exist.
                Errors.Add(new Error(1));
            }
        }

        private void ReporterIsInProject(string value, dynamic parameters)
        {
            if (!Db.GetProject(ResourceParams.OrganizationSlug, ResourceParams.ProjectSlug, ResourceParams.UserName).Members.Any(m => m.UserName == value))
            {
                //TODO: Add error member trying to remove not found.
                Errors.Add(new Error(1));
            }
        }
        #endregion
    }
}
