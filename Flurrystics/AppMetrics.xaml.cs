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
        string EndDate,EndDate2;
        string StartDate, StartDate2;
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

            TimeSpan timeRange = DateTime.Parse(EndDate) - DateTime.Parse(StartDate);

            //StartDate2 = StartDate; EndDate2 = EndDate;

            StartDate2 = String.Format("{0:yyyy-MM-dd}", DateTime.Parse(StartDate).AddDays(-timeRange.TotalDays));
            EndDate2 = String.Format("{0:yyyy-MM-dd}", DateTime.Parse(EndDate).AddDays(-timeRange.TotalDays));

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

        private void LoadUpXMLAppMetricsForTile(string metrics, Uri targetUri, StandardTileData tileToUpdate)
        {
            string eDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            string sDate = eDate;
            //string sDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-2));
            char[] splitChars1 = { '?' };
            string[] parameters = targetUri.ToString().Split(splitChars1);

            string queryParams = parameters[1]; // just take part after ?

            char[] splitChars = { '&' }; // split query parameters by &
            string[] p = queryParams.Split(splitChars);
            //Debug.WriteLine("SplitCount:" + p.Count());
            //Debug.WriteLine("param1:" + p[0]);
            //Debug.WriteLine("param2:" + p[1]);

            char[] splitChars2 = { '=' };

            string[] p1 = p[0].Split(splitChars2);
            string[] p2 = p[1].Split(splitChars2);

            string appapikey = p1[1];
            string apiKey = p2[1];
            Debug.WriteLine("apiKey:" + apiKey);
            Debug.WriteLine("appapikey:" + appapikey);

            Debug.WriteLine("LoadUpXMLAppMetrics:" + sDate + " - " + eDate);

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
                                   Label = DateTime.Parse((string)query.Attribute("date")).ToShortDateString()
                               };

                    List<ChartDataPoint> count = data.ToList();

                    int result = -1;

                    if (count.Count > 0)
                    {
                        Debug.WriteLine("We got count for livetile!");
                        result = int.Parse(count[0].Value.ToString());
                        tileToUpdate.BackTitle = "Active Users";
                        tileToUpdate.BackContent = "Yesterday: " + result;
                    }
                }
                catch (NotSupportedException) // it's not XML - probably API overload
                {
                    ShellToast backgroundToast = new ShellToast();
                    backgroundToast.Title = "Flurrysticks";
                    backgroundToast.Content = "Flurry API overload";
                    backgroundToast.Show();
                }

                ShellTile.Create(targetUri, tileToUpdate); // create Tile NO MATTER WHAT 

            });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            string callURL = "http://api.flurry.com/appMetrics/" + metrics + "?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + sDate + "&endDate=" + eDate;
            Debug.WriteLine("Calling URL:" + callURL);
            w.DownloadStringAsync(new Uri(callURL));

        }

        private void LoadUpXMLAppMetrics(string metrics, Telerik.Windows.Controls.RadCartesianChart targetChart, Microsoft.Phone.Controls.PerformanceProgressBar progressBar,
                                        RadCustomHubTile rt1, RadCustomHubTile rt2, RadCustomHubTile rt3,
                                        TextBlock t1, TextBlock t2, TextBlock t3, TextBlock tb, 
                                        String sDate, String eDate, TextBlock tr1, TextBlock tr2,
                                        int targetSeries) 
        {
            App.lastRequest = Util.getCurrentTimestamp();
            String queryURL = sDate + " - " + eDate;

            if (targetSeries > 0) { 
                // progressBar.Visibility = System.Windows.Visibility.Visible;
                rt1.IsFrozen = false;
                rt2.IsFrozen = false;
                rt3.IsFrozen = false;
                tr2.Visibility = System.Windows.Visibility.Visible;
                tr2.Text = "(" + DateTime.Parse(sDate).ToShortDateString() + " - " + DateTime.Parse(eDate).ToShortDateString() + ")";
            }
            else  // reset compare chart
            {
                TextBlock[] totals = { xtotal1, xtotal2, xtotal3, xtotal4, xtotal5, xtotal6, xtotal7, xtotal8 };
                if (MainPivot.SelectedIndex < 8)
                {
                    totals[MainPivot.SelectedIndex].Visibility = System.Windows.Visibility.Collapsed;
                }
                targetChart.Series[1].ItemsSource = null;
                tr1.Visibility = System.Windows.Visibility.Visible;
                tr2.Visibility = System.Windows.Visibility.Collapsed;                
                tr1.Text = "(" + DateTime.Parse(sDate).ToShortDateString() + " - " + DateTime.Parse(eDate).ToShortDateString() + ")";
                VisualStateManager.GoToState(rt1, "NotFlipped", true);
                VisualStateManager.GoToState(rt2, "NotFlipped", true);
                VisualStateManager.GoToState(rt3, "NotFlipped", true);
                rt1.IsFrozen = true;
                rt2.IsFrozen = true;
                rt3.IsFrozen = true;
            }

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
                    
                    // for processed data for comparison
                    ObservableCollection<ChartDataPoint> newData = new ObservableCollection<ChartDataPoint>();

                    if (targetSeries > 0) // if it's compare we have to fake time
                    {
                        var previousData = targetChart.Series[0].ItemsSource;
                        IEnumerator<ChartDataPoint> enumerator = previousData.GetEnumerator() as System.Collections.Generic.IEnumerator<ChartDataPoint>;
                        int p = 0;
                        
                        while (enumerator.MoveNext())
                        {
                            ChartDataPoint c = enumerator.Current;
                            ChartDataPoint n = data.ElementAt(p) as ChartDataPoint;
                            n.Label = c.Label;
                            newData.Add(new ChartDataPoint { Value = n.Value, Label = c.Label });
                            p++;
                        }

                    }

                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsIndeterminate = false;

                        if (targetSeries>0) {
                            targetChart.Series[targetSeries].ItemsSource = newData;
                            targetChart.Series[targetSeries].DisplayName = StartDate2 + " - " + EndDate2; 
                        } else {
                            targetChart.Series[targetSeries].ItemsSource = data;
                        }
                    
                    List<ChartDataPoint> count = data.ToList(); 

                    if (count != null)
                    {
                         targetChart.HorizontalAxis.LabelInterval = Util.getLabelIntervalByCount(count.Count);
                    }
                    else targetChart.HorizontalAxis.LabelInterval = Util.getLabelInterval(DateTime.Parse(StartDate),DateTime.Parse(EndDate));                 
                    
                        // count max,min,latest,total for display purposes
                    double latest = 0, minim = 9999999999999, maxim = 0, totalCount = 0;
                    IEnumerator<ChartDataPoint> Myenum = data.GetEnumerator();
                    while (Myenum.MoveNext())
                        {
                            ChartDataPoint oneValue = Myenum.Current;
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

                    tb.Visibility = System.Windows.Visibility.Visible;                    

                    }
                        catch (NotSupportedException) // it's not XML - probably API overload
                    {
                        MessageBox.Show("Flurry API overload, please try again later.");
                    }

                });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            string callURL = "http://api.flurry.com/appMetrics/" + metrics + "?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + sDate + "&endDate=" + eDate;
            Debug.WriteLine("Calling URL:" + callURL);
            w.DownloadStringAsync(
                new Uri(callURL)
                );
        }

        private void LoadUpXMLEvents(Microsoft.Phone.Controls.PerformanceProgressBar progressBar, String sDate, String eDate)
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
                new Uri("http://api.flurry.com/eventMetrics/Summary?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + sDate + "&endDate=" + eDate)
                );
        }

        
        private void updatePivot()
        {

            Telerik.Windows.Controls.RadCartesianChart[] targetCharts = { chart1, chart2, chart3, chart4, chart5, chart6, chart7, chart8 };
            PerformanceProgressBar[] progressBars = { progressBar1, progressBar2, progressBar3, progressBar4, progressBar5, progressBar6, progressBar7, progressBar8 };
            RadCustomHubTile[] t1s = { tile1, tile4, tile7, tile10, tile13, tile16, tile19, tile22 };
            RadCustomHubTile[] t2s = { tile2, tile5, tile8, tile11, tile14, tile17, tile20, tile23 };
            RadCustomHubTile[] t3s = { tile3, tile6, tile9, tile12, tile15, tile18, tile21, tile24 };
            TextBlock[] c1s = { number1, number4, number7, number10, number13, number16, number19, number22 };
            TextBlock[] c2s = { number2, number5, number8, number11, number14, number17, number20, number23 };
            TextBlock[] c3s = { number3, number6, number9, number12, number15, number18, number21, number24 };
            TextBlock[] totals = { total1, total2, total3, total4, total5, total6, total7, total8 };
            TextBlock[] d1s = { date1, date1_2, date1_3, date1_4, date1_5, date1_6, date1_7, date1_8 };
            TextBlock[] d2s = { date2, date2_2, date2_3, date2_4, date2_5, date2_6, date2_7, date2_8 };

            int s = MainPivot.SelectedIndex;

            string[] AppMetricsStrings = { "ActiveUsers", "ActiveUsersByWeek", "ActiveUsersByMonth", "NewUsers", "MedianSessionLength", "AvgSessionLength", "Sessions", "RetainedUsers" };

            if (lastPivotItem == MainPivot.SelectedIndex) { return; } // protection against calling this twice for the same thing
            switch (MainPivot.SelectedIndex)
            {
                case 8: // Events
                    this.Perform(() => LoadUpXMLEvents(progressBar9, StartDate, EndDate), 1000);
                    break;
                default: // else = appmetrics
                    this.Perform(() => LoadUpXMLAppMetrics(AppMetricsStrings[s], targetCharts[s], progressBars[s], t1s[s], t2s[s], t3s[s], c1s[s], c2s[s], c3s[s], totals[s], StartDate, EndDate, d1s[s], d2s[s], 0), 1000);
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
                this.Perform(() => LoadUpXMLEvents(progressBar9, StartDate, EndDate), 1000);
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
                if (Util.Is512Mb)
                {
                    this.Perform(() => LoadUpXMLAppMetricsForTile("ActiveUsers", targetUri, secondaryTile), 0);
                }
                else // put standard shelltile for 256devices
                {
                    ShellTile.Create(targetUri, secondaryTile); // Pass tileParameter as QueryString 
                }
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

        private void toggleOption_Click(object sender, EventArgs e)
        {
            ChartPanAndZoomBehavior b = chart1.Behaviors.ElementAt(0) as ChartPanAndZoomBehavior;
            ChartPanAndZoomBehavior b2 = chart2.Behaviors.ElementAt(0) as ChartPanAndZoomBehavior;
            ChartPanAndZoomBehavior b3 = chart3.Behaviors.ElementAt(0) as ChartPanAndZoomBehavior;
            ChartPanAndZoomBehavior b4 = chart4.Behaviors.ElementAt(0) as ChartPanAndZoomBehavior;
            ChartPanAndZoomBehavior b5 = chart5.Behaviors.ElementAt(0) as ChartPanAndZoomBehavior;
            ChartPanAndZoomBehavior b6 = chart6.Behaviors.ElementAt(0) as ChartPanAndZoomBehavior;
            ChartPanAndZoomBehavior b7 = chart7.Behaviors.ElementAt(0) as ChartPanAndZoomBehavior;
            ChartPanAndZoomBehavior b8 = chart8.Behaviors.ElementAt(0) as ChartPanAndZoomBehavior;
            if (b.PanMode == ChartPanZoomMode.Horizontal)
            {
                b.PanMode = ChartPanZoomMode.None;
                b.ZoomMode = ChartPanZoomMode.None;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[3]).IconUri = new Uri("/Images/flurryst_icon_bar_zoom.png", UriKind.Relative);
            }
            else
            {
                b.PanMode = ChartPanZoomMode.Horizontal;
                b.ZoomMode = ChartPanZoomMode.Horizontal;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[3]).IconUri = new Uri("/Images/flurryst_icon_bar_zoomcancel.png", UriKind.Relative);
            }

            b2.ZoomMode = b.ZoomMode; b2.PanMode = b.PanMode;
            b3.ZoomMode = b.ZoomMode; b3.PanMode = b.PanMode;
            b4.ZoomMode = b.ZoomMode; b4.PanMode = b.PanMode;
            b5.ZoomMode = b.ZoomMode; b5.PanMode = b.PanMode;
            b6.ZoomMode = b.ZoomMode; b6.PanMode = b.PanMode;
            b7.ZoomMode = b.ZoomMode; b7.PanMode = b.PanMode;
            b8.ZoomMode = b.ZoomMode; b8.PanMode = b.PanMode;
        }

        private void compareOption_Click(object sender, EventArgs e)
        {

            if (MainPivot.SelectedIndex > 7) { return; } // do nothing for events - there's no compare

            Telerik.Windows.Controls.RadCartesianChart targetChart;
            Telerik.Windows.Controls.RadCartesianChart[] targetCharts = { chart1, chart2, chart3, chart4, chart5, chart6, chart7, chart8 };
            PerformanceProgressBar[] progressBars = { progressBar1, progressBar2, progressBar3, progressBar4, progressBar5, progressBar6, progressBar7, progressBar8 };
            RadCustomHubTile[] t1s = { tile1, tile4, tile7, tile10, tile13, tile16, tile19, tile22 };
            RadCustomHubTile[] t2s = { tile2, tile5, tile8, tile11, tile14, tile17, tile20, tile23 };
            RadCustomHubTile[] t3s = { tile3, tile6, tile9, tile12, tile15, tile18, tile21, tile24 };
            TextBlock[] c1s = { change1, change4, change7, change10, change13, change16, change19, change22 };
            TextBlock[] c2s = { change2, change5, change8, change11, change14, change17, change20, change23 };
            TextBlock[] c3s = { change3, change6, change9, change12, change15, change18, change21, change24 };
            TextBlock[] totals = { xtotal1, xtotal2, xtotal3, xtotal4, xtotal5, xtotal6, xtotal7, xtotal8 };
            TextBlock[] d1s = { date1, date1_2, date1_3, date1_4, date1_5, date1_6, date1_7, date1_8 };
            TextBlock[] d2s = { date2, date2_2, date2_3, date2_4, date2_5, date2_6, date2_7, date2_8 };
            string[] AppMetricsStrings = { "ActiveUsers", "ActiveUsersByWeek", "ActiveUsersByMonth", "NewUsers", "MedianSessionLength", "AvgSessionLength", "Sessions", "RetainedUsers" };

            int s = MainPivot.SelectedIndex;

            targetChart = targetCharts[s];

            if (targetChart.Series[1].ItemsSource == null) {
                this.Perform(() => LoadUpXMLAppMetrics(AppMetricsStrings[s], targetChart, progressBars[s], t1s[s], t2s[s], t3s[s], c1s[s], c2s[s], c3s[s], totals[s], StartDate2, EndDate2, d1s[s], d2s[s], 1), 1000);
            } else
            {
                targetChart.Series[1].ItemsSource = null;
                totals[s].Visibility = System.Windows.Visibility.Collapsed;
                d2s[s].Visibility = System.Windows.Visibility.Collapsed;
                VisualStateManager.GoToState(t1s[s], "NotFlipped", true);
                VisualStateManager.GoToState(t2s[s], "NotFlipped", true);
                VisualStateManager.GoToState(t3s[s], "NotFlipped", true);
                t1s[s].IsFrozen = true;
                t2s[s].IsFrozen = true;
                t3s[s].IsFrozen = true;
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
             // In landscape mode, the totals grid is moved to the right on the screen
             // which puts it in row 1, column 1.
            if ((e.Orientation & PageOrientation.Landscape) != 0) // landscape
            {
                MainPivot.Margin = new Thickness(20, 0, 0, 0);
                double topMargin2 = 0; double topMargin = topMargin2 + 50;
                tiles1.Visibility = System.Windows.Visibility.Collapsed;
                grid1.Margin = new Thickness(10, topMargin2, 0, 0);
                chart1.Margin = new Thickness(0, topMargin, 10, 0);
                tiles2.Visibility = System.Windows.Visibility.Collapsed;
                grid2.Margin = new Thickness(10, topMargin2, 0, 0);
                chart2.Margin = new Thickness(0, topMargin, 10, 0);
                tiles3.Visibility = System.Windows.Visibility.Collapsed;
                grid3.Margin = new Thickness(10, topMargin2, 0, 0);
                chart3.Margin = new Thickness(0, topMargin, 10, 0);
                tiles4.Visibility = System.Windows.Visibility.Collapsed;
                grid4.Margin = new Thickness(10, topMargin2, 0, 0);
                chart4.Margin = new Thickness(0, topMargin, 10, 0);
                tiles5.Visibility = System.Windows.Visibility.Collapsed;
                grid5.Margin = new Thickness(10, topMargin2, 0, 0);
                chart5.Margin = new Thickness(0, topMargin, 10, 0);
                tiles6.Visibility = System.Windows.Visibility.Collapsed;
                grid6.Margin = new Thickness(10, topMargin2, 0, 0);
                chart6.Margin = new Thickness(0, topMargin, 10, 0);
                tiles7.Visibility = System.Windows.Visibility.Collapsed;
                grid7.Margin = new Thickness(10, topMargin2, 0, 0);
                chart7.Margin = new Thickness(0, topMargin, 10, 0);
                tiles8.Visibility = System.Windows.Visibility.Collapsed;
                grid8.Margin = new Thickness(10, topMargin2, 0, 0);
                chart8.Margin = new Thickness(0, topMargin, 10, 0);
            }
            else // portrait
            {
                MainPivot.Margin = new Thickness(0, 0, 0, 0);
                double topMargin2 = 150; double topMargin = topMargin2 + 50;
                tiles1.Visibility = System.Windows.Visibility.Visible;
                grid1.Margin = new Thickness(10, topMargin2, 0, 0);
                chart1.Margin = new Thickness(0, topMargin, 10, 0);
                tiles2.Visibility = System.Windows.Visibility.Visible;
                grid2.Margin = new Thickness(10, topMargin2, 0, 0);
                chart2.Margin = new Thickness(0, topMargin, 10, 0);
                tiles3.Visibility = System.Windows.Visibility.Visible;
                grid3.Margin = new Thickness(10, topMargin2, 0, 0);
                chart3.Margin = new Thickness(0, topMargin, 10, 0);
                tiles4.Visibility = System.Windows.Visibility.Visible;
                grid4.Margin = new Thickness(10, topMargin2, 0, 0);
                chart4.Margin = new Thickness(0, topMargin, 10, 0);
                tiles5.Visibility = System.Windows.Visibility.Visible;
                grid5.Margin = new Thickness(10, topMargin2, 0, 0);
                chart5.Margin = new Thickness(0, topMargin, 10, 0);
                tiles6.Visibility = System.Windows.Visibility.Visible;
                grid6.Margin = new Thickness(10, topMargin2, 0, 0);
                chart6.Margin = new Thickness(0, topMargin, 10, 0);
                tiles7.Visibility = System.Windows.Visibility.Visible;
                grid7.Margin = new Thickness(10, topMargin2, 0, 0);
                chart7.Margin = new Thickness(0, topMargin, 10, 0);
                tiles8.Visibility = System.Windows.Visibility.Visible;
                grid8.Margin = new Thickness(10, topMargin2, 0, 0);
                chart8.Margin = new Thickness(0, topMargin, 10, 0);
            }
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