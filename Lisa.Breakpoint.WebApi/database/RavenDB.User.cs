using Raven.Abstractions.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lisa.Breakpoint.WebApi
{
    public partial class RavenDB
    {
        public IList<User> GetAllUsers()
        {
            return session.Query<User>().ToList();
        }

        public User GetUser(string userName)
        {
            return session.Query<User>()
                .Where(u => u.UserName == userName)
                .SingleOrDefault();
        }

        public string GetGroupFromUser(string organization, string projectslug, string userName)
        {
            var project = session.Query<Project>()
                .Where(p => p.Organization == organization && p.Slug == projectslug && p.Members.Any(m => m.UserName == userName))
                .SingleOrDefault();

            if (project == null)
            {
                return null;
            }

            return project.Members
                    .Where(m => m.UserName == userName)
                    .SingleOrDefault().Role;
        }

        public bool UserExists(string userName)
        {
            return session.Query<User>().Any(u => u.UserName.Equals(userName));
        }

        public User PostUser(UserPost user)
        {
            var userEntity = new User()
            {
                UserName = user.UserName,
                FullName = user.FullName
            };

            if (!session.Query<User>().Where(u => u.UserName == userEntity.UserName).Any())
            {
                session.Store(userEntity);
                session.SaveChanges();

                return userEntity;
            }

            return null;
        }

        public User PatchUser(int id, User patchedUser)
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
                    _documentStore.DatabaseCommands.Patch("users/" + id, new[] { patchRequest });
                }
            }
            return user;
        }

        public void DeleteUser(int id)
        {
            User user = session.Load<User>(id);
            session.Delete(user);
            session.SaveChanges();
        }
    }
}