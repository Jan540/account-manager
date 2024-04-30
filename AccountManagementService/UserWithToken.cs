using AccountManagementLibrary;

namespace AccountManagementService
{
    internal class UserWithToken
    {
        public UserWithToken(User user, string token)
        {
            User = user;
            Token = token;
        }

        public string Token { get; }
        public User User { get; }

        public override string ToString()
        {
            return $"{User.Firstname} {User.Lastname}";
        }
    }
}