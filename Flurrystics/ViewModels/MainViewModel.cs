using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using System.Xml.Linq;

namespace Flurrystics
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.Items = new ObservableCollection<AppViewModel>();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<AppViewModel> Items { get; private set; }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {

            XDocument loadedData = XDocument.Load("getAllApplications.xml");

            var data = from query in loadedData.Descendants("person")
                       select new AppViewModel
                       {
                           LineOne = (string)query.Attribute("name"),
                           LineTwo = (string)query.Attribute("platform")
                       };

            /*
            var w = new WebClient();

            Observable
            .FromEvent<DownloadStringCompletedEventArgs>(w, "DownloadStringCompleted")
            .Subscribe(r =>
             {

                 XDocument loadedData = XDocument.Parse(r.EventArgs.Result);

                       var data = from query in loadedData.Descendants("application")
                       select new ItemViewModel()
                       {
                           /*
                           FirstName = (string)query.Element("firstname"),
                           LastName = (string)query.Element("lastname"),
                           Age = (int)query.Element("age")
                       
                       };
            */

            //this.Items = data;
            //listBox.ItemsSource = data;
                 
             // });

            /*
            w.DownloadStringAsync(
              new Uri("http://api.flurry.com/appInfo/getAllApplications?apiAccessCode=DJBUBP9NE5YBQB5CQKH3")
              );
            */

            // Sample data; replace with real data
            /*
            this.Items.Add(new ItemViewModel() { LineOne = "runtime one", LineTwo = "Maecenas praesent accumsan bibendum", LineThree = "Facilisi faucibus habitant inceptos interdum lobortis nascetur pharetra placerat pulvinar sagittis senectus sociosqu" });
            */

            this.IsDataLoaded = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }




    }
}