using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AccountManagementLibrary;

namespace AccountManagementService
{
    internal class DataManager
    {
        private static readonly string usersFile = "users.csv";
        private static readonly string groupsFile = "groups.csv";
        private static readonly string groupMembersFile = "memberships.csv";

        private static readonly Dictionary<int, User> users = new();
        private static readonly List<Group> groups = new();

        internal static Dictionary<int, User> UserData
        {
            get
            {
                if (!users.Any())
                    foreach (var line in File.ReadAllLines(usersFile).Skip(1))
                    {
                        // #UID;Login;Firstname;Lastname;Password Hash
                        var data = line.Split(';');
                        var user = new User(
                            uid: int.Parse(data[0]),
                            login: data[1],
                            firstname: data[2],
                            lastname: data[3],
                            passwordHash: data[4]
                        );

                        users[user.Uid] = user;
                    }

                return users;
            }
        }

        internal static List<Group> GroupData
        {
            get
            {
                if (!groups.Any())
                    foreach (var line in File.ReadAllLines(groupsFile).Skip(1))
                    {
                        // #GID;Name
                        var data = line.Split(';');

                        groups.Add(new Group(
                            gid: int.Parse(data[0]),
                            name: data[1]
                        ));
                    }

                return groups;
            }
        }

        static DataManager()
        {
            List<Tuple<int, int>> memberships = new();

            foreach (var line in File.ReadAllLines(groupMembersFile).Skip(1))
            {
                // #GID;UID
                var data = line.Split(';');
                var gid = int.Parse(data[0]);
                var uid = int.Parse(data[1]);

                memberships.Add(new Tuple<int, int>(gid, uid) );
            }

            var groupedMems = memberships.GroupBy(m => m.Item1).ToList();

            foreach (var groupThing in groupedMems)
            {
                var group = GroupData.First(g => g.Gid == groupThing.Key);

                foreach (var membership in groupThing)
                {
                    var user = UserData.First(u => u.Key == membership.Item2);
                    group.Users.Add(user.Value);
                }
            }
        }
    }
}
