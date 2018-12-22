using GlassLCU.API;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GlassLCU
{
    public sealed class LeagueClient : ISender
    {
        private static IDictionary<string, object> CacheDic = new Dictionary<string, object>();

        private string Token;
        private int Port;
        private bool IsTryingToInit;

        private bool _Connected;
        public bool IsConnected
        {
            get => _Connected;
            private set
            {
                _Connected = value;
                ConnectedChanged?.Invoke(value);
            }
        }

        public bool IsSender => GenerationUtils.Sender == this;

        public IRestClient Client { get; private set; }
        public ILeagueSocket Socket { get; }

        public delegate void ConnectedChangedDelegate(bool connected);
        public event ConnectedChangedDelegate ConnectedChanged;

        public LeagueClient()
        {
            this.Socket = new LeagueSocket(new LWebSocket());
            this.Client = new RestClient();

            if (GenerationUtils.Sender == null)
                SetAsSender();
        }

        /// <summary>
        /// Sets this class as the sender, which takes care of sending requests to the LCU when called through
        /// any interface method.
        /// </summary>
        public void SetAsSender()
        {
            GenerationUtils.Sender = this;
        }

        /// <summary>
        /// Begins to look for the LoL client and inits when detected.
        /// </summary>
        public void BeginTryInit(InitializeMethod method = InitializeMethod.CommandLine, int interval = 500,
            CancellationToken cancellationToken = default, TaskCompletionSource<bool> taskCompletionSource = null)
        {
            if (IsTryingToInit)
                return;

            IsTryingToInit = true;
            
            new Thread(() =>
            {
                while (!Init(method))
                {
                    Thread.Sleep(interval);

                    if (cancellationToken.IsCancellationRequested)
                        break;
                }

                taskCompletionSource?.SetResult(!cancellationToken.IsCancellationRequested);
                IsTryingToInit = false;
            })
            {
                IsBackground = true
            }.Start();
        }

        public bool Init(InitializeMethod method = InitializeMethod.CommandLine)
        {
            if (IsConnected)
                return false;

            if (!GetClientInfo(method, out Port, out Token, out var p))
                return false;

            Client.BaseUrl = new Uri("https://127.0.0.1:" + Port);
            Client.Authenticator = new HttpBasicAuthenticator("riot", Token);
            Client.ConfigureWebRequest(o =>
            {
                o.Accept = "application/json";
                o.ServerCertificateValidationCallback = delegate { return true; };
            });

            if (!Socket.Connect(Port, Token))
                return false;

            Socket.Closed += () => Close();

            IsConnected = true;

            return true;
        }

        private bool GetClientInfo(InitializeMethod method, out int port, out string token, out Process proc)
        {
            if (method == InitializeMethod.CommandLine)
            {
                Process[] processes = ProcessResolver.GetProcessesByName("LeagueClientUx");

                if (processes.Length == 0)
                    goto exit;

                Process process = processes[0];

                string cmdLine = ProcessResolver.GetCommandLine(process);

                if (cmdLine == null)
                    goto exit;

                port = int.Parse(Regex.Match(cmdLine, @"(?<=--app-port=)\d+").Value);
                token = Regex.Match(cmdLine, "(?<=--remoting-auth-token=).*?(?=\")").Value;
                proc = process;

                return true;
            }
            else if (method == InitializeMethod.Lockfile)
            {
                var p = Process.GetProcessesByName("LeagueClient");

                if (p.Length == 0)
                    goto exit;

                string lockFilePath;

                try
                {
                    lockFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(p[0].MainModule.FileName), "../../../../../../lockfile"));
                }
                catch
                {
                    goto exit;
                }

                if (!File.Exists(lockFilePath))
                    goto exit;

                string lockFile;

                using (var stream = File.Open(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    lockFile = new StreamReader(stream).ReadToEnd();

                string[] parts = lockFile.Split(':');
                port = int.Parse(parts[2]);
                token = parts[3];
                proc = p[0];

                return true;
            }

        exit:
            port = 0;
            token = null;
            proc = null;
            return false;
        }

        public void Close()
        {
            IsConnected = false;
            Socket.Close();
        }
        
        public async Task<string> GetSwaggerJson()
        {
            return (await Client.ExecuteTaskAsync<string>(new RestRequest("swagger/v2/swagger.json"))).Content;
        }

        Task<T> ISender.Request<T>(string method, string path, object body)
            => Request<T>((Method)Enum.Parse(typeof(Method), method, true), path, body);

        /// <summary>
        /// Sends a request to the LCU and returns data.
        /// </summary>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="path">The path for the endpoint.</param>
        /// <param name="body">Optional additional data to be sent.</param>
        /// <param name="includeFields">If not empty, only send <paramref name="body"/>'s fields whose names are
        /// in this array.</param>
        public async Task<T> Request<T>(Method method, string path, object body = null, params string[] includeFields)
        {
            if (!IsConnected)
                return default;
            
            var resp = await Client.ExecuteTaskAsync(BuildRequest(path, method, body, includeFields));
            CheckErrors(resp);

            return JsonConvert.DeserializeObject<T>(resp.Content, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        Task ISender.Request(string method, string path, object body)
            => Request((Method)Enum.Parse(typeof(Method), method, true), path, body);

        /// <summary>
        /// Sends a request to the LCU.
        /// </summary>
        /// <param name="method">The HTTP method to use.</param>
        /// <param name="path">The path for the endpoint.</param>
        /// <param name="body">Optional additional data to be sent.</param>
        /// <param name="includeFields">If not empty, only send <paramref name="body"/>'s fields whose names are
        /// in this array.</param>
        public async Task Request(Method method, string path, object body = null, params string[] includeFields)
        {
            if (!IsConnected)
                return;
            
            var resp = await Client.ExecuteTaskAsync(BuildRequest(path, method, body, includeFields));
            CheckErrors(resp);
        }


        private static string GetCommandLine(Process process)
        {
            using (var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
            using (var objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }
        }

        private static RestRequest BuildRequest(string resource, Method method, object data, string[] fields = null)
        {
            var req = new RestRequest(resource, method);

            if (data != null)
            {
                object realData = data;

                if (fields?.Length != 0)
                {
                    var dic = new Dictionary<string, object>();

                    foreach (var item in data.GetType().GetProperties().Where(o => fields.Contains(o.Name)))
                    {
                        dic[item.Name] = item.GetValue(data);
                    }

                    realData = dic;
                }

                req.AddHeader("Content-Type", "application/json");
                req.AddJsonBody(realData);
            }

            return req;
        }

        private static void CheckErrors(IRestResponse response)
        {
            if (response.Content.Contains("\"errorCode\""))
            {
                var error = JsonConvert.DeserializeObject<ErrorData>(response.Content);

                if (error.Message == "No active delegate")
                    throw new NoActiveDelegateException(error);

                throw new APIErrorException(error);
            }
        }
    }
}
