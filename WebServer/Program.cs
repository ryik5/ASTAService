using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;
using System.Net;

namespace WebServer
{
    class Program
    {
        static string webSocketUri;
      static  Logger log = null;
      static  WebSocketServer aServer;
        /// <summary>
        /// Store the list of online users. Wish I had a ConcurrentList. 
        /// </summary>
        protected static ConcurrentDictionary<User, string> OnlineUsers = new ConcurrentDictionary<User, string>();

        /// <summary>
        /// Initialize the application and start the Alchemy Websockets server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            log = new Logger();

            ServerLaunchAsync();
            
            //Console.ReadLine();
            //Console.ReadKey();
            aServer.Stop();
            WriteString("Finish");
        }
               
        private static void ServerLaunchAsync()
        {
            // Initialize the server on port 81, accept any IPs, and bind events.
            WebSocketServer aServer = new WebSocketServer(5000, IPAddress.Any)
            {
                OnReceive = OnReceive,
                OnSend = OnSend,
                OnConnected = OnConnect,
                OnDisconnect = OnDisconnect,
                TimeOut = new TimeSpan(0, 5, 0)
            };

            aServer.Start();

            // Accept commands on the console and keep it alive
            var command = string.Empty;
            while (command != "exit"||command != "stop")
            {
                command = Console.ReadLine();
            }

            WriteString($"Websocket server is waiting on {aServer.Origin}: {aServer.Port}");            
        }

        private static void WebSocket_EvntInfoMessage(object sender, TextEventArgs e)
        {
            log.WriteString(e.Message);
        }

        public static void WriteString(string text)
        {
            if (log != null)
                log.WriteString(text);
        }

        /// <summary>
        /// Event fired when a client connects to the Alchemy Websockets server instance.
        /// Adds the client to the online users list.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnConnect(UserContext context)
        {
            WriteString("Client Connected from : " + context.ClientAddress);

            var me = new User { Context = context };

            OnlineUsers.TryAdd(me, String.Empty);
        }

        /// <summary>
        /// Event fired when a data is received from the Alchemy Websockets server instance.
        /// Parses data as JSON and calls the appropriate message or sends an error message.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnReceive(UserContext context)
        {
            WriteString("Received data from: " + context.ClientAddress);
                var json = context.DataFrame.ToString();

            try
            {
            WriteString("Raw data:" + json);

                // <3 dynamics
                dynamic obj = JsonConvert.DeserializeObject(json);

                switch ((int)obj.Type)
                {
                    case (int)CommandType.Register:
                        var r1 = new Response { Type = ResponseType.Message, Data = new { Message = $"Вы отправили {obj?.Data?.Message}" } };
                        context.Send(JsonConvert.SerializeObject(r1));

                        Register(obj.Name.Value, context);
                        break;
                    case (int)CommandType.Message:
                        var r2 = new Response { Type = ResponseType.Message, Data = new { Message = $"Вы отправили {obj?.Data?.Message}" } };
                        context.Send(JsonConvert.SerializeObject(r2));

                        ChatMessage(obj.Message.Value, context);
                        break;
                    case (int)CommandType.NameChange:
                        var r3 = new Response { Type = ResponseType.Message, Data = new { Message = $"Вы отправили {obj?.Data?.Message}" } };
                        context.Send(JsonConvert.SerializeObject(r3));

                        NameChange(obj.Name.Value, context);
                        break;
                }
            }
            catch (Exception e) // Bad JSON! For shame.
            {
                //  var r = new Response { Type = ResponseType.Error, Data = new { V =$"what did you want to say? {json} +{e.Message}" } };
                var r = new Response { Type= ResponseType.Message, Data =new { Message = $" Сейчас {DateTime.Now.ToString("yyyy-MM-dd hh:MM:ss")} и ты спросил {json}{Environment.NewLine} Сказать время?, ошибка: {e.Message}" } };
                context.Send(JsonConvert.SerializeObject(r));
            }
        }

        /// <summary>
        /// Event fired when the Alchemy Websockets server instance sends data to a client.
        /// Logs the data to the console and performs no further action.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnSend(UserContext context)
        {
            var json = context.DataFrame.ToString();
            WriteString($"Send to : {context.ClientAddress} message: {json}");
        }

        /// <summary>
        /// Event fired when a client disconnects from the Alchemy Websockets server instance.
        /// Removes the user from the online users list and broadcasts the disconnection message
        /// to all connected users.
        /// </summary>
        /// <param name="context">The user's connection context</param>
        public static void OnDisconnect(UserContext context)
        {
            WriteString("Client Disconnected : " + context.ClientAddress);
            var user = OnlineUsers.Keys.Where(o => o.Context.ClientAddress == context.ClientAddress).Single();

            string trash; // Concurrent dictionaries make things weird

            OnlineUsers.TryRemove(user, out trash);

            if (!String.IsNullOrEmpty(user.Name))
            {
                var r = new Response { Type = ResponseType.Disconnect, Data = new { user.Name } };

                Broadcast(JsonConvert.SerializeObject(r));
            }

            BroadcastNameList();
        }

        /// <summary>
        /// Register a user's context for the first time with a username, and add it to the list of online users
        /// </summary>
        /// <param name="name">The name to register the user under</param>
        /// <param name="context">The user's connection context</param>
        private static void Register(string name, UserContext context)
        {
            var u = OnlineUsers.Keys.Where(o => o.Context.ClientAddress == context.ClientAddress).Single();
            var r = new Response();

            if (ValidateName(name))
            {
                u.Name = name;

                r.Type = ResponseType.Connection;
                r.Data = new { u.Name };

                Broadcast(JsonConvert.SerializeObject(r));

                BroadcastNameList();
                OnlineUsers[u] = name;
            }
            else
            {
                SendError("Name is of incorrect length.", context);
            }
        }

        /// <summary>
        /// Broadcasts a chat message to all online usrs
        /// </summary>
        /// <param name="message">The chat message to be broadcasted</param>
        /// <param name="context">The user's connection context</param>
        private static void ChatMessage(string message, UserContext context)
        {
            var u = OnlineUsers.Keys.Where(o => o.Context.ClientAddress == context.ClientAddress).Single();
            var r = new Response { Type = ResponseType.Message, Data = new { u.Name, Message = message } };

            Broadcast(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Update a user's name if they sent a name-change command from the client.
        /// </summary>
        /// <param name="name">The name to be changed to</param>
        /// <param name="aContext">The user's connection context</param>
        private static void NameChange(string name, UserContext aContext)
        {
            var u = OnlineUsers.Keys.Where(o => o.Context.ClientAddress == aContext.ClientAddress).Single();

            if (ValidateName(name))
            {
                var r = new Response
                {
                    Type = ResponseType.NameChange,
                    Data = new { Message = u.Name + " is now known as " + name }
                };
                Broadcast(JsonConvert.SerializeObject(r));

                u.Name = name;
                OnlineUsers[u] = name;

                BroadcastNameList();
            }
            else
            {
                SendError("Name is of incorrect length.", aContext);
            }
        }

        /// <summary>
        /// Broadcasts an error message to the client who caused the error
        /// </summary>
        /// <param name="errorMessage">Details of the error</param>
        /// <param name="context">The user's connection context</param>
        private static void SendError(string errorMessage, UserContext context)
        {
            var r = new Response { Type = ResponseType.Error, Data = new { Message = errorMessage } };

            context.Send(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Broadcasts a list of all online users to all online users
        /// </summary>
        private static void BroadcastNameList()
        {
            var r = new Response
            {
                Type = ResponseType.UserCount,
                Data = new { Users = OnlineUsers.Values.Where(o => !String.IsNullOrEmpty(o)).ToArray() }
            };
            Broadcast(JsonConvert.SerializeObject(r));
        }

        /// <summary>
        /// Broadcasts a message to all users, or if users is populated, a select list of users
        /// </summary>
        /// <param name="message">Message to be broadcast</param>
        /// <param name="users">Optional list of users to broadcast to. If null, broadcasts to all. Defaults to null.</param>
        private static void Broadcast(string message, ICollection<User> users = null)
        {
            if (users == null)
            {
                foreach (var u in OnlineUsers.Keys)
                {
                    u.Context.Send(message);
                }
            }
            else
            {
                foreach (var u in OnlineUsers.Keys.Where(users.Contains))
                {
                    u.Context.Send(message);
                }
            }
        }

        /// <summary>
        /// Checks validity of a user's name
        /// </summary>
        /// <param name="name">Name to check</param>
        /// <returns></returns>
        private static bool ValidateName(string name)
        {
            var isValid = false;
            if (name.Length > 3 && name.Length < 25)
            {
                isValid = true;
            }

            return isValid;
        }

        /// <summary>
        /// Defines the type of response to send back to the client for parsing logic
        /// </summary>
        public enum ResponseType
        {
            Connection = 0,
            Disconnect = 1,
            Message = 2,
            NameChange = 3,
            UserCount = 4,
            Error = 255
        }

        /// <summary>
        /// Defines the response object to send back to the client
        /// </summary>
        public class Response
        {
            public ResponseType Type { get; set; }
            public dynamic Data { get; set; }
        }

        /// <summary>
        /// Holds the name and context instance for an online user
        /// </summary>
        public class User
        {
            public string Name = String.Empty;
            public UserContext Context { get; set; }
        }

        /// <summary>
        /// Defines a type of command that the client sends to the server
        /// </summary>
        public enum CommandType
        {
            Register = 0,
            Message,
            NameChange
        }

    }


    public class Logger
    {
        readonly object obj = new object();

        public Logger() { }

        public void WriteString(string text)
        {
            RecordEntry("Message", text);
        }
        private void RecordEntry(string eventText, string text)
        {
            string path = Assembly.GetExecutingAssembly().Location;
            string pathToLog = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".log");
            lock (obj)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(pathToLog, true))
                {
                    writer.WriteLine($"{DateTime.Now.ToString("yyyy.MM.dd|hh:mm:ss")}|{eventText}|{text}");
                    writer.Flush();
                }
            }
        }
    }
    
    /// <summary>
    /// using in other class 
    /// public delegate void InfoMessage(object sender, TextEventArgs e); 
    /// public event InfoMessage EvntInfoMessage; 
    /// EvntInfoMessage?.Invoke(this, new TextEventArgs("info message to target class")); 
    /// using in the caller class: 
    /// reader.EvntInfoMessage += Write_text; 
    /// signature of method: 
    /// void Write_text(object sender, TextEventArgs e){ sender as (className); e.Action; } 
    /// </summary>
    public class TextEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public TextEventArgs(string message)
        {
            Message = message;
        }
    }
}