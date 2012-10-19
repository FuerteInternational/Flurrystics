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

namespace Flurrystics
{
    public partial class PivotPage2 : PhoneApplicationPage
    {
        string apiKey;
        string appapikey = ""; // initial apikey of the app
        string appName = ""; // appName
        string eventName = ""; // eventName
        string EndDate;
        string StartDate;
        XDocument loadedData;
        ObservableCollection<AppViewModel> ParamKeys = new ObservableCollection<AppViewModel>();

        public PivotPage2()
        {
            InitializeComponent();
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
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
            NavigationContext.QueryString.TryGetValue("eventName", out eventName);
            String whatTitle = "FLURRYSTICS - " + appName + " - " + eventName;
            SubTitle.Text = whatTitle;
            Debug.WriteLine(whatTitle);
            ParamKeys.Clear();
            NoParameters.Visibility = System.Windows.Visibility.Collapsed;
            this.Perform(() => LoadUpXMLEventMetrics(), 1000);

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
                    

          if (ParamKeys.Count > 1)
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

        private void LoadUpXMLEventMetrics()
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
                   
                    LoadUpXMLEventParameters(loadedData,0,true);

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

                    tile1.Count = (int)latest;
                    tile2.Count = (int)minim;
                    tile3.Count = (int)maxim;
                    total1.Text = totalCount.ToString();
                    tile4.Count = (int)latest2;
                    tile5.Count = (int)minim2;
                    tile6.Count = (int)maxim2;
                    total2.Text = totalCount2.ToString();
                    tile7.Count = (int)latest3;
                    tile8.Count = (int)minim3;
                    tile9.Count = (int)maxim3;
                    total3.Text = totalCount3.ToString();
                    int setInterval = Util.getLabelInterval(DateTime.Parse(StartDate), DateTime.Parse(EndDate));
                    chart1.Series[0].ItemsSource = data;
                    chart1.HorizontalAxis.LabelInterval = setInterval;
                    chart2.Series[0].ItemsSource = data;
                    chart2.HorizontalAxis.LabelInterval = setInterval;
                    chart3.Series[0].ItemsSource = data;
                    chart3.HorizontalAxis.LabelInterval = setInterval;

                    }
                        catch (NotSupportedException) // it's not XML - probably API overload
                    {
                        //MessageBox.Show("Flurry API overload, please try again later.");
                    }

                });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            string callURL = "http://api.flurry.com/eventMetrics/Event?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + StartDate + "&endDate=" + EndDate + "&eventName=" + eventName;
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

    } // class

    public class Data // all parameters w/ keys
    {
        public string key { get; set; }
        public IEnumerable<XElement> content { get; set; }
        public System.Collections.IEnumerable children { get; set; }
    }

}