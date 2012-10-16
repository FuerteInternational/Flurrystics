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
using System.IO.IsolatedStorage;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Threading;

namespace Flurrystics
{
    public partial class MainPage : PhoneApplicationPage
    {

        string apiKey;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

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

            // Set the data context of the listbox control to the sample data
            //DataContext = App.ViewModel;
            //this.Loaded += new RoutedEventHandler(MainPage_Loaded)
            this.Perform(() => LoadUpXML(), 1000);
        }

        private void LoadUpXML()
        {
            App.lastRequest = Util.getCurrentTimestamp();
            var w = new WebClient();
            Observable
            .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
            .Subscribe(r =>
            {
                XDocument loadedData = null;
                try
                {
                    try
                    {
                        loadedData = XDocument.Parse(r.EventArgs.Result);
                    }
                    catch (WebException) // load failed, probably wrong apiKey - goto settings
                    {
                        NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
                    }

                    if (loadedData != null)
                    {

                        //XDocument loadedData = XDocument.Load("getAllApplications.xml");
                        PageTitle.Text = (string)loadedData.Root.Attribute("companyName");
                        var data = from query in loadedData.Descendants("application")
                                   orderby (string)query.Attribute("name")
                                   select new AppViewModel
                                   {
                                       LineOne = (string)query.Attribute("name"),
                                       LineTwo = (string)query.Attribute("platform"),
                                       LineThree = DateTime.Parse((string)query.Attribute("createdDate")).ToLongDateString(),
                                       LineFive = getIconFileForPlatform((string)query.Attribute("platform")),
                                       LineFour = (string)query.Attribute("apiKey")
                                   };
                        progressBar1.Visibility = System.Windows.Visibility.Collapsed;
                        progressBar1.IsIndeterminate = false; // switch off so it doesn't hit performance when not visible (!)
                        MainListBox.ItemsSource = data;
                    }
                }
                catch (NotSupportedException)
                {
                    MessageBox.Show("Flurry API overload, please try again later."); // should not happen - EVER
                }

            });

            if (Util.InternetIsAvailable()) // if Internet is available - go download and process our feed
            {
                w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
                w.DownloadStringAsync(
                    new Uri("http://api.flurry.com/appInfo/getAllApplications?apiAccessCode=" + apiKey)
                    );

            }
        }

        private string getIconFileForPlatform(string input) {
            string output = "Images/flurryst_iconapple.png";
            switch (input)
            {
                case "iPhone":
                case "iPad":
                    output = "Images/flurryst_iconapple.png";
                    break;
                case "Android":
                    output = "Images/flurryst_iconandroid.png";
                    break;
                case "WindowsPhone":
                    output = "Images/flurrst_iconwindows.png";
                    break;
                case "BlackberrySDK":
                    output = "Images/flurryst_iconblackberry.png";
                    break;
                case "JavaMESDK":
                    output = "Images/flurryst_iconjava.png";
                    break;

            }
        return output;
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


        // Handle selection changed on ListBox
        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (MainListBox.SelectedIndex == -1)
                return;

            // Navigate to the new page
            AppViewModel selected = (AppViewModel)MainListBox.Items[MainListBox.SelectedIndex];
            NavigationService.Navigate(new Uri("/AppMetrics.xaml?apikey=" + selected.LineFour + "&appName=" + selected.LineOne, UriKind.Relative));
                
                // .SelectedIndex, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            MainListBox.SelectedIndex = -1;
        }

        private void SettingsOption_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
            //Do work for your application here.
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