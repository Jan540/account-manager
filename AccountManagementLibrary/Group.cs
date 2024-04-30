using System.Collections.ObjectModel;

namespace AccountManagementLibrary
{
    public class Group
    {
        public Group()
        {
        }

        public Group(int gid, string name)
        {
            Gid = gid;
            Name = name;
        }

        public int Gid { get; set; }
        public string Name { get; set; }
        public ObservableCollection<User> Users { get; set; } = new ObservableCollection<User>();

        public override string ToString() => Name;
    }
}
