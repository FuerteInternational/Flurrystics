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
using System.Diagnostics;
using Microsoft.Phone.Shell;
using Telerik.Charting;
using Telerik.Windows.Controls;
using System.Diagnostics;
using Microsoft.Phone.Shell;

namespace Flurrystics
{
    public partial class PivotPage2 : PhoneApplicationPage
    {
        string apiKey;
        string appapikey = ""; // initial apikey of the app
        string appName = ""; // appName
        string eventName = ""; // eventName
        string platform = "";
        string EndDate, EndDate2;
        string StartDate, StartDate2;
        XDocument loadedData;
        ObservableCollection<AppViewModel> ParamKeys = new ObservableCollection<AppViewModel>();

        public PivotPage2()
        {
            InitializeComponent();
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            FlurryWP7SDK.Api.LogEvent("EventMetrics");
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

            TimeSpan timeRange = DateTime.Parse(EndDate) - DateTime.Parse(StartDate);

            //StartDate2 = StartDate; EndDate2 = EndDate;

            StartDate2 = String.Format("{0:yyyy-MM-dd}", DateTime.Parse(StartDate).AddDays(-timeRange.TotalDays));
            EndDate2 = String.Format("{0:yyyy-MM-dd}", DateTime.Parse(EndDate).AddDays(-timeRange.TotalDays));

            NavigationContext.QueryString.TryGetValue("apikey", out apiKey);
            NavigationContext.QueryString.TryGetValue("appapikey", out appapikey);
            NavigationContext.QueryString.TryGetValue("appName", out appName);
            NavigationContext.QueryString.TryGetValue("eventName", out eventName);
            NavigationContext.QueryString.TryGetValue("platform", out platform);
            String whatTitle = "FLURRYSTICS - " + appName + " - " + eventName;
            SubTitle.Text = whatTitle;
            Debug.WriteLine(whatTitle);
            ParamKeys.Clear();
            first = true;
            NoParameters.Visibility = System.Windows.Visibility.Collapsed;
            this.Perform(() => LoadUpXMLEventMetrics(StartDate,EndDate,0), 1000);

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

        private void LoadUpXMLEventParameters(XDocument loadedData, int selectedIndex, bool addParam) {
             // parse data for parameters
                    var dataParam = from query in loadedData.Descendants("key")
                                    select new Data
                                    {
                                        key = (string)query.Attribute("name"),
                                        content = (IEnumerable<XElement>)query.Descendants("value"),
                                    };              
                    IEnumerator<Data> enumerator = dataParam.GetEnumerator();
                    int index = 0;
                    IEnumerable<AppViewModel> dataParams = null;
                    while (enumerator.MoveNext())
                    {
                        Data dataParamValues = enumerator.Current;
                        if (addParam) {  
                            ParamKeys.Add(new AppViewModel { LineOne = dataParamValues.key });
                        }
                            dataParams = from query in dataParamValues.content
                                                        orderby (int)query.Attribute("totalCount") descending
                                                        select new AppViewModel
                                                       {
                                                           LineOne = (string)query.Attribute("name"),
                                                           LineTwo = (string)query.Attribute("totalCount")
                                                       };
                            if (index == selectedIndex)
                            {
                                Debug.WriteLine("Setting parameter values list");
                                ParametersListBox.ItemsSource = dataParams; // dataParamValues.children;
                            }
                            Debug.WriteLine("Processing line: " + index);
                            index = index + 1;                            
                    }

                    Debug.WriteLine("ParamKeys.Count=" + ParamKeys.Count);

          if (ParamKeys.Count > 0)
                    {
                        ParametersMetricsListPicker.ItemsSource = ParamKeys;
                        ParametersMetricsListPicker.IsEnabled = true;
                        NoParameters.Visibility = System.Windows.Visibility.Collapsed;

                        List<AppViewModel> check = dataParams.ToList();
                        if (check.Count > 0)
                        {
                            NoParameters.Visibility = System.Windows.Visibility.Collapsed;
                            ParametersMetricsListPicker.IsEnabled = true;
                        }
                        else // show no events available
                        {
                            NoParameters.Visibility = System.Windows.Visibility.Visible;
                            ParametersMetricsListPicker.IsEnabled = false;
                        }

                    }
                    else
                    {
                        ParametersMetricsListPicker.IsEnabled = false;
                        NoParameters.Visibility = System.Windows.Visibility.Visible;
                    }

            progressBar1.Visibility = System.Windows.Visibility.Collapsed;
            progressBar1.IsIndeterminate = false;
        }

        private void LoadUpXMLEventMetrics(String sDate, String eDate, int targetSeries)
        {
            Telerik.Windows.Controls.RadCartesianChart[] targetCharts = { chart1, chart2, chart3 };
            RadCustomHubTile[] t1s = { tile1, tile4, tile7 };
            RadCustomHubTile[] t2s = { tile2, tile5, tile8 };
            RadCustomHubTile[] t3s = { tile3, tile6, tile9 };
            TextBlock[] n1s = { number1, number4, number7 };
            TextBlock[] n2s = { number2, number5, number8 };
            TextBlock[] n3s = { number3, number6, number9 };
            TextBlock[] c1s = { change1, change4, change7 };
            TextBlock[] c2s = { change2, change5, change8 };
            TextBlock[] c3s = { change3, change6, change9 };
            TextBlock[] totals2 = { xtotal1, xtotal2, xtotal3 };
            TextBlock[] totals = { total1, total2, total3 };
            TextBlock[] d1s = { date1, date1_2, date1_3 };
            TextBlock[] d2s = { date2, date2_2, date2_3 };
            //int s = MainPivot.SelectedIndex;
            App.lastRequest = Util.getCurrentTimestamp();
            String queryURL = sDate + " - " + eDate;

            if (targetSeries > 0)
            {
                // progressBar.Visibility = System.Windows.Visibility.Visible;
                for (int i = 0; i < 3; i++)
                {
                    t1s[i].IsFrozen = false;
                    t2s[i].IsFrozen = false;
                    t3s[i].IsFrozen = false;
                    totals2[i].Visibility = System.Windows.Visibility.Visible;
                    d2s[i].Text = "(" + DateTime.Parse(sDate).ToShortDateString() + " - " + DateTime.Parse(eDate).ToShortDateString() + ")";
                    d2s[i].Visibility = System.Windows.Visibility.Visible;
                }
            }
            else  // reset compare chart
            {
                for (int i = 0; i < 3; i++)
                {
                    totals2[i].Visibility = System.Windows.Visibility.Collapsed;
                    targetCharts[i].Series[1].ItemsSource = null;
                    d1s[i].Visibility = System.Windows.Visibility.Visible;
                    d2s[i].Visibility = System.Windows.Visibility.Collapsed;
                    d1s[i].Text = "(" + DateTime.Parse(sDate).ToShortDateString() + " - " + DateTime.Parse(eDate).ToShortDateString() + ")";
                    t1s[i].IsFrozen = true;
                    t2s[i].IsFrozen = true;
                    t3s[i].IsFrozen = true;
                    VisualStateManager.GoToState(t1s[i], "NotFlipped", true);
                    VisualStateManager.GoToState(t2s[i], "NotFlipped", true);
                    VisualStateManager.GoToState(t3s[i], "NotFlipped", true);
                }
            }

            WebClient w = new WebClient();

                Observable
                .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
                .Subscribe(r =>
                {
                    try
                    {
                        loadedData = XDocument.Parse(r.EventArgs.Result);
                        //XDocument loadedData = XDocument.Load("getAllApplications.xml");

                    // ListTitle.Text = (string)loadedData.Root.Attribute("metric");
                    // parse data for charts
                    var data = from query in loadedData.Descendants("day")
                               select new ChartDataPoint
                               {
                                   // <day uniqueUsers="378" totalSessions="3152" totalCount="6092" duration="0" date="2012-09-13"/>
                                   Value1 = (double)query.Attribute("uniqueUsers"),
                                   Value2 = (double)query.Attribute("totalSessions"),
                                   Value3 = (double)query.Attribute("totalCount"),
                                   // Value4 = (double)query.Attribute("duration"),
                                   Label = Util.stripOffYear(DateTime.Parse((string)query.Attribute("date")))
                               };
                    if (!(targetSeries > 0))
                    { // process parameters only when not processing compare data
                        LoadUpXMLEventParameters(loadedData, 0, true);
                    }

                    // count max,min,latest,total for display purposes
                    double latest = 0, minim = 9999999999999, maxim = 0, totalCount = 0;
                    double latest2 = 0, minim2 = 9999999999999, maxim2 = 0, totalCount2 = 0;
                    double latest3 = 0, minim3 = 9999999999999, maxim3 = 0, totalCount3 = 0;
                    IEnumerator<ChartDataPoint> enumerator = data.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        ChartDataPoint oneValue = enumerator.Current;

                        latest = oneValue.Value1;
                        minim = Math.Min(minim, oneValue.Value1);
                        maxim = Math.Max(maxim, oneValue.Value1);
                        totalCount = totalCount + oneValue.Value1;
                        
                        latest2 = oneValue.Value2;
                        minim2 = Math.Min(minim2, oneValue.Value2);
                        maxim2 = Math.Max(maxim2, oneValue.Value2);
                        totalCount2 = totalCount2 + oneValue.Value2;
                        
                        latest3 = oneValue.Value3;
                        minim3 = Math.Min(minim, oneValue.Value3);
                        maxim3 = Math.Max(maxim, oneValue.Value3);
                        totalCount3 = totalCount3 + oneValue.Value3;

                    }

                    if (!(targetSeries > 0))
                    {
                            n1s[0].Text = latest.ToString();
                            n2s[0].Text = minim.ToString();
                            n3s[0].Text = maxim.ToString();
                            totals[0].Text = totalCount.ToString();
                            n1s[1].Text = latest2.ToString();
                            n2s[1].Text = minim2.ToString();
                            n3s[1].Text = maxim2.ToString();
                            totals[1].Text = totalCount2.ToString();
                            n1s[2].Text = latest3.ToString();
                            n2s[2].Text = minim3.ToString();
                            n3s[2].Text = maxim3.ToString();
                            totals[2].Text = totalCount3.ToString();
                    }
                    else
                    {
                            c1s[0].Text = latest.ToString();
                            c2s[0].Text = minim.ToString();
                            c3s[0].Text = maxim.ToString();
                            totals2[0].Text = totalCount.ToString();
                            c1s[1].Text = latest2.ToString();
                            c2s[1].Text = minim2.ToString();
                            c3s[1].Text = maxim2.ToString();
                            totals2[1].Text = totalCount2.ToString();
                            c1s[2].Text = latest3.ToString();
                            c2s[2].Text = minim3.ToString();
                            c3s[2].Text = maxim3.ToString();
                            totals2[2].Text = totalCount3.ToString();
                    }

                    List<ChartDataPoint> count = data.ToList();

                    int setInterval = 5; // default
                        if (count != null)
                        {
                            setInterval = Util.getLabelIntervalByCount(count.Count);
                        }
                        else setInterval = Util.getLabelInterval(DateTime.Parse(StartDate), DateTime.Parse(EndDate));

                        // re-assign time data if comparing

                    // for processed data for comparison
                    ObservableCollection<ChartDataPoint> newData = new ObservableCollection<ChartDataPoint>();

                    if (targetSeries > 0) // if it's compare we have to fake time
                    {
                        var previousData = targetCharts[0].Series[0].ItemsSource;
                        IEnumerator<ChartDataPoint> myenumerator = previousData.GetEnumerator() as System.Collections.Generic.IEnumerator<ChartDataPoint>;
                        int p = 0;

                        while (myenumerator.MoveNext())
                        {
                            ChartDataPoint c = myenumerator.Current;
                            ChartDataPoint n = data.ElementAt(p) as ChartDataPoint;
                            n.Label = c.Label;
                            newData.Add(new ChartDataPoint { Value1 = n.Value1, Value2 = n.Value2, Value3=n.Value3, Label = c.Label });
                            p++;
                        }

                    }
                        if (!(targetSeries > 0))
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                targetCharts[i].Series[0].ItemsSource = data;
                                targetCharts[i].HorizontalAxis.LabelInterval = setInterval;                          
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                targetCharts[i].Series[1].ItemsSource = newData;
                                targetCharts[i].HorizontalAxis.LabelInterval = setInterval;
                            }
                        }

                    }
                        catch (NotSupportedException) // it's not XML - probably API overload
                    {
                        MessageBox.Show("Flurry API overload, please try again later.");
                    }

                });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            string callURL = "http://api.flurry.com/eventMetrics/Event?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + sDate + "&endDate=" + eDate + "&eventName=" + eventName;
            Debug.WriteLine(callURL);
            w.DownloadStringAsync(
                new Uri(callURL)
                );
        }

        private bool first = true;
        private void ParametersMetricsListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (!first) // do not execute for the first time
            {
                LoadUpXMLEventParameters(loadedData, ParametersMetricsListPicker.SelectedIndex,false);
            }
            else first = false;
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
                Uri targetUri = new Uri("/AppMetrics.xaml?appapikey=" + appapikey + "&apikey=" + apiKey + "&appName=" + appName +"&platform="+platform, UriKind.Relative);
                //ShellTile.Create(targetUri, secondaryTile); // Pass tileParameter as QueryString 
                this.Perform(() => LoadUpXMLAppMetricsForTile("ActiveUsers", targetUri, secondaryTile), 0);
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
        }

        private void compareOption_Click(object sender, EventArgs e)
        {

            if (MainPivot.SelectedIndex > 2) { return; } // do nothing for events - there's no compare

            Telerik.Windows.Controls.RadCartesianChart targetChart;
            Telerik.Windows.Controls.RadCartesianChart[] targetCharts = { chart1, chart2, chart3};
            RadCustomHubTile[] t1s = { tile1, tile4, tile7};
            RadCustomHubTile[] t2s = { tile2, tile5, tile8};
            RadCustomHubTile[] t3s = { tile3, tile6, tile9};
            TextBlock[] c1s = { change1, change4, change7 };
            TextBlock[] c2s = { change2, change5, change8};
            TextBlock[] c3s = { change3, change6, change9};
            TextBlock[] totals = { xtotal1, xtotal2, xtotal3};
            TextBlock[] d1s = { date1, date1_2, date1_3};
            TextBlock[] d2s = { date2, date2_2, date2_3};

            int s = MainPivot.SelectedIndex;

            targetChart = targetCharts[s];

            if (targetChart.Series[1].ItemsSource == null)
            {
                this.Perform(() => LoadUpXMLEventMetrics( StartDate2, EndDate2, 1), 1000);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    targetCharts[i].Series[1].ItemsSource = null;
                    totals[i].Visibility = System.Windows.Visibility.Collapsed;
                    t1s[i].IsFrozen = true;
                    t2s[i].IsFrozen = true;
                    t3s[i].IsFrozen = true;
                    d2s[i].Visibility = System.Windows.Visibility.Collapsed;
                    VisualStateManager.GoToState(t1s[i], "NotFlipped", true);
                    VisualStateManager.GoToState(t2s[i], "NotFlipped", true);
                    VisualStateManager.GoToState(t3s[i], "NotFlipped", true);
                }
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
             // In landscape mode, the totals grid is moved to the right on the screen
             // which puts it in row 1, column 1.
            if ((e.Orientation & PageOrientation.Landscape) != 0)
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
            }
            else
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
            }
        }

        

    } // class

    public class Data // all parameters w/ keys
    {
        public string key { get; set; }
        public IEnumerable<XElement> content { get; set; }
        public System.Collections.IEnumerable children { get; set; }
    }

}