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

namespace Flurrystics
{
    public partial class MainPage : PhoneApplicationPage
    {

        IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
        string sFile = "Data.txt";
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
            //first = true;
            foreach (string info in apiKeys.Names)
            {
                PivotItems.Add(new AppViewModel{ LineOne = info });
            }

            if (!(PivotItems.Count > 0)) // if no api key present - send user to settings
            {
                NavigationService.Navigate(new Uri("/Settings.xaml?pivotIndex=-3", UriKind.Relative));
            }
            else
            {
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
            }
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
                NavigationService.Navigate(new Uri("/Settings.xaml?pivotIndex=-3", UriKind.Relative));
            }

            reader.Close();
          
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
                        NavigationService.Navigate(new Uri("/Settings.xaml?error=yes&pivotIndex="+pivotIndex, UriKind.Relative));
                    }

                    if (loadedData != null)
                    {

                        //XDocument loadedData = XDocument.Load("getAllApplications.xml");
                        PivotItem pi = (PivotItem)MainPivot.ItemContainerGenerator.ContainerFromIndex(pivotIndex); // got out pivot item
                        //PivotItem item =  (PivotItem)MainPivot.ItemContainerGenerator.ContainerFromIndex(0);
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
                        ProgressBar p = FindFirstElementInVisualTree<ProgressBar>(pi);

                        p.Visibility = System.Windows.Visibility.Collapsed;
                        p.IsIndeterminate = false; // switch off so it doesn't hit performance when not visible (!)
                        l.ItemsSource = data;
                        this.Perform(() => SaveApiKeyData(), 100, 100);
                        //
                    }
                }
                catch (NotSupportedException)
                {
                    MessageBox.Show("Flurry API overload, please try again later."); // should not happen - EVER (however it may happen if more clients (devices) access one APIkey)
                }

            });

            if (Util.InternetIsAvailable()) // if Internet is available - go download and process our feed
            {
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

            }
            else
            {
                throw new Util.ExitException();
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
            NavigationService.Navigate(new Uri("/Settings.xaml?pivotIndex="+MainPivot.SelectedIndex, UriKind.Relative));
        }

        private void SettingsOptionAdd_Click(object sender, EventArgs e)
        {
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
            int selected = MainPivot.SelectedIndex;
            //PivotItem selectedItem = (PivotItem)MainPivot.Items[selected];
            MessageBoxResult m = MessageBox.Show("Are you sure you want to remove account: "+PivotItems[selected].LineOne, "Confirm flurry account removal", MessageBoxButton.OKCancel);

            if (m == MessageBoxResult.OK)
            { // yes - we gonna delete that account!
                //ApiKeysContainer apiKeys = new ApiKeysContainer();
                apiKeys.Strings.RemoveAt(selected);
                apiKeys.Names.RemoveAt(selected);
                //ObservableCollection<AppViewModel> PivotItems = new ObservableCollection<AppViewModel>();
                PivotItems.RemoveAt(selected);
                if (lastPivotItemCount > 0) { lastPivotItemCount--; }
                SaveApiKeyData();
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
                ShellTile.Create(targetUri, secondaryTile); // Pass tileParameter as QueryString 
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