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
using System.Diagnostics;
using Microsoft.Phone.Shell;

namespace Flurrystics
{
    public partial class PivotPage1 : PhoneApplicationPage
    {
        string apiKey;
        string appapikey = ""; // initial apikey of the app
        string appName = ""; // appName
        string platform = ""; // platform
        string[] EventMetrics = { "usersLastDay", "usersLastWeek", "usersLastMonth", "avgUsersLastDay", "avgUsersLastWeek", "avgUsersLastMonth", "totalSessions", "totalCount" };
        string EndDate;
        string StartDate;
        private int lastPivotItem = -1;
        private bool firstTime = true;
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
            FlurryWP7SDK.Api.LogEvent("AppMetrics");
            Debug.WriteLine("OnNavigatedTo");
            /*
            try
            {
                apiKey = (string)IsolatedStorageSettings.ApplicationSettings["apikey"];
            }
            catch (KeyNotFoundException)
            {
                NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
            }
             * */

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
            NavigationContext.QueryString.TryGetValue("apikey", out apiKey);
            NavigationContext.QueryString.TryGetValue("appapikey", out appapikey);
            NavigationContext.QueryString.TryGetValue("appName", out appName);
            NavigationContext.QueryString.TryGetValue("platform", out platform);
            SubTitle.Text = "FLURRYSTICS - " + appName;

            lastPivotItem = -1; // forcing to reset when returning from date settings
            NoEvents.Visibility = System.Windows.Visibility.Collapsed;
            updatePivot();
            
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

        private void LoadUpXMLAppMetrics(string metrics, Telerik.Windows.Controls.RadCartesianChart targetChart, Microsoft.Phone.Controls.PerformanceProgressBar progressBar, 
                                        TextBlock t1, TextBlock t2, TextBlock t3, TextBlock tb)
        {
            App.lastRequest = Util.getCurrentTimestamp();
            String queryURL = StartDate + " - " + EndDate;

            Debug.WriteLine("LoadUpXMLAppMetrics:"+queryURL);

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

                    targetChart.Series[0].ItemsSource = data;
                    List<ChartDataPoint> count = data.ToList();

                    if (count != null)
                    {
                         targetChart.HorizontalAxis.LabelInterval = Util.getLabelIntervalByCount(count.Count);
                    }
                    else targetChart.HorizontalAxis.LabelInterval = Util.getLabelInterval(DateTime.Parse(StartDate),DateTime.Parse(EndDate));
                    
                    
                        // count max,min,latest,total for display purposes
                    double latest = 0, minim = 9999999999999, maxim = 0, totalCount = 0;
                    IEnumerator<ChartDataPoint> enumerator = data.GetEnumerator();
                    while (enumerator.MoveNext())
                        {
                            ChartDataPoint oneValue = enumerator.Current;
                            latest = oneValue.Value;
                            minim = Math.Min(minim, oneValue.Value);
                            maxim = Math.Max(maxim, oneValue.Value);
                            totalCount = totalCount + oneValue.Value;
                        }

                    t1.Text = latest.ToString();    
                    t2.Text = minim.ToString();
                    t3.Text = maxim.ToString();
                    switch (metrics)
                    {
                        case "MedianSessionLength":
                        case "AvgSessionLength":
                            tb.Text = "N/A"; // makes no sense for these metrics
                            break;
                        default:
                            tb.Text = totalCount.ToString();
                            break;
                    }
                    

                    }
                        catch (NotSupportedException) // it's not XML - probably API overload
                    {
                        MessageBox.Show("Flurry API overload, please try again later.");
                    }

                });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            string callURL = "http://api.flurry.com/appMetrics/" + metrics + "?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + StartDate + "&endDate=" + EndDate;
            Debug.WriteLine("Calling URL:" + callURL);
            w.DownloadStringAsync(
                new Uri(callURL)
                );
        }

        private void LoadUpXMLEvents(Microsoft.Phone.Controls.PerformanceProgressBar progressBar)
        {
            App.lastRequest = Util.getCurrentTimestamp();
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
                    List<AppViewModel> check = data.ToList();
                    if (check.Count > 0)
                    {
                        EventsListBox.ItemsSource = data;
                        EventsListBox.Visibility = System.Windows.Visibility.Visible;
                        NoEvents.Visibility = System.Windows.Visibility.Collapsed;
                        EventsMetricsListPicker.IsEnabled = true;
                    }
                    else // show no events available
                    {
                        EventsListBox.Visibility = System.Windows.Visibility.Collapsed;
                        NoEvents.Visibility = System.Windows.Visibility.Visible;
                        EventsMetricsListPicker.IsEnabled = false;
                    }

                }
                catch (NotSupportedException) // it's not XML - probably API overload
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

        
        private void updatePivot()
        {
            if (lastPivotItem == MainPivot.SelectedIndex) { return; } // protection against calling this twice for the same thing
            switch (MainPivot.SelectedIndex)
            {
                case 0:     //ActiveUsers
                    this.Perform(() => LoadUpXMLAppMetrics("ActiveUsers", chart1, progressBar1, number1, number2, number3, total1), 1000);
                    break;
                case 1:     //ActiveUsersByWeek
                    this.Perform(() => LoadUpXMLAppMetrics("ActiveUsersByWeek", chart2, progressBar2, number4, number5, number6, total2), 1000);
                    break;
                case 2:     //ActiveUsers
                    this.Perform(() => LoadUpXMLAppMetrics("ActiveUsersByMonth", chart3, progressBar3, number7, number8, number9, total3), 1000);
                    break;
                case 3:     //ActiveUsersByWeek
                    this.Perform(() => LoadUpXMLAppMetrics("NewUsers", chart4, progressBar4, number10, number11, number12, total4), 1000);
                    break;
                case 4:     //ActiveUsers
                    this.Perform(() => LoadUpXMLAppMetrics("MedianSessionLength", chart5, progressBar5, number13, number14, number15, total5), 1000);
                    break;
                case 5:     //ActiveUsersByWeek
                    this.Perform(() => LoadUpXMLAppMetrics("AvgSessionLength", chart6, progressBar6, number16, number17, number18, total6), 1000);
                    break;
                case 6:     //ActiveUsers
                    this.Perform(() => LoadUpXMLAppMetrics("Sessions", chart7, progressBar7, number19, number20, number21, total7), 1000);
                    break;
                case 7:     //ActiveUsersByWeek
                    this.Perform(() => LoadUpXMLAppMetrics("RetainedUsers", chart8, progressBar8, number22, number23, number24, total8), 1000);
                    break;
                case 8: // Events
                    this.Perform(() => LoadUpXMLEvents(progressBar9), 1000);
                    break;
            } // switch
            lastPivotItem = MainPivot.SelectedIndex;
        }

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updatePivot();  
        }

        private void EventsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (EventsListBox.SelectedIndex == -1)
                return;

            // Navigate to the new page
            AppViewModel selected = (AppViewModel)EventsListBox.Items[EventsListBox.SelectedIndex];
            String calling = "/EventMetrics.xaml?appapikey=" + appapikey + "&apikey="+apiKey+"&appName=" + appName + "&eventName=" + selected.LineOne+"&platform="+platform;
            Debug.WriteLine("calling: " + calling);
            NavigationService.Navigate(new Uri(calling, UriKind.Relative));

            // .SelectedIndex, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            EventsListBox.SelectedIndex = -1;
        }
   
        private void EventsMetricsListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { // change event metrics
            if (!firstTime) // do not execute for the first time
            {
                progressBar9.Visibility = System.Windows.Visibility.Visible;
                this.Perform(() => LoadUpXMLEvents(progressBar9), 1000);
            }
            else firstTime=false;
        }

        private void ChartTrackBallBehavior_TrackInfoUpdated(object sender, Telerik.Windows.Controls.TrackBallInfoEventArgs e)
        {
            DateTime date = DateTime.Now;
            foreach (DataPointInfo info in e.Context.DataPointInfos)
            {
                CategoricalDataPoint dataPoint = info.DataPoint as CategoricalDataPoint;
                date = (DateTime)dataPoint.Category;
                info.DisplayHeader = info.Series.DisplayName + ": ";
                info.DisplayContent = dataPoint.Value * 1000;
            }
            e.Header = date.ToString("MMMM-yyyy");
        }

        private void timeRangeOption_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/TimeRange.xaml", UriKind.Relative));
        }

        private void pinOption_Click(object sender, EventArgs e)
        {

            string tileParameter = appapikey;
            ShellTile tile = CheckIfTileExist(tileParameter);// Check if Tile's title has been used 
            if (tile == null)
            {
                StandardTileData secondaryTile = new StandardTileData
                {
                    Title = Util.shrinkString(appName),
                    BackgroundImage = new Uri("Background2.png", UriKind.Relative),
                    //Count = 0,
                    //BackContent = "Secondary Tile Test"

                };
                Uri targetUri = new Uri("/AppMetrics.xaml?appapikey=" + appapikey + "&apikey=" + apiKey + "&appName=" + appName+"&platform="+platform, UriKind.Relative);
                ShellTile.Create(targetUri, secondaryTile); // Pass tileParameter as QueryString 
            }
            else
            {
                MessageBox.Show("Tile " + appName + " already exists on homescreen.");
            }

        }

        private ShellTile CheckIfTileExist(string tileUri)
        {
            ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault(
                    tile => tile.NavigationUri.ToString().Contains(tileUri));
            return shellTile;
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