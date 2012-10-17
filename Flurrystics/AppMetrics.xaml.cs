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
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using System.Xml.Linq;
using System.ComponentModel;
using System.Threading;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;
using Telerik.Windows.Controls;
using Telerik.Charting;

namespace Flurrystics
{
    public partial class PivotPage1 : PhoneApplicationPage
    {
        string apiKey;
        string appapikey = ""; // initial apikey of the app
        string appName = ""; // appName
        string[] EventMetrics = { "usersLastDay", "usersLastWeek", "usersLastMonth", "avgUsersLastDay", "avgUsersLastWeek", "avgUsersLastMonth", "totalSessions", "totalCount" };
        ObservableCollection<AppViewModel> EventMetricsNames = new ObservableCollection<AppViewModel>();

        public PivotPage1()
        {
            InitializeComponent();            
            EventMetricsNames.Add(new AppViewModel { LineOne = "Users Last Day" });
            EventMetricsNames.Add(new AppViewModel { LineOne = "Users Last Week" });
            EventMetricsNames.Add(new AppViewModel { LineOne = "Users Last Month" });
            EventMetricsNames.Add(new AppViewModel { LineOne = "Avg Users Last Day" });
            EventMetricsNames.Add(new AppViewModel { LineOne = "Avg Users Last Week" });
            EventMetricsNames.Add(new AppViewModel { LineOne = "Avg Users Last Month" });
            EventMetricsNames.Add(new AppViewModel { LineOne = "Total Counts" });
            EventMetricsNames.Add(new AppViewModel { LineOne = "Total Sessions" });

            EventsMetricsListPicker.ItemsSource = EventMetricsNames;

        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                apiKey = (string)IsolatedStorageSettings.ApplicationSettings["apikey"];
            }
            catch (KeyNotFoundException)
            {
                NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
            }
            NavigationContext.QueryString.TryGetValue("apikey", out appapikey);
            NavigationContext.QueryString.TryGetValue("appName", out appName);
            MainPivot.Title = "FLURRYSTICS - " + appName; 


        }

        private void Perform(Action myMethod, int delayInMilliseconds)
        {

            long diff = Util.getCurrentTimestamp() - App.lastRequest;
            int throttledDelay = 0;

            if (diff < delayInMilliseconds) // if delay between requests is less then second then count time we need to wait before firing up next request
            {
                throttledDelay = (int)diff;
            }

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) => Thread.Sleep(throttledDelay);
            worker.RunWorkerCompleted += (s, e) => myMethod.Invoke();
            worker.RunWorkerAsync();        
        
        }

        private void LoadUpXMLAppMetrics(string metrics, Telerik.Windows.Controls.RadCartesianChart targetChart, Microsoft.Phone.Controls.PerformanceProgressBar progressBar)
        {
            App.lastRequest = Util.getCurrentTimestamp();
            string EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            string StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-31));
            String queryURL = StartDate + " - " + EndDate;

            WebClient w = new WebClient();

                Observable
                .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
                .Subscribe(r =>
                {
                    try
                    {
                        XDocument loadedData = XDocument.Parse(r.EventArgs.Result);
                        //XDocument loadedData = XDocument.Load("getAllApplications.xml");

                    // ListTitle.Text = (string)loadedData.Root.Attribute("metric");
                    var data = from query in loadedData.Descendants("day")
                               select new ChartDataPoint
                               {
                                   Value = (double)query.Attribute("value"),
                                   Label = Util.stripOffYear(DateTime.Parse((string)query.Attribute("date")))
                               };

                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsIndeterminate = false;

                    //targetChart.DataContext = data;
                    targetChart.Series[0].ItemsSource = data;
                    //targetChart.Series[0].ItemsSource = new double[] { 20, 30, 50, 10, 60, 40, 20, 80 };

                    //targetChart.DataSource = data;
                    // MainListBox.ItemsSource = data;

                    }
                        catch (NotSupportedException e) // it's not XML - probably API overload
                    {
                        //MessageBox.Show("Flurry API overload, please try again later.");
                    }

                });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            w.DownloadStringAsync(
                new Uri("http://api.flurry.com/appMetrics/"+metrics+"?apiAccessCode="+apiKey+"&apiKey=" + appapikey + "&startDate=" + StartDate + "&endDate=" + EndDate)
                );
        }

        private void LoadUpXMLEvents(Microsoft.Phone.Controls.PerformanceProgressBar progressBar)
        {
            App.lastRequest = Util.getCurrentTimestamp();
            string EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            string StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-31));
            String queryURL = StartDate + " - " + EndDate;

            WebClient w = new WebClient();

            Observable
            .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
            .Subscribe(r =>
            {
                try
                {
                    XDocument loadedData = XDocument.Parse(r.EventArgs.Result);

                    // ListTitle.Text = (string)loadedData.Root.Attribute("metric");
                    var data = from query in loadedData.Descendants("event")
                               orderby (int)query.Attribute(EventMetrics[EventsMetricsListPicker.SelectedIndex]) descending
                               select new AppViewModel
                               {
                                   LineOne = (string)query.Attribute("eventName"),
                                   LineTwo = (string)query.Attribute(EventMetrics[EventsMetricsListPicker.SelectedIndex])
                               };

                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsIndeterminate = false;
                    EventsListBox.ItemsSource = data;

                }
                catch (NotSupportedException e) // it's not XML - probably API overload
                {
                    //MessageBox.Show("Flurry API overload, please try again later.");
                }

            });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            w.DownloadStringAsync(
                // http://api.flurry.com/eventMetrics/Summary?apiAccessCode=DJBUBP9NE5YBQB5CQKH3&apiKey=HXCWZ1L3CWMVGQM68JPI&startDate=2012-09-01&endDate=2012-09-02
                new Uri("http://api.flurry.com/eventMetrics/Summary?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + StartDate + "&endDate=" + EndDate)
                );
        }

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (MainPivot.SelectedIndex)
            {
                case 0:     //ActiveUsers
                    this.Perform(() => LoadUpXMLAppMetrics("ActiveUsers", chart1, progressBar1), 1000);
                    break;
                case 1:     //ActiveUsersByWeek
                    //this.Perform(() => LoadUpXMLAppMetrics("ActiveUsersByWeek", chart2, progressBar2), 1000);
                    break;
                case 2:     //ActiveUsers
                    //this.Perform(() => LoadUpXMLAppMetrics("ActiveUsersByMonth", chart3, progressBar3), 1000);
                    break;
                case 3:     //ActiveUsersByWeek
                    //this.Perform(() => LoadUpXMLAppMetrics("NewUsers", chart4, progressBar4), 1000);
                    break;
                case 4:     //ActiveUsers
                    //this.Perform(() => LoadUpXMLAppMetrics("MedianSessionLength", chart5, progressBar5), 1000);
                    break;
                case 5:     //ActiveUsersByWeek
                    //this.Perform(() => LoadUpXMLAppMetrics("AvgSessionLength", chart6, progressBar6), 1000);
                    break;
                case 6:     //ActiveUsers
                    //this.Perform(() => LoadUpXMLAppMetrics("Sessions", chart7, progressBar7), 1000);
                    break;
                case 7:     //ActiveUsersByWeek
                    //this.Perform(() => LoadUpXMLAppMetrics("RetainedUsers", chart8, progressBar8), 1000);
                    break;
                case 8: // Events
                    this.Perform(() => LoadUpXMLEvents(progressBar9), 1000);
                    break;

            } // switch
        }

        private void EventsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (EventsListBox.SelectedIndex == -1)
                return;

            // Navigate to the new page
            AppViewModel selected = (AppViewModel)EventsListBox.Items[EventsListBox.SelectedIndex];
            NavigationService.Navigate(new Uri("/EventMetrics.xaml?apikey=" + appapikey + "&appName=" + appName + "&eventName=" + selected.LineOne, UriKind.Relative));

            // .SelectedIndex, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            EventsListBox.SelectedIndex = -1;
        }

        private int count = 1;

        private void EventsMetricsListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { // change event metrics
            if (count > 1) // do not execute for the first time
            {
                progressBar9.Visibility = System.Windows.Visibility.Visible;
                this.Perform(() => LoadUpXMLEvents(progressBar9), 1000);
            }
            else count=2;
        }

        private void ChartTrackBallBehavior_TrackInfoUpdated(object sender, Telerik.Windows.Controls.TrackBallInfoEventArgs e)
        {
            DateTime date = DateTime.Now;
            /*
            foreach (DataPointInfo info in e.Context.DataPointInfos)
            {
                CategoricalDataPoint dataPoint = info.DataPoint as CategoricalDataPoint;
                date = (DateTime)dataPoint.Category;
                info.DisplayHeader = info.Series.DisplayName + ": ";
                info.DisplayContent = dataPoint.Value * 1000;
            }

            e.Header = date.ToString("MMMM-yyyy");
            */
        }

    } // class

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; } // appmetrics
        public double Value1 { get; set; } // Unique Users
        public double Value2 { get; set; } // Total Sessions
        public double Value3 { get; set; } //
        public double Value4 { get; set; }
    }

   public class EventMetricsName
   {
       public string MetricsName
       {
           get;
           set;
       }

   }

}