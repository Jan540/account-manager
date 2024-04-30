using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AccountManagementLibrary;

namespace AccountManagementClient
{
    public partial class MainWindow : Window
    {
        private string token = null;
        private string loggedInUser = null;

        List<User> users = null;
        ObservableCollection<Group> groups = null;
        object lastSelectedTreeViewItem = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        // DO NOT TOUCH
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = tbLogin.Text;
            string password = tbPassword.Password;
            string hash = Helpers.ComputeSha256Hash(password);

            try
            {
                string response = await UdpHelper.LoginAsync(username, hash);
                if (response == "failed")
                {
                    tbStatus.Text = "Failed to login!";
                }
                else
                {
                    tbStatus.Text = $"User {username} logged in.";
                    token = response;
                    loggedInUser = username;

                    // Load users and groups
                    users = await TcpHelper.LoadUsersAsync();
                    groups = await TcpHelper.LoadGroupsAsync();
                    tvGroups.ItemsSource = groups;
                }
            }
            catch (InvalidOperationException ioe)
            {
                tbStatus.Text = ioe.Message;
            }
        }

        // DO NOT TOUCH
        private async void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            string response = await UdpHelper.LogoutAsync(loggedInUser, token);
            if (response == "failed")
            {
                tbStatus.Text = "Failed to logout!";
            }
            else
            {
                tbStatus.Text = "Logout successful!";
                token = null;
                loggedInUser = null;
                tvGroups.ItemsSource = null;
                lbUsers.ItemsSource = null;
            }
        }

        // DO NOT TOUCH
        private void tvGroups_SelectedItemChanged(object sender, RoutedEventArgs e)
        {
            lastSelectedTreeViewItem = e.OriginalSource;
            var selectedGroup = GetSelectedGroup();
            UpdateUserList(selectedGroup);
        }

        // DO NOT TOUCH
        private Group GetSelectedGroup()
        {
            var selectedItem = tvGroups.SelectedItem;
            if (selectedItem == null)
                return null;

            Group selectedGroup;
            if (selectedItem is Group group)
            {
                selectedGroup = group;
            }
            else
            {
                var parent = VisualTreeHelper.GetParent(lastSelectedTreeViewItem as TreeViewItem);
                while (!(parent is TreeViewItem parentItem))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                selectedGroup = (Group)((TreeViewItem)parent).Header;
            }

            return selectedGroup;
        }

        // DO NOT TOUCH
        private void UpdateUserList(Group selectedGroup)
        {
            if (selectedGroup != null)
            {
                // Show all the users that are not part of the group yet
                lbUsers.ItemsSource = users.Where(u => !selectedGroup.Users.Any(su => su.Uid == u.Uid));
            }
        }

        // DO NOT TOUCH
        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            User selectedUser = lbUsers.SelectedItem as User;
            if (selectedUser == null)
            {
                MessageBox.Show("No user is selected.");
                return;
            }

            Group selectedGroup = GetSelectedGroup();
            if (selectedGroup == null)
            {
                MessageBox.Show("No group is selected.");
            }

            selectedGroup.Users.Add(selectedUser);
            UpdateUserList(selectedGroup);

            try
            {
                bool success = await TcpHelper.ProcessAddToGroupAsync(selectedGroup, selectedUser);
                if (success)
                    tbStatus.Text = $"User '{selectedUser.Firstname} {selectedUser.Lastname}' added to group '{selectedGroup.Name}'.";
                else
                    tbStatus.Text = $"Failed to add user '{selectedUser.Firstname} {selectedUser.Lastname}' to group '{selectedGroup.Name}'.";
            }
            catch (InvalidOperationException ioe)
            {
                tbStatus.Text = ioe.Message;
            }
        }

        // DO NOT TOUCH
        private async void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            Group selectedGroup = GetSelectedGroup();
            User selectedUser = GetSelectedUser();
            if (selectedUser == null)
            {
                // No user selected - remove group?
                var answer = MessageBox.Show($"Do you want to delete group '{selectedGroup.Name}'?", "Delete group?", MessageBoxButton.YesNo);
                if (answer == MessageBoxResult.Yes)
                {
                    groups.Remove(selectedGroup);

                    try
                    {
                        var success = await TcpHelper.ProcessRemoveGroupAsync(selectedGroup);
                        if (success)
                            tbStatus.Text = $"Successfully removed group '{selectedGroup.Name}'.";
                        else
                            tbStatus.Text = $"Could not remove group '{selectedGroup.Name}'.";
                    }
                    catch (InvalidOperationException ioe)
                    {
                        tbStatus.Text = ioe.Message;
                    }
                }
            }
            else
            {
                selectedGroup.Users.Remove(selectedUser);

                try
                {
                    var success = await TcpHelper.ProcessRemoveFromGroupAsync(selectedGroup, selectedUser);
                    if (success)
                        tbStatus.Text = $"Successfully removed user '{selectedUser.Firstname} {selectedUser.Lastname}' from group '{selectedGroup.Name}'.";
                    else
                        tbStatus.Text = $"Could not remove user '{selectedUser.Firstname} {selectedUser.Lastname}' from group '{selectedGroup.Name}'.";
                }
                catch (InvalidOperationException ioe)
                {
                    tbStatus.Text = ioe.Message;
                }

                UpdateUserList(selectedGroup);
            }
        }

        // DO NOT TOUCH
        private User GetSelectedUser()
        {
            var selectedUser = tvGroups.SelectedItem as User;
            return selectedUser;
        }

        // DO NOT TOUCH
        private async void btnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            string groupname = tbNewGroup.Text;
            if (string.IsNullOrWhiteSpace(groupname))
            {
                MessageBox.Show("No group name entered!");
                return;
            }

            try
            {
                int newGroupId = await TcpHelper.ProcessAddGroupAsync(groupname);
                Group newGroup = new Group(newGroupId, groupname);
                groups.Add(newGroup);
                tbStatus.Text = $"Added new group '{groupname}' with id '{newGroupId}'.";
            } 
            catch (InvalidOperationException ioe)
            {
                tbStatus.Text = ioe.Message;
            }
        }
    }
}
