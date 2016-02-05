using System;
using System.Collections.Generic;
using System.Linq;

namespace Lisa.Breakpoint.WebApi
{
    class UserValidator : ValidatorBase<UserPost>
    {
        public UserValidator(RavenDB db)
            :base(db)
        {

        }

        public override IEnumerable<Error> ValidatePatches(ResourceParameters resource, IEnumerable<Patch> patches)
        {
            Errors = new List<Error>();
            ResourceParams = resource;

            foreach (var patch in patches)
            {
                // Don't allow any user patches
            }

            SetRemainingPatchError(patches);
            ResourceParams = null;
            return Errors;
        }

        public override IEnumerable<Error> ValidatePost(ResourceParameters resource, UserPost user)
        {
            Errors = new List<Error>();

            Allow<string>("username", user.UserName, new Action<string, object>[] { UserNameIsUnique, UserNameIsNotReserved });

            return Errors;
        }

        #region Patch validation
        #endregion

        #region Post validation
        private void UserNameIsUnique(string value, object parameters)
        {
            if (Db.UserExists(value))
            {
                Errors.Add(new Error(1104, new { Type = "user", Value = "username" }));
            }
        }

        private void UserNameIsNotReserved(string value, object parameters)
        {
            if (ProjectRoles.List.Contains(value) || OrganizationRoles.List.Contains(value) || AssignmentTypes.List.Contains(value))
            {
                Errors.Add(new Error(1104, new { Type = "user", Value = "username" }));
            }
        }
        #endregion
    }
}
