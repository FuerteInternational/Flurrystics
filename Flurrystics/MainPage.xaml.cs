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
using System.IO;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.ObjectModel;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using Microsoft.Phone.Scheduler;

namespace Flurrystics
{
    public partial class MainPage : PhoneApplicationPage
    {

        IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
        string sFile = "Data.txt"; // 
        ApiKeysContainer apiKeys = new ApiKeysContainer();
        ObservableCollection<AppViewModel> PivotItems = new ObservableCollection<AppViewModel>();
        private int lastPivotItemCount = 0;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            myFile = IsolatedStorageFile.GetUserStoreForApplication();
            
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            FlurryWP7SDK.Api.LogEvent("MainPage");
            LoadApiKeyData();
            MainPivot.ItemsSource = null;
            PivotItems.Clear();
            foreach (string info in apiKeys.Names)
            {
                PivotItems.Add(new AppViewModel{ LineOne = info });
            }
                MainPivot.ItemsSource = PivotItems;
                if (lastPivotItemCount != PivotItems.Count) 
                {
                    
                    if (lastPivotItemCount==0) { // on startup
                        Debug.WriteLine("Setting first position in Pivot");
                        lastPivotItemCount = PivotItems.Count;
                        MainPivot.SelectedIndex = 0;
                    }
                    else 
                        {
                            Debug.WriteLine("Setting last position in Pivot");
                            lastPivotItemCount = PivotItems.Count;
                            MainPivot.SelectedIndex = lastPivotItemCount - 1;
                        }
                    
                }

                this.Perform(() => LoadUpXML(MainPivot.SelectedIndex), 1000, 1000);

            // StartPeriodicAgent();
                
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
                        tileToUpdate.BackTitle = tileToUpdate.Title;
                        tileToUpdate.BackContent = "Yesterday: " + result + " Active Users";
                    }
                }
                catch (NotSupportedException) // it's not XML - probably API overload
                {
                    Debug.WriteLine("Flurry API overload");
                    /*
                    ShellToast backgroundToast = new ShellToast();
                    backgroundToast.Title = "Flurrysticks";
                    backgroundToast.Content = "Flurry API overload";
                    backgroundToast.Show();
                     * */
                }

                ShellTile.Create(targetUri, tileToUpdate); // create Tile NO MATTER WHAT 

            });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            string callURL = "http://api.flurry.com/appMetrics/" + metrics + "?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + sDate + "&endDate=" + eDate;
            Debug.WriteLine("Calling URL:" + callURL);
            w.DownloadStringAsync(new Uri(callURL));

        }

        private void LoadApiKeyData()
        {
            //myFile.DeleteFile(sFile);
            if (!myFile.FileExists(sFile))
            {
                IsolatedStorageFileStream dataFile = myFile.CreateFile(sFile);
                dataFile.Close();
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ApiKeysContainer));

            //Reading and loading data
            StreamReader reader = new StreamReader(new IsolatedStorageFileStream(sFile, FileMode.Open, myFile));

            try
            {
                apiKeys = (ApiKeysContainer)serializer.Deserialize(reader);
            }
            catch (InvalidOperationException) // XML doesnt exists probably - redirect user to settings to add ONE
            {
                showErrorPanel(0,-3);
                // NavigationService.Navigate(new Uri("/Settings.xaml?pivotIndex=-3", UriKind.Relative));
            }

            reader.Close();

            if (apiKeys.Names.Count==0)
            {
                showErrorPanel(0, -3);
            }

        }

        private void SaveApiKeyData()
        {
            myFile.DeleteFile(sFile);
            if (!myFile.FileExists(sFile))
            {
                IsolatedStorageFileStream dataFile = myFile.CreateFile(sFile);
                dataFile.Close();
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ApiKeysContainer));

            //Reading and loading data
            StreamWriter writer = new StreamWriter(new IsolatedStorageFileStream(sFile, FileMode.OpenOrCreate, myFile));

            serializer.Serialize(writer, apiKeys); // this is for save

            // apiKeys = (ApiKeysContainer)serializer.Deserialize(writer); // this is for load

            writer.Close();

        }

        private T FindFirstElementInVisualTree<T>(DependencyObject parentElement) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parentElement);
            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parentElement, i);

                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    var result = FindFirstElementInVisualTree<T>(child);
                    if (result != null)
                        return result;

                }
            }
            return null;
        }

        private void LoadUpXML(int pivotIndex)
        {
            Debug.WriteLine("LoadUpXML: " + pivotIndex);
            App.lastRequest = Util.getCurrentTimestamp();

            // if (Util.InternetIsAvailable())  { // if Internet is available - go download and process our feed

            var w = new WebClient();
            Observable
            .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
            .Subscribe(r =>
            {
                PivotItem pi = null;
                ProgressBar p = null;
                XDocument loadedData = null;
                try
                {
                    try
                    {
                        loadedData = XDocument.Parse(r.EventArgs.Result);
                    }
                    catch (WebException) // load failed, probably wrong apiKey - goto settings
                    {
                        pi = (PivotItem)MainPivot.ItemContainerGenerator.ContainerFromIndex(pivotIndex); // got out pivot item
                        p = FindFirstElementInVisualTree<ProgressBar>(pi);
                        p.Visibility = System.Windows.Visibility.Collapsed;
                        showErrorPanel(1, pivotIndex);
                        //NavigationService.Navigate(new Uri("/Settings.xaml?error=yes&pivotIndex="+pivotIndex, UriKind.Relative));
                    }

                    if (loadedData != null)
                    {

                        // XDocument loadedData = XDocument.Load("getAllApplications.xml");
                        pi = (PivotItem)MainPivot.ItemContainerGenerator.ContainerFromIndex(pivotIndex); // got out pivot item
                        p = FindFirstElementInVisualTree<ProgressBar>(pi);
                        // PivotItem item =  (PivotItem)MainPivot.ItemContainerGenerator.ContainerFromIndex(0);
                        // pi.Header = (string)loadedData.Root.Attribute("companyName");
                        string cName = (string)loadedData.Root.Attribute("companyName");
                        PivotItems.ElementAt(pivotIndex).LineOne = cName;
                        apiKeys.Names[pivotIndex] = cName; 
                        var data = from query in loadedData.Descendants("application")
                                   orderby (string)query.Attribute("name")
                                   select new AppViewModel
                                   {
                                       LineOne = (string)query.Attribute("name"),
                                       LineTwo = (string)query.Attribute("platform"),
                                       LineThree = DateTime.Parse((string)query.Attribute("createdDate")).ToLongDateString(),
                                       LineFive = getIconFileForPlatform(((String)query.Attribute("platform")).Trim()),
                                       LineFour = (string)query.Attribute("apiKey")
                                   };

                        
                        // now lets find out ListBox and ProgressBar
                        ListBox l = FindFirstElementInVisualTree<ListBox>(pi);
                        p.Visibility = System.Windows.Visibility.Collapsed;
                        p.IsIndeterminate = false; // switch off so it doesn't hit performance when not visible (!)
                        l.ItemsSource = data;
                        errorPanel.Visibility = System.Windows.Visibility.Collapsed;
                        this.Perform(() => SaveApiKeyData(), 100, 100);
                        //
                    }
                }
                catch (NotSupportedException)
                {
                    MessageBox.Show("Flurry API overload, please try again later."); // should not happen - EVER (however it may happen if more clients (devices) access one APIkey)
                    //p.Visibility = System.Windows.Visibility.Collapsed;
                    //showErrorPanel(1, pivotIndex);
                }

            });

                w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
                try
                {
                    w.DownloadStringAsync(
                        new Uri("http://api.flurry.com/appInfo/getAllApplications?apiAccessCode=" + apiKeys.Strings[MainPivot.SelectedIndex])
                        );
                }
                catch (ArgumentOutOfRangeException)
                { // probably nothing yet specified
                }

            /*
            } else {
                Debug.WriteLine("No Internet");
            }
             */
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
                    output = "Images/flurryst_iconwindows.png";
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

        private void Perform(Action myMethod, int delayInMilliseconds, int constDelay)
        {

            long diff = Util.getCurrentTimestamp() - App.lastRequest;
            int throttledDelay = 0;

            if (diff < delayInMilliseconds) // if delay between requests is less then second then count time we need to wait before firing up next request
            {
                throttledDelay = (int)diff;
            }

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) => Thread.Sleep(throttledDelay + constDelay);
            worker.RunWorkerCompleted += (s, e) => myMethod.Invoke();
            worker.RunWorkerAsync();

        }

        // Handle selection changed on ListBox
        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox current = (ListBox)sender;
            // If selected index is -1 (no selection) do nothing
            if (current.SelectedIndex == -1)
                return;

            // Navigate to the new page
            AppViewModel selected = (AppViewModel)current.Items[current.SelectedIndex];
            NavigationService.Navigate(new Uri("/AppMetrics.xaml?appapikey=" + selected.LineFour + "&apikey=" + apiKeys.Strings[MainPivot.SelectedIndex] + "&appName=" + selected.LineOne+"&platform="+selected.LineTwo, UriKind.Relative));               
            // .SelectedIndex, UriKind.Relative));
            // Reset selected index to -1 (no selection)
            current.SelectedIndex = -1;
        }

        private void SettingsOption_Click(object sender, EventArgs e)
        {
            if (PivotItems.Count() > 0)
            {
                errorPanel.Visibility = System.Windows.Visibility.Collapsed;
                NavigationService.Navigate(new Uri("/Settings.xaml?pivotIndex=" + MainPivot.SelectedIndex, UriKind.Relative));
            }
            else
            {
                MessageBox.Show("Nothing to edit. Please add some account first.");
            }
        }

        private void SettingsOptionAdd_Click(object sender, EventArgs e)
        {
            errorPanel.Visibility = System.Windows.Visibility.Collapsed;
            NavigationService.Navigate(new Uri("/Settings.xaml?pivotIndex=-2", UriKind.Relative));
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private bool first = true;
        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!first)
            {
                Pivot current = (Pivot)sender;
                if (current.Items.Count > 0)
                {
                    this.Perform(() => LoadUpXML(current.SelectedIndex), 1000,0);
                }
            }
            else first = false;
        }

        private void DeleteOption_Click(object sender, EventArgs e)
        {

            if (PivotItems.Count() > 0)
            {

                int selected = MainPivot.SelectedIndex;
                //PivotItem selectedItem = (PivotItem)MainPivot.Items[selected];
                string deletedAccountName = PivotItems[selected].LineOne;
                MessageBoxResult m = MessageBox.Show("Are you sure you want to remove account: " + deletedAccountName, "Confirm flurry account removal", MessageBoxButton.OKCancel);

                if (m == MessageBoxResult.OK)
                { // yes - we gonna delete that account!
                    //ApiKeysContainer apiKeys = new ApiKeysContainer();
                    apiKeys.Strings.RemoveAt(selected);
                    apiKeys.Names.RemoveAt(selected);
                    //ObservableCollection<AppViewModel> PivotItems = new ObservableCollection<AppViewModel>();
                    PivotItems.RemoveAt(selected);
                    if (lastPivotItemCount > 0) { lastPivotItemCount--; }
                    errorPanel.Visibility = System.Windows.Visibility.Collapsed;
                    SaveApiKeyData();

                    if (lastPivotItemCount == 0)
                    {
                        showErrorPanel(0, -3);
                    }

                    /*
                    // If the Main App is Running, Toast will not show
                    ShellToast popupMessage = new ShellToast()
                    {
                        Title = "Flurrysticks",
                        Content = "Account "+deletedAccountName+" removed."
                        // NavigationUri = new Uri("/Views/DeepLink.xaml", UriKind.Relative)
                    };
                    popupMessage.Show();
                    */

                }

            }
            else
            {
                MessageBox.Show("Nothing to delete.  Please add some account first.");
            }

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) // clicking on context menu item
        {
            PivotItem selectedPivotItem = MainPivot.ItemContainerGenerator.ContainerFromIndex(MainPivot.SelectedIndex) as PivotItem;
            ListBox selectedListBox = FindFirstElementInVisualTree<ListBox>(selectedPivotItem);
            ListBoxItem selectedListBoxItem = selectedListBox.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            //ListBoxItem contextMenuListItem = selectedListBox.ItemContainerGenerator.ContainerFromItem((sender as ContextMenu).DataContext) as ListBoxItem;
            TextBlock selectedTitle = FindFirstElementInVisualTree<TextBlock>(selectedListBoxItem);
            string tileParameter = selectedTitle.Text; // selectedListBoxItem.Name; // "Param=" + ((Button)sender).Name;//Use Button.Name to mark Tile 
            int selectedIndex = selectedListBox.Items.IndexOf(selectedListBoxItem.DataContext);
            AppViewModel selected = (AppViewModel)selectedListBox.Items[selectedIndex];
            ShellTile tile = CheckIfTileExist(selected.LineFour);// Check if Tile's title has been used  
            if (tile == null)
            {
                StandardTileData secondaryTile = new StandardTileData
                {
                    Title = Util.shrinkString(tileParameter),
                    BackgroundImage = new Uri("Background2.png", UriKind.Relative),
                    //Count = 0,
                    //BackContent = "Secondary Tile Test"
                    //BackContent = "Platform: " + selected.LineTwo,
                    //BackTitle = tileParameter

                };                
                Uri targetUri = new Uri("/AppMetrics.xaml?appapikey=" + selected.LineFour + "&apikey=" + apiKeys.Strings[MainPivot.SelectedIndex] + "&appName=" + selected.LineOne+"&platform="+selected.LineTwo, UriKind.Relative); 
                //TO-DO: call create shelltile
                if (Util.Is512Mb)
                {
                    this.Perform(() => LoadUpXMLAppMetricsForTile("ActiveUsers", targetUri, secondaryTile), 0, 0);
                }
                else // put standard shelltile for 256devices
                {
                    ShellTile.Create(targetUri, secondaryTile); // Pass tileParameter as QueryString 
                }
            }
            else
            {
                MessageBox.Show("Tile " + tileParameter + " already exists on homescreen.");
            }

        }

        private ShellTile CheckIfTileExist(string tileUri)
        {
            ShellTile shellTile = ShellTile.ActiveTiles.FirstOrDefault(
                    tile => tile.NavigationUri.ToString().Contains(tileUri));
            return shellTile;
        }

        private int errorPivotIndex = -3;

        private void showErrorPanel(int errorType, int pivotIndex)
        {
            if ((pivotIndex != MainPivot.SelectedIndex) && (MainPivot.Items.Count>0)) { return; } 

            errorPanel.Visibility = System.Windows.Visibility.Visible;         
            MainPivot.IsEnabled = false;
            errorPivotIndex = pivotIndex;
            switch (errorType)
            {
                case 0: // no account
                    errorStatus.Text = "Please go to Settings and add at least one valid Flurry API key.";
                    RetryButton.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 1: // account error
                    errorStatus.Text = "Not valid Flurry API key present or Internet connection has been lost.";
                    RetryButton.Visibility = System.Windows.Visibility.Visible;
                    break;
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            this.Perform(() => LoadUpXML(MainPivot.SelectedIndex), 1000, 1000);
            errorPanel.Visibility = System.Windows.Visibility.Collapsed;
            MainPivot.IsEnabled = true;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            errorPanel.Visibility = System.Windows.Visibility.Collapsed;
            MainPivot.IsEnabled = true;
            NavigationService.Navigate(new Uri("/Settings.xaml?error=yes&pivotIndex=" + errorPivotIndex, UriKind.Relative));
        }

    }

    [XmlRoot]
    public class ApiKeysContainer
    {
        private List<string> strings = new List<string>();
        public List<string> Strings { get { return strings; } set { strings = value; } }

        private List<string> names = new List<string>();
        public List<string> Names { get { return names; } set { names = value; } }
    }

    public class MyPivotItems
    {
        public string MyPivotItem
        {
            get;
            set;
        }

    }

}