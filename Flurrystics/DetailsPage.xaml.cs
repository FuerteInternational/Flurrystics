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

    public partial class DetailsPage : PhoneApplicationPage
    {
        // Constructor
        public DetailsPage()
        {
            InitializeComponent();
            // ContentText.Text = StartDate + " - " + EndDate;
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string apikey = "";
            if (NavigationContext.QueryString.TryGetValue("apikey", out apikey))
            {
                // ListTitle.Text = apikey;
                // http://api.flurry.com/appMetrics/ActiveUsers?apiAccessCode=DJBUBP9NE5YBQB5CQKH3&apiKey=HXCWZ1L3CWMVGQM68JPI&startDate=2012-09-01&endDate=2012-10-01
                string EndDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now);
                string StartDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-14));
                String queryURL = StartDate + " - " + EndDate;
                var w = new WebClient();

                Observable
                .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
                .Subscribe(r =>
                {

                    XDocument loadedData = XDocument.Parse(r.EventArgs.Result);
                    //XDocument loadedData = XDocument.Load("getAllApplications.xml");

                    ListTitle.Text = (string)loadedData.Root.Attribute("metric");
                    var data = from query in loadedData.Descendants("day")
                               select new ChartDataPoint
                               {
                                   Value = (double)query.Attribute("value"),
                                   Label = (string)query.Attribute("date")
                               };

                    // progressBar1.Visibility = System.Windows.Visibility.Collapsed;
                    chart1.DataSource = data;
                    // MainListBox.ItemsSource = data;
                });
                w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
                w.DownloadStringAsync(
                    new Uri("http://api.flurry.com/appMetrics/ActiveUsers?apiAccessCode=DJBUBP9NE5YBQB5CQKH3&apiKey="+apikey+"&startDate="+StartDate+"&endDate="+EndDate)
                    );
                
            }
        }

        private void DetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;
        }

    }

}