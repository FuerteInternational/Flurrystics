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

namespace Flurrystics
{
    public partial class Settings : PhoneApplicationPage
    {

        string apiKey;
        string error;

        public Settings()
        {
            InitializeComponent();
            try
            {
                apiKey = (string)IsolatedStorageSettings.ApplicationSettings["apikey"];
            }
            catch (KeyNotFoundException)
            {
                apiKey = "no-api-key";
            }
            apiKeyTextBox.Text = apiKey;
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            NavigationContext.QueryString.TryGetValue("error", out error);

            if (error!=null) { ErrorBox.Visibility = System.Windows.Visibility.Visible; } else
                ErrorBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void SettingsSave_Click(object sender, EventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["apikey"] = apiKeyTextBox.Text;
            IsolatedStorageSettings.ApplicationSettings.Save();
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

    }
}