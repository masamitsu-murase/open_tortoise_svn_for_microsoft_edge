
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace TortoiseSvnOpener
{
    class Program
    {
        static AppServiceConnection connection = null;
        const string TORTOISE_PROC_NAME = "TortoiseProc.exe";

        /// <summary>
        /// Creates an app service thread
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Thread appServiceThread = new Thread(new ThreadStart(ThreadProc));
            appServiceThread.Start();
            Application.Run();
        }

        /// <summary>
        /// Creates the app service connection
        /// </summary>
        static async void ThreadProc()
        {
            connection = new AppServiceConnection();
            connection.AppServiceName = "murase.masamitsu.OpenTortoiseSvnUwp";
            connection.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            connection.RequestReceived += Connection_RequestReceived;
            connection.ServiceClosed += Connection_ServiceClosed;

            await connection.OpenAsync();
        }

        /// <summary>
        /// Occurs when the other endpoint closes the connection to the app service
        /// </summary>
        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            Application.Exit();
        }

        /// <summary>
        /// Receives message from UWP app and sends a response back
        /// </summary>
        private static async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();
            try
            {
                var value = args.Request.Message.First().Value.ToString();
                var jsonSerializer = new JavaScriptSerializer();
                var response = new ValueSet();
                try
                {
                    var valueList = (IDictionary<string, object>)jsonSerializer.DeserializeObject(value);
                    string messageType = valueList["action"].ToString();

                    switch (messageType)
                    {
                        case "search_tsvn":
                            var tsvnPath = SearchTsvn();
                            if (tsvnPath != "")
                            {
                                response.Add("message", jsonSerializer.Serialize(new { result = true, data = tsvnPath }));
                            }
                            else
                            {
                                response.Add("message", jsonSerializer.Serialize(new { result = true, data = false }));
                            }
                            break;
                        case "tsvn":
                            var path = valueList["path"].ToString();
                            var tsvn_args = ((object[])valueList["args"]).Select(o => (string)o).ToArray();
                            var ret = RunTsvn(path, tsvn_args);
                            response.Add("message", jsonSerializer.Serialize(new { result = ret }));
                            break;
                        default:
                            response.Add("message", jsonSerializer.Serialize(new { result = false, error = "Unknown action" }));
                            break;
                    }
                }
                catch (System.Exception e)
                {
                    response = new ValueSet();
                    response.Add("message", jsonSerializer.Serialize(new { result = false, error = e.ToString() }));
                }
                await args.Request.SendResponseAsync(response);
            }
            finally
            {
                messageDeferral.Complete();
                Application.Exit();
            }
        }

        private static string SearchTsvn()
        {
            const string tsvnRelativePath = "TortoiseSVN\\bin\\" + TORTOISE_PROC_NAME;
            string[] envList = { "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432" };
            var path = envList.Select(envName => System.Environment.GetEnvironmentVariable(envName))
                .Where(s => s != null).Select(dir => System.IO.Path.Combine(dir, tsvnRelativePath))
                .FirstOrDefault(file => System.IO.File.Exists(file));
            return path;
        }

        private static bool RunTsvn(string path, string[] args)
        {
            if (!path.EndsWith(TORTOISE_PROC_NAME))
            {
                throw new System.ArgumentException("Path should end with " + TORTOISE_PROC_NAME + ".");
            }

            if (!System.IO.File.Exists(path))
            {
                throw new System.ArgumentException(TORTOISE_PROC_NAME + " is not found.");
            }

            var proc = System.Diagnostics.Process.Start(path, string.Join(" ", args));
            if (proc == null)
            {
                throw new System.InvalidOperationException("Failed to run " + TORTOISE_PROC_NAME + ".");
            }

            return true;
        }

        private static void HandleErrorException(Exception exception)
        {
        }
    }
}
