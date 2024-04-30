using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using AccountManagementLibrary;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace AccountManagementService
{
    internal class ChartModels
    {
        private PieChart pieChart;
        private List<Group> groups;
        private readonly CartesianChart barChart;
        private readonly List<User> users;

        public ChartModels(PieChart pieChart, List<Group> groups, CartesianChart barChart, Dictionary<int, User> users)
        {
            this.pieChart = pieChart;
            this.groups = groups;
            
            this.barChart = barChart;
            this.users = users.Values.OrderBy(u => u.Lastname).ThenBy(u => u.Firstname).ToList();
            GroupNumberFormatter = value => value.ToString("N");
            UserLabels = this.users.Select(u => u.Lastname).ToArray();
            UpdateCharts();
            this.barChart.DataContext = this;
        }

        public string[] UserLabels { get; }
        public Func<double, string> GroupNumberFormatter { get; }

        internal void UpdateCharts()
        {
            // This is not very efficient and could be improved with incremental
            // updates but to keep usage of the method simple, we do without it
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var g in groups)
                {
                    var existingSeries = pieChart.Series.Where(s => s.Title == g.Name).FirstOrDefault();
                    if (existingSeries == null)
                    {
                        existingSeries = new PieSeries
                        {
                            Title = g.Name,
                            Values = new ChartValues<ObservableValue> { new ObservableValue(g.Users.Count) },
                            DataLabels = true,
                            LabelPoint = chartPoint => string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation)
                        };
                        pieChart.Series.Add(existingSeries);
                    }
                    else
                    {
                        ((ObservableValue)existingSeries.Values[0]).Value = g.Users.Count;
                    }
                }

                // find any series that have been removed (if any)
                for (int i = 0; i < pieChart.Series.Count; i++)
                {
                    var series = pieChart.Series[i];
                    if (!groups.Where(g => g.Name == series.Title).Any())
                    {
                        pieChart.Series.Remove(series);
                        i--;
                    }
                }

                // The bar chart only has a single series (the group count series)
                // with values for each of the users.
                // Since the user count never changes, we don't have to handle added/removed users.
                var existingBarSeries = barChart.Series.FirstOrDefault();
                if (existingBarSeries == null)
                {
                    existingBarSeries = new ColumnSeries
                    {
                        Title = "Group Membership",
                        DataLabels = true,
                        LabelPoint = chartPoint => string.Format("{0}", chartPoint.Y),
                        Values = new ChartValues<ObservableValue>(),
                    };
                    barChart.Series.Add(existingBarSeries);
                }

                int valueCount = existingBarSeries.Values.Count;
                for (int i = 0; i < users.Count; i++)
                {
                    var user = users[i];
                    var groupCount = groups.SelectMany(g => g.Users.Where(x => x.Lastname == user.Lastname)).Count();
                    if (i < valueCount)
                    {
                        ((ObservableValue)existingBarSeries.Values[i]).Value = groupCount;
                    }
                    else
                    {
                        existingBarSeries.Values.Add(new ObservableValue(groupCount));
                    }
                }
            });
        }
    }
}
