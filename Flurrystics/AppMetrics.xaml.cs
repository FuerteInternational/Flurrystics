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

namespace Flurrystics
{
    public partial class PivotPage1 : PhoneApplicationPage
    {

        string apikey = ""; // initial apikey of the app
        WebClient w;

        public PivotPage1()
        {
            InitializeComponent();
            w = new WebClient();
        }

                // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {          
            NavigationContext.QueryString.TryGetValue("apikey", out apikey);
        }

        private void LoadUpXML(string metrics, AmCharts.Windows.QuickCharts.SerialChart targetChart, Microsoft.Phone.Controls.PerformanceProgressBar progressBar)
        {
            string EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            string StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-31));
            String queryURL = StartDate + " - " + EndDate;
            

            Observable
            .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
            .Subscribe(r =>
            {

                XDocument loadedData = XDocument.Parse(r.EventArgs.Result);
                //XDocument loadedData = XDocument.Load("getAllApplications.xml");

                // ListTitle.Text = (string)loadedData.Root.Attribute("metric");
                var data = from query in loadedData.Descendants("day")
                           select new ChartDataPoint
                           {
                               Value = (double)query.Attribute("value"),
                               Label = (string)query.Attribute("date")
                           };

                progressBar.Visibility = System.Windows.Visibility.Collapsed;
                progressBar.IsIndeterminate = false;

                targetChart.DataSource = data;
                // MainListBox.ItemsSource = data;
            });
            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            w.DownloadStringAsync(
                new Uri("http://api.flurry.com/appMetrics/"+metrics+"?apiAccessCode=DJBUBP9NE5YBQB5CQKH3&apiKey=" + apikey + "&startDate=" + StartDate + "&endDate=" + EndDate)
                );
        }


        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (MainPivot.SelectedIndex)
            {
                case 0:     //ActiveUsers
                    LoadUpXML("ActiveUsers", chart1, progressBar1);
                    break;
                case 1:     //ActiveUsersByWeek
                    LoadUpXML("ActiveUsersByWeek", chart2, progressBar2);
                    break;
                case 2:     //ActiveUsers
                    LoadUpXML("ActiveUsersByMonth", chart3, progressBar3);
                    break;
                case 3:     //ActiveUsersByWeek
                    LoadUpXML("NewUsers", chart4, progressBar4);
                    break;
                case 4:     //ActiveUsers
                    LoadUpXML("MedianSessionLength", chart5, progressBar5);
                    break;
                case 5:     //ActiveUsersByWeek
                    LoadUpXML("AvgSessionLength", chart6, progressBar6);
                    break;
                case 6:     //ActiveUsers
                    LoadUpXML("Sessions", chart7, progressBar7);
                    break;
                case 7:     //ActiveUsersByWeek
                    LoadUpXML("RetainedUsers", chart8, progressBar8);
                    break;

            } // switch
        }



    } // class

   public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }

}