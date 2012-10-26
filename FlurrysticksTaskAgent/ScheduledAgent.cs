using System.Windows;
using System;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using Microsoft.Phone.Reactive;
using System.Xml.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows.Navigation;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.ObjectModel;

namespace FlurrysticksTaskAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }
        
        private void Perform(Action myMethod, int delayInMilliseconds)
        {
            Debug.WriteLine("Perform " + myMethod.ToString());
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, e) => Thread.Sleep(delayInMilliseconds);
            worker.RunWorkerCompleted += (s, e) => myMethod.Invoke();
            worker.RunWorkerAsync();

        }

        public long getCurrentTimestamp()
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            DateTime currentDate = DateTime.Now;
            long totalMiliSecond = (currentDate.Ticks - unixEpoch.Ticks) / 10000;
            return totalMiliSecond;
        }

        // http://api.flurry.com/appMetrics/ActiveUsers?apiAccessCode=DJBUBP9NE5YBQB5CQKH3&apiKey=HXCWZ1L3CWMVGQM68JPI&startDate=2012-10-24&endDate=2012-10-24

        private void LoadUpXMLAppMetricsForTile(string metrics, ShellTile tileToUpdate, bool last)
        {
            string eDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-1));
            string sDate = eDate;
            //string sDate = String.Format("{0:yyyy-MM-dd}", DateTime.Now.AddDays(-2));
            char[] splitChars1 = { '?' };
            string[] parameters = tileToUpdate.NavigationUri.ToString().Split(splitChars1);

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
                        var newTileData = new StandardTileData
                        {
                            // TO-DO: call to fetch actual data
                            BackTitle = "Active Users",
                            //Count = rand.Next(0,99),
                            BackContent = "Yesterday: "+ result
                        };
                        tileToUpdate.Update(newTileData);
                        if (last) {
                            Debug.WriteLine("background tasks COMPLETE");
                            NotifyComplete(); 
                        }
                    }
                  
                }
                catch (NotSupportedException) // it's not XML - probably API overload
                {
                    ShellToast backgroundToast = new ShellToast();
                    backgroundToast.Title = "Flurrysticks";
                    backgroundToast.Content = "Flurry API overload";
                    backgroundToast.Show();
                }

            });

            w.Headers[HttpRequestHeader.Accept] = "application/xml"; // get us XMLs version!
            string callURL = "http://api.flurry.com/appMetrics/" + metrics + "?apiAccessCode=" + apiKey + "&apiKey=" + appapikey + "&startDate=" + sDate + "&endDate=" + eDate;
            Debug.WriteLine("Calling URL:" + callURL);
            w.DownloadStringAsync(new Uri(callURL));

        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {

            Debug.WriteLine("OnInvoke PeriodicTask");

            // test code
            /*
            ShellToast backgroundToast = new ShellToast();
            backgroundToast.Title = "Scheduled Task";
            backgroundToast.Content = "Running...";
            backgroundToast.Show();
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(5));
            */

            if (task is PeriodicTask)
            {

                //TODO: Add code to perform your task in background          
                // var tileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == "/");
                IEnumerator<ShellTile> tilesEnum = ShellTile.ActiveTiles.GetEnumerator();
                int howmany = ShellTile.ActiveTiles.Count() - 1; // decrease by 1 because of primary tile
                while (tilesEnum.MoveNext()) // loop to update each and every live tile
                {
                    ShellTile currentTile = tilesEnum.Current;
                    Debug.WriteLine("processing tile: " + currentTile.NavigationUri.ToString());
                    var rand = new Random(0);
                    int count = 0;

                    if (currentTile.NavigationUri.ToString() != "/") // ignore primare tile, only deal w/ secondary tiles
                    {
                        /*
                        var newTileData = new StandardTileData
                        {
                            // TO-DO: call to fetch actual data
                            BackTitle = "Backtile", 
                            //Count = rand.Next(0,99),
                            BackContent = "Yesterday: 88" 
                        };
                         * */
                        bool isItLastOne = ((howmany-1) == count);
                        this.Perform(() => LoadUpXMLAppMetricsForTile("ActiveUsers", currentTile, isItLastOne), count * 2000);
                        
                        Debug.WriteLine("Updated " + DateTime.Now.ToShortTimeString());
                        count++;
                     } 
                 }

                //ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(10));

            }
            
            //NotifyComplete();

        }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; } // appmetrics
        public double Value1 { get; set; } // Unique Users
        public double Value2 { get; set; } // Total Sessions
        public double Value3 { get; set; } //
        public double Value4 { get; set; }
    }

}