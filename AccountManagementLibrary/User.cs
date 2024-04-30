namespace AccountManagementLibrary
{
    public class User
    {
        public User()
        {
        }

        public User(int uid, string login, string firstname, string lastname, string passwordHash)
        {
            Uid = uid;
            Login = login;
            Firstname = firstname;
            Lastname = lastname;
            PasswordHash = passwordHash;
        }

        public int Uid { get; set; }
        public string Login { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string PasswordHash { get; set; }

        public override string ToString() => $"{Firstname} {Lastname}";
    }
}
