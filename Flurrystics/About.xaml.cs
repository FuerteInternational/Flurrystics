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

namespace Flurrystics
{
    public partial class AboutPage1 : PhoneApplicationPage
    {
        public AboutPage1()
        {
            InitializeComponent();
        }

        private void FuerteWebJump_Click(object sender, RoutedEventArgs e)
        {
            var task = new Microsoft.Phone.Tasks.WebBrowserTask
            {
                URL = "http://www.fuerteint.com"
            };

            task.Show();
        }
    }
}