using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lisa.Breakpoint.WebApi
{
    class ProjectValidator : ValidatorBase<ProjectPost>
    {
        public ProjectValidator(RavenDB db)
            :base(db)
        {

        }

        public override IEnumerable<Error> ValidatePatches(ResourceParameters resource, IEnumerable<Patch> patches)
        {
            Errors = new List<Error>();
            ResourceParams = resource;

            foreach (var patch in patches)
            {
                Allow<Member>(patch, "add", new Regex(@"^members$"), new Action<Member, object>[] { MemberIsUser, MemberIsInOrganization, MemberIsNotInProject, MemberHasValidRole }, new Action<dynamic>[] { });
                Allow<Member>(patch, "remove", new Regex(@"^members$"), new Action<Member, object>[] { MemberIsInProject, ProjectRetainsManagers }, new Action<dynamic>[] { });
                Allow<string>(patch, "replace", new Regex(@"^currentversion$"), new Action<string, object>[] { }, new Action<dynamic>[] { });
            }

            SetRemainingPatchError(patches);
            ResourceParams = null;
            return Errors;
        }

        public override IEnumerable<Error> ValidatePost(ResourceParameters resource, ProjectPost project)
        {
            Errors = new List<Error>();
            ResourceParams = resource;

            Allow<string>("name", project.Name, new Action<string, object>[] { NameCanBeSlug, ProjectNameIsUnique });
            Allow<Member[]>("members", project.Members.ToArray(), new Action<Member[], object>[] { ProjectHasMembers, MembersExists, MembersAreInOrganization, MembersHaveValidRoles });

            return Errors;
        }

        #region General validation
        private void ValueIsGroup(string value, dynamic parameters)
        {
            if (!ProjectRoles.List.Contains(value))
            {
                Errors.Add(ProjectRoles.InvalidValueError);
            }
        }
        #endregion

        #region Patch validation
        private void MemberIsUser(Member value, dynamic parameters)
        {
            if (!Db.UserExists(value.UserName))
            {
                Errors.Add(new Error(1401, new { UserName = value.UserName }));
            }
        }

        private void MemberIsInOrganization(Member value, dynamic parameters)
        {
            var members = Db.GetOrganization(ResourceParams.OrganizationSlug).Members.ToList();

            if (!members.Contains(value.UserName))
            {
                Errors.Add(new Error(1402, new { UserName = value.UserName }));
            }
        }

        private void MemberIsNotInProject(Member value, dynamic parameters)
        {
            if (Db.GetProject(ResourceParams.OrganizationSlug, ResourceParams.ProjectSlug, ResourceParams.UserName).Members.Any(m => m.UserName == value.UserName))
            {
                Errors.Add(new Error(1312, new { UserName = value.UserName }));
            }
        }

        private void MemberIsInProject(Member value, dynamic parameters)
        {
            if (!Db.GetProject(ResourceParams.OrganizationSlug, ResourceParams.ProjectSlug, ResourceParams.UserName).Members.Any(m => m.UserName == value.UserName))
            {
                Errors.Add(new Error(1403, new { UserName = value.UserName }));
            }
        }

        private void MemberHasValidRole(Member value, dynamic parameters)
        {
            if (!ProjectRoles.List.Contains(value.Role))
            {
                Errors.Add(ProjectRoles.InvalidValueError);
            }
        }

        private void ProjectRetainsManagers(Member value, dynamic parameters)
        {
            var managerCount = Db.GetProject(ResourceParams.OrganizationSlug, ResourceParams.ProjectSlug, ResourceParams.UserName).Members.Where(m => m.Role == ProjectRoles.Managers).Count();
            
            _managersDeleted++;

            if (managerCount <= 1 || _managersDeleted >= managerCount)
            {
                Errors.Add(new Error(1309));
            }
        }
        #endregion

        #region Post Validation
        private void ProjectNameIsUnique(string value, dynamic parameters)
        {
            if (Db.ProjectExists(ResourceParams.OrganizationSlug, Db.ToUrlSlug(value)))
            {
                Errors.Add(new Error(1104, new { type = "project", value = "name" }));
            }
        }
        
        private void ProjectHasMembers(Member[] value, dynamic parameters)
        {
            if (value.Count() < 1)
            {
                Errors.Add(new Error(1310));
            }
        }

        private void MembersExists(Member[] value, dynamic parameters)
        {
            foreach (var member in value)
            {
                if (!Db.UserExists(member.UserName))
                {
                    Errors.Add(new Error(1401, new { UserName = member.UserName }));
                }
            }
        }

        private void MembersAreInOrganization(Member[] value, dynamic parameters)
        {
            var members = Db.GetOrganization(ResourceParams.OrganizationSlug).Members.ToList();

            foreach (var member in value)
            {
                if (!members.Contains(member.UserName))
                {
                    Errors.Add(new Error(1402, new { UserName = member.UserName }));
                }
            }
        }

        private void NameCanBeSlug(string value, dynamic parameters)
        {
            if (string.IsNullOrWhiteSpace(Db.ToUrlSlug(value)))
            {
                Errors.Add(new Error(1214));
            }
        }

        private void MembersHaveValidRoles(Member[] value, dynamic parameters)
        {
            foreach (var member in value)
            {
                if (!ProjectRoles.List.Contains(member.Role))
                {
                    Errors.Add(ProjectRoles.InvalidValueError);
                }
            }
        }
        #endregion

        private int _managersDeleted = 0;
    }
}
