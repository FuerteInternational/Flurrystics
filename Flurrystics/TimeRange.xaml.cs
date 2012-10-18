using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using System.IO.IsolatedStorage;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Input; 

namespace Flurrystics
{
    public partial class TimeRange : PhoneApplicationPage
    {

        string EndDate;
        string StartDate;
        RadDatePicker radDatePicker = new RadDatePicker();

        public TimeRange()
        {
            InitializeComponent();
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                EndDate = (string)IsolatedStorageSettings.ApplicationSettings["EndDate"];
                StartDate = (string)IsolatedStorageSettings.ApplicationSettings["StartDate"];
            }
            catch (KeyNotFoundException) // setting default
            {
                EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
                StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1).AddMonths(-1));
            }

            updateButtons();
        }

        private void updateButtons()
        {
            radDatePicker1.Value = DateTime.Parse(StartDate);
            radDatePicker2.Value = DateTime.Parse(EndDate);
            radDatePicker2.MinValue = DateTime.Parse(StartDate).AddDays(1);
            radDatePicker2.MaxValue = DateTime.Now;
            radDatePicker1.MaxValue = DateTime.Parse(EndDate).AddDays(-1);
        }

        private void saveDates()
        {
            IsolatedStorageSettings.ApplicationSettings["EndDate"] = EndDate;
            IsolatedStorageSettings.ApplicationSettings["StartDate"] = StartDate;
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        private void SettingsSave_Click(object sender, EventArgs e)
        {
            saveDates();
            NavigationService.GoBack();
        }

        private void SettingsCancel_Click(object sender, EventArgs e)
        { // do not store anything - just go backj
            NavigationService.GoBack();
        }

        private void button5_lastYear_Click(object sender, RoutedEventArgs e)
        {
            EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1).AddYears(-1));
            updateButtons();
            saveDates();
            NavigationService.GoBack();
        }

        private void button4_last6Months_Click(object sender, RoutedEventArgs e)
        {
            EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1).AddMonths(-6));
            updateButtons();
            saveDates();
            NavigationService.GoBack();
        }

        private void button3_lastQuarter_Click(object sender, RoutedEventArgs e)
        {
            EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1).AddMonths(-3));
            updateButtons();
            saveDates();
            NavigationService.GoBack();
        }

        private void button2_lastMonth_Click(object sender, RoutedEventArgs e)
        {
            EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1).AddMonths(-1));
            updateButtons();
            saveDates();
            NavigationService.GoBack();
        }

        private void button1_lastWeek_Click(object sender, RoutedEventArgs e)
        {
            EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1).AddDays(-7));
            updateButtons();
            saveDates();
            NavigationService.GoBack();
        }

        private void radDatePicker1_ValueChanged(object sender, ValueChangedEventArgs<object> args)
        {
            StartDate = String.Format("{0:yyyy-MM-dd}", radDatePicker1.Value);
            updateButtons();
        }

        private void radDatePicker2_ValueChanged(object sender, ValueChangedEventArgs<object> args)
        {
            EndDate = String.Format("{0:yyyy-MM-dd}", radDatePicker2.Value);
            updateButtons();
        }
    }
}