using Backbone.Core.Entities;

namespace Backbone.Core.Specifications
{
    namespace Backbone.Core.Specifications
    {
        public class UserWithDetailsSpecification : BaseSpecification<User>
        {
            public UserWithDetailsSpecification(int userId)
                : base(u => u.UserId == userId)
            {
                AddInclude(u => u.UserDetails);
                AddInclude(u => u.UserAddresses);
                AddInclude(u => u.UserRoleMappings);
                AddInclude("UserRoleMappings.Role");
            }

            public UserWithDetailsSpecification(string username)
                : base(u => u.UserName == username)
            {
                AddInclude(u => u.UserDetails);
                AddInclude(u => u.Status);
            }
        }

        public class ActiveUsersSpecification : BaseSpecification<User>
        {
            public ActiveUsersSpecification(int statusId)
                : base(u => u.StatusId == statusId)
            {
                ApplyOrderBy(u => u.UserName);
            }

            public ActiveUsersSpecification(int skip, int take, int statusId)
                : base(u => u.StatusId == statusId)
            {
                ApplyOrderBy(u => u.UserName);
                ApplyPaging(skip, take);
            }
        }
    }
}
