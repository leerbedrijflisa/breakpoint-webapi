using Lisa.Breakpoint.WebApi.Models;
using Raven.Abstractions.Data;
using Raven.Client;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lisa.Breakpoint.WebApi.database
{
    public partial class RavenDB
    {
        public IList<User> GetAllUsers()
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                return session.Query<User>().ToList();
            }
        }

        public User GetUser(string userName)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                return session.Query<User>()
                    .Where(u => u.Username == userName)
                    .SingleOrDefault();
            }
        }

        public string GetGroupFromUser(string organization, string projectslug, string userName)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                var project = session.Query<Project>()
                    .Where(p => p.Organization == organization && p.Slug == projectslug && p.Members.Any(m => m.Username == userName))
                    .SingleOrDefault();

                if (project != null)
                {
                    return project.Members
                        .Where(m => m.Username == userName)
                        .SingleOrDefault().Role;
                }
                else
                {
                    return null;
                }
            }
        }

        public bool UserExists(string userName)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                return session.Query<User>().Any(u => u.Username.Equals(userName));
            }
        }

        public User PostUser(UserPost user)
        {
            var userEntity = new User()
            {
                Username = user.Username,
                FullName = user.FullName
            };

            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                if (!session.Query<User>().Where(u => u.Username == userEntity.Username).Any())
                {
                    session.Store(userEntity);
                    session.SaveChanges();

                    return userEntity;
                }
                else
                {
                    return null;
                }
            }
        }

        public User PatchUser(int id, User patchedUser)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                User user = session.Load<User>(id);
                foreach (PropertyInfo propertyInfo in user.GetType().GetProperties())
                {
                    var newVal = patchedUser.GetType().GetProperty(propertyInfo.Name).GetValue(patchedUser, null);
                    if (newVal != null)
                    {
                        var patchRequest = new PatchRequest()
                        {
                            Name = propertyInfo.Name,
                            Type = PatchCommandType.Set,
                            Value = newVal.ToString()
                        };
                        documentStore.DatabaseCommands.Patch("users/" + id, new[] { patchRequest });
                    }
                }
                return user;
            }
        }

        public void DeleteUser(int id)
        {
            using (IDocumentSession session = documentStore.Initialize().OpenSession())
            {
                User user = session.Load<User>(id);
                session.Delete(user);
                session.SaveChanges();
            }
        }
    }
}