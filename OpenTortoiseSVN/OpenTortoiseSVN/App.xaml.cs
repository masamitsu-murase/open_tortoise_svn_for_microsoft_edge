using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace OpenTortoiseSVN
{
    class ConnectionDeferral
    {
        public ConnectionDeferral(AppServiceConnection con, BackgroundTaskDeferral def)
        {
            connection = con;
            deferral = def;
        }

        public AppServiceConnection connection { get; set; }
        public BackgroundTaskDeferral deferral { get; set; }
    }
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private Dictionary<int, ConnectionDeferral> connectionMap = new Dictionary<int, ConnectionDeferral>();
        private Dictionary<int, ConnectionDeferral> desktopConnectionMap = new Dictionary<int, ConnectionDeferral>();
        private Dictionary<int, TaskCompletionSource<bool>> taskCompletionSourceMap = new Dictionary<int, TaskCompletionSource<bool>>();
        private int connectionIndex = 0;
        private int desktopConnectionIndex = 0;
        private Object thisLock = new Object();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Initializes the app service on the host process 
        /// </summary>
        protected async override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            IBackgroundTaskInstance taskInstance = args.TaskInstance;
            if (taskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;

                if (appService.CallerPackageFamilyName == Windows.ApplicationModel.Package.Current.Id.FamilyName) // App service connection from desktopBridge App
                {
                    var desktopBridgeDeferral = taskInstance.GetDeferral();
                    var desktopConnection = appService.AppServiceConnection;

                    var connectionDeferral = new ConnectionDeferral(desktopConnection, desktopBridgeDeferral);
                    int index;
                    lock (thisLock)
                    {
                        index = desktopConnectionIndex;
                        desktopConnectionIndex++;
                        desktopConnectionMap.Add(index, connectionDeferral);
                    }
                    taskInstance.Canceled += (s, a) => {
                        OnAppServicesCanceled(s, a, index);
                    };
                    desktopConnection.RequestReceived += async (s, a) => {
                        await OnDesktopAppServiceRequestReceived(s, a, index);
                    };
                    desktopConnection.ServiceClosed += (s, a) => {
                        OnConnectionClosed(s, a, index);
                    };

                    lock (thisLock)
                    {
                        if (taskCompletionSourceMap.ContainsKey(index))
                        {
                            taskCompletionSourceMap[index].TrySetResult(true);
                        }
                    }
                }
                else // App service connection from Edge browser
                {
                    // Microsoft.MicrosoftEdge_8wekyb3d8bbwe
                    var appServiceDeferral = taskInstance.GetDeferral();
                    var connection = appService.AppServiceConnection;

                    var connectionDeferral = new ConnectionDeferral(connection, appServiceDeferral);
                    int index;
                    lock (thisLock)
                    {
                        index = connectionIndex;
                        connectionIndex++;
                        connectionMap.Add(index, connectionDeferral);
                    }

                    taskInstance.Canceled += (s, a) => {
                        OnAppServicesCanceled(s, a, index);
                    };
                    connection.RequestReceived += async (s, a) => {
                        await OnAppServiceRequestReceived(s, a, index);
                    };
                    connection.ServiceClosed += (s, a) => {
                        OnConnectionClosed(s, a, index);
                    };

                    try
                    {
                        // Make sure the tortoise_svn_opener.exe is in your AppX folder, if not rebuild the solution
                        await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    }
                    catch (Exception)
                    {
                        lock (thisLock)
                        {
                            if (connectionMap.ContainsKey(index))
                            {
                                connectionMap[index].connection.Dispose();
                                connectionMap[index].deferral.Complete();
                                connectionMap.Remove(index);
                            }
                            if (taskCompletionSourceMap.ContainsKey(index))
                            {
                                taskCompletionSourceMap[index].TrySetResult(false);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Receives message from Extension (via Edge)
        /// </summary>
        private async Task OnAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args, int index)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();

            try
            {
                TaskCompletionSource<bool> tcs = null;
                ConnectionDeferral cd = null;
                lock (thisLock)
                {
                    if (desktopConnectionMap.ContainsKey(index))
                    {
                        cd = desktopConnectionMap[index];
                    }
                    else
                    {
                        tcs = new TaskCompletionSource<bool>();
                        taskCompletionSourceMap.Add(index, tcs);
                    }
                }
                if (tcs != null)
                {
                    await tcs.Task;
                    lock (thisLock)
                    {
                        if (!desktopConnectionMap.ContainsKey(index))
                        {
                            return;
                        }
                        cd = desktopConnectionMap[index];
                    }
                }
                var desktopConnection = cd.connection;

                // Send message to the desktopBridge component and wait for response
                AppServiceResponse desktopResponse = await desktopConnection.SendMessageAsync(args.Request.Message);
                await args.Request.SendResponseAsync(desktopResponse.Message);
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        /// <summary>
        /// Receives message from desktopBridge App
        /// </summary>
        private async Task OnDesktopAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args, int index)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();

            try
            {
                AppServiceConnection connection = null;
                lock (thisLock)
                {
                    if (connectionMap.ContainsKey(index))
                    {
                        connection = connectionMap[index].connection;
                    }
                }
                if (connection != null)
                {
                    await connection.SendMessageAsync(args.Request.Message);
                }
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        /// <summary>
        /// Associate the cancellation handler with the background task 
        /// </summary>
        private void OnAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason, int index)
        {
            ConnectionDeferral app_dc = null;
            ConnectionDeferral desktop_dc = null;

            lock (thisLock)
            {
                if (connectionMap.ContainsKey(index))
                {
                    app_dc = connectionMap[index];
                    connectionMap.Remove(index);
                }
                if (desktopConnectionMap.ContainsKey(index))
                {
                    desktop_dc = desktopConnectionMap[index];
                    desktopConnectionMap.Remove(index);
                }
                if (taskCompletionSourceMap.ContainsKey(index))
                {
                    taskCompletionSourceMap[index].TrySetResult(false);
                }
            }
            if (app_dc != null)
            {
                app_dc.connection.Dispose();
                app_dc.deferral.Complete();
            }
            if (desktop_dc != null)
            {
                desktop_dc.connection.Dispose();
                desktop_dc.deferral.Complete();
            }
        }

        /// <summary>
        /// Occurs when the other endpoint closes the connection to the app service
        /// </summary>
        private void OnConnectionClosed(AppServiceConnection sender, AppServiceClosedEventArgs args, int index)
        {
            ConnectionDeferral app_dc = null;
            ConnectionDeferral desktop_dc = null;

            lock (thisLock)
            {
                if (connectionMap.ContainsKey(index))
                {
                    app_dc = connectionMap[index];
                    connectionMap.Remove(index);
                }
                if (desktopConnectionMap.ContainsKey(index))
                {
                    desktop_dc = desktopConnectionMap[index];
                    desktopConnectionMap.Remove(index);
                }
                if (taskCompletionSourceMap.ContainsKey(index))
                {
                    taskCompletionSourceMap[index].TrySetResult(false);
                }
            }
            if (app_dc != null)
            {
                app_dc.connection.Dispose();
                app_dc.deferral.Complete();
            }
            if (desktop_dc != null)
            {
                desktop_dc.connection.Dispose();
                desktop_dc.deferral.Complete();
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
