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
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.IO.IsolatedStorage;
using System.Windows.Navigation;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace Flurrystics
{
    public partial class Settings : PhoneApplicationPage
    {
        string error;
        int apiIndex;
        IsolatedStorageFile myFile = IsolatedStorageFile.GetUserStoreForApplication();
        string sFile = "Data.txt";
        ApiKeysContainer apiKeys = new ApiKeysContainer();

        public Settings()
        {
            InitializeComponent();
            myFile = IsolatedStorageFile.GetUserStoreForApplication();
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            NavigationContext.QueryString.TryGetValue("error", out error);
            apiIndex = 0;

                string pivotIndex;
                NavigationContext.QueryString.TryGetValue("pivotIndex", out pivotIndex);
                Debug.WriteLine("Settings:" + pivotIndex);
                apiIndex = int.Parse(pivotIndex);

            LoadApiKeyData();

            if (apiIndex >= 0)
            {
                apiKeyTextBox.Text = apiKeys.Strings[apiIndex]; 
            }

            if (error!=null) { ErrorBox.Visibility = System.Windows.Visibility.Visible; } else
                ErrorBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void SettingsSave_Click(object sender, EventArgs e)
        {
            //IsolatedStorageSettings.ApplicationSettings["apikey"] = apiKeyTextBox.Text;
            //IsolatedStorageSettings.ApplicationSettings.Save();

            if (apiIndex < 0) // add new account
            {
                apiKeys.Strings.Add(apiKeyTextBox.Text);
                apiKeys.Names.Add("loading...");
            }
            else
            {
                apiKeys.Strings[apiIndex] = apiKeyTextBox.Text;
            }

            SaveApiKeyData();

            NavigationService.GoBack();
        }

        private void SettingsCancel_Click(object sender, EventArgs e)
        { // do not store anything - just go backj
            NavigationService.GoBack();
        }

        private void FlurryWebJump_Click(object sender, RoutedEventArgs e)
        {
            var task = new Microsoft.Phone.Tasks.WebBrowserTask
            {
                URL = "https://dev.flurry.com/form.do"
            };

            task.Show();
        }

        private void codeTextChanged(object sender, TextChangedEventArgs e)
        {
            // Save cursor's position
            int cursorLocation = apiKeyTextBox.SelectionStart;

            // Uppercase text
            apiKeyTextBox.Text = apiKeyTextBox.Text.ToUpper();

            // Restore cursor's position
            apiKeyTextBox.SelectionStart = cursorLocation;
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
            catch (InvalidOperationException) { }

            reader.Close();

        }

        private void SaveApiKeyData()
        {
            //myFile.DeleteFile(sFile);
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

    }
}