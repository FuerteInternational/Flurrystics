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
using System.IO.IsolatedStorage;

namespace Flurrystics
{
    public partial class Settings : PhoneApplicationPage
    {

        string apiKey;

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

    }
}