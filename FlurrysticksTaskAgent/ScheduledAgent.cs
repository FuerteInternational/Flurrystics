using System.Windows;
using System;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using System.Collections.Generic;

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

            ShellToast backgroundToast = new ShellToast();
            backgroundToast.Title = "Scheduled Task";
            backgroundToast.Content = "Running...";
            backgroundToast.Show();
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(5));

            if (task is PeriodicTask)
            {

                //TODO: Add code to perform your task in background

                /*
                // var tileToFind = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == "/");
                IEnumerator<ShellTile> tilesEnum = ShellTile.ActiveTiles.GetEnumerator();

                while (tilesEnum.MoveNext()) // loop to update each and every live tile
                {
                    ShellTile currentTile = tilesEnum.Current;
                    Debug.WriteLine("processing tile: " + currentTile.NavigationUri.ToString());
                    /*
                    if (currentTile.NavigationUri.ToString() != "/") // ignore primare tile, only deal w/ secondary tiles
                    {
                        var rand = new Random(0);
                        var newTileData = new StandardTileData
                        {
                            // TO-DO: call to fetch actual data

                            BackTitle = "BackTitle",
                            BackContent = rand.Next(0,99).ToString() 
                        };

                        Debug.WriteLine("Updated " + DateTime.Now.ToShortTimeString());
                        currentTile.Update(newTileData);
                     } */
                // }
            
                /*
                // If the Main App is Running, Toast will not show
                ShellToast popupMessage = new ShellToast()
                {
                    Title = "Flurrysticks",
                    Content = "Background Task Launched"
                    //NavigationUri = new Uri("/Views/DeepLink.xaml", UriKind.Relative)
                };
                popupMessage.Show();
                */
 
            }
            NotifyComplete();
        }
    }
}