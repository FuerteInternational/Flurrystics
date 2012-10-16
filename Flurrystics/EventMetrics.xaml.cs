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

        public PivotPage2()
        {
            InitializeComponent();
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
            NavigationContext.QueryString.TryGetValue("eventName", out eventName);
            MainPivot.Title = "FLURRYSTICS - " + appName + " - " + eventName;

            this.Perform(() => LoadUpXMLEventMetrics(progressBar1), 1000);

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

        private void LoadUpXMLEventMetrics(Microsoft.Phone.Controls.PerformanceProgressBar progressBar)
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
                                   // <day uniqueUsers="378" totalSessions="3152" totalCount="6092" duration="0" date="2012-09-13"/>
                                   Value1 = (double)query.Attribute("uniqueUsers"),
                                   Value2 = (double)query.Attribute("totalSessions"),
                                   Value3 = (double)query.Attribute("totalCount"),
                                   // Value4 = (double)query.Attribute("duration"),
                                   Label = Util.stripOffYear(DateTime.Parse((string)query.Attribute("date")))
                               };

                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsIndeterminate = false;

                    chart1.DataSource = data;
                    chart2.DataSource = data;
                    chart3.DataSource = data;
                    //chart4.DataSource = data;
                    // MainListBox.ItemsSource = data;

                    }
                        catch (NotSupportedException e) // it's not XML - probably API overload
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

        private void ParametersMetricsListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ParametersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

    } // class

}