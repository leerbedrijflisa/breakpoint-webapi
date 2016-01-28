using Raven.Abstractions.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lisa.Breakpoint.WebApi
{
    public partial class RavenDB
    {
        public IList<Organization> GetAllOrganizations(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }
            
            return session.Query<Organization>()
                .Where(o => o.Members.Any(m => m == userName))
                .ToList();
        }

        public Organization GetOrganization(string organization)
        {
            if (string.IsNullOrWhiteSpace(organization))
            {
                return null;
            }
            
            return session.Query<Organization>()
                .Where(o => o.Slug == organization)
                .SingleOrDefault();
        }

        public IList<string> GetMembersNotInProject(string organization, string project)
        {
            var projectMembers = session.Query<Project>()
                .Where(p => p.Organization == organization && p.Slug == project)
                .SingleOrDefault().Members;

            var members = session.Query<Organization>()
                .Where(o => o.Slug == organization)
                .SingleOrDefault().Members;

            // Filter organization members by checking if the project contains the member
            var x = members
                .Where(name => !projectMembers
                    .Select(pm => pm.UserName)
                    .Contains(name));

            return x.ToList();
        }

        public Organization PostOrganization(OrganizationPost organization)
        {
            if (organization == null)
            {
                return null;
            }

            var organizationEntity = new Organization()
            {
                Name = organization.Name,
                Members = organization.Members,
                Slug = ToUrlSlug(organization.Name)
            };

            session.Store(organizationEntity);
            string organizationId = session.Advanced.GetDocumentId(organizationEntity);
            organizationEntity.Number = organizationId.Split('/').Last();
            session.SaveChanges();

            return organizationEntity;
        }

        public Organization PatchOrganization(int id, Organization patchedOrganization)
        {
            Organization Organization = session.Load<Organization>(id);

            foreach (PropertyInfo propertyInfo in Organization.GetType().GetProperties())
            {
                var newVal = patchedOrganization.GetType().GetProperty(propertyInfo.Name).GetValue(patchedOrganization, null);

                if (newVal != null)
                {
                    var patchRequest = new PatchRequest()
                    {
                        Name = propertyInfo.Name,
                        Type = PatchCommandType.Set,
                        Value = newVal.ToString()
                    };
                    _documentStore.DatabaseCommands.Patch("organizations/" + id, new[] { patchRequest });
                }
            }

            return Organization;
        }

        public void DeleteOrganization(string organizationSlug)
        {
            Organization organization = session.Query<Organization>().Where(o => o.Slug == organizationSlug).SingleOrDefault();

            List<Report> reports = session.Query<Report>().Where(r => r.Organization == organizationSlug).ToList();

            List<Project> projects = session.Query<Project>().Where(p => p.Organization == organizationSlug).ToList();

            foreach (var report in reports)
            {
                session.Delete(report);
            }
                
            foreach (var project in projects)
            {
                session.Delete(project);
            }
            session.Delete(organization);
            session.SaveChanges();
        }

        public bool OrganizationExists(string organizationSlug)
        {
            if (string.IsNullOrWhiteSpace(organizationSlug))
            {
                return false;
            }

            return session.Query<Organization>().Any(m => m.Slug == organizationSlug);
        }
    }
}