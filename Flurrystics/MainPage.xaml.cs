using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using System.Xml.Linq;

namespace Flurrystics
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            //DataContext = App.ViewModel;
            //this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            var w = new WebClient();

            Observable
            .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
            .Subscribe(r =>
             {

            XDocument loadedData = XDocument.Parse(r.EventArgs.Result);
            //XDocument loadedData = XDocument.Load("getAllApplications.xml");

            PageTitle.Text = (string)loadedData.Root.Attribute("companyName");

            var data = from query in loadedData.Descendants("application")
                       select new AppViewModel
                       {
                           LineOne = (string)query.Attribute("name"),
                           LineTwo = (string)query.Attribute("platform") + ", created: " + (string)query.Attribute("createdDate"),
                           LineFour = (string)query.Attribute("apiKey")
                       };

            progressBar1.Visibility = System.Windows.Visibility.Collapsed;

            MainListBox.ItemsSource = data;

             });
            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            w.DownloadStringAsync(
                new Uri("http://api.flurry.com/appInfo/getAllApplications?apiAccessCode=DJBUBP9NE5YBQB5CQKH3")
                );

        }

        private bool InternetIsAvailable()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("Internet connection not available. Please try again later.");
                return false;
            }
            return true;
        }

        // Handle selection changed on ListBox
        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (MainListBox.SelectedIndex == -1)
                return;

            // Navigate to the new page
            AppViewModel selected = (AppViewModel)MainListBox.Items[MainListBox.SelectedIndex];
            NavigationService.Navigate(new Uri("/AppMetrics.xaml?apikey=" + selected.LineFour, UriKind.Relative));
                
                // .SelectedIndex, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            MainListBox.SelectedIndex = -1;
        }

        // Load data for the ViewModel Items
        /*
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }*/

    }
}