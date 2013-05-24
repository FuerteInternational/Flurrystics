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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Telerik.Windows.Controls.Primitives;
using Telerik.Windows.Controls;
using Microsoft.Phone.Scheduler;
using System.Diagnostics;
using BugSense;

namespace Flurrystics
{
    public partial class App : Application
    {

        public static long lastRequest = 0; // timestamp of lastrequest through the app
        public static long requestCount = 0;

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        //public PhoneApplicationFrame RootFrame { get; private set; }
        public RadPhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {

            BugSenseHandler.Instance.Init(this, "dde4e090");

            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;
            // BugSenseHandler.Instance.UnhandledException += OnUnhandledException;
            
            /*
            var options = BugSenseHandler.Instance.GetDefaultOptions();
            options.Type = enNotificationType.MessageBox;
            BugSenseHandler.Instance.Init(this, "dde4e090", options);
            BugSenseHandler.Instance.UnhandledException += Instance_UnhandledException;
            */

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            ThemeManager.ToLightTheme();

            if (Util.Is512Mb)
            {
                StartPeriodicAgent();
            }

            // Show graphics profiling information while debugging.
            //if (System.Diagnostics.Debugger.IsAttached)
            //{
                // Display the current frame rate counters.
                //Application.Current.Host.Settings.EnableFrameRateCounter = false;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
             //   PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
           // }

        }

        private static void StartPeriodicAgent()
        {
            var periodicTask = new PeriodicTask("FlurrysticksTaskAgent")
            {
                // The description is required. This is the string that the user
                // will see in the background services Settings page on the device.
                Description = "Provides background updates for Applytics live tiles." 
            };
            // If the agent is already registered with the system,
            // call the StopPeriodicAgent helper method. 
            if (ScheduledActionService.Find(periodicTask.Name) != null)
            {
                Debug.WriteLine("Agent exists, stopping...");
                StopPeriodicAgent();
            }
            ScheduledActionService.Add(periodicTask);
            Debug.WriteLine("Adding periodicTask, starting..."); 
            // ScheduledActionService.LaunchForTest("FlurrysticksTaskAgent", TimeSpan.FromSeconds(5));
        }

        private static void StopPeriodicAgent()
        {

            ScheduledActionService.Remove("FlurrysticksTaskAgent");

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            FlurryWP7SDK.Api.StartSession("9697X37ZJW956JD76Q3M");
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            FlurryWP7SDK.Api.StartSession("9697X37ZJW956JD76Q3M");
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Ensure that required application state is persisted here.
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RadPhoneApplicationFrame frame = new RadPhoneApplicationFrame();
            RootFrame = frame;
            RootFrame.Navigated += CompleteInitializePhoneApplication;
            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;
            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
            /*

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
             * */
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}