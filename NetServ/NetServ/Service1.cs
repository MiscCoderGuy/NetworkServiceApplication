using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Security.Principal;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Management;

namespace NetServ
{
    public partial class Service1 : ServiceBase
    {
        private Timer timer;
        private FileSystemWatcher watcher;
        private FileSystemWatcher watcher2;
        private const string logFile = @"C:\Users\User\Desktop\DocActivityLog.csv";
        private const string documentsPath = @"C:\Windows\Logs";
        private const string monitorPath2 = @"C:\Users\User\AppData\Local";

        public Service1()
        {
            InitializeComponent();
            if (!EventLog.SourceExists("NetServSource"))
            {
                EventLog.CreateEventSource("NetServSource", "NetServLog");
            }
            eventLog1.Source = "NetServSource";
            eventLog1.Log = "NetServLog";
            string logEntry = $"DateTime,Event,Group,UserID";
            LogToFile(logEntry);
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Net Serv Started.");

            // Initialize and start the timer
            timer = new Timer();
            timer.Interval = 300000; // 5 minutes
            timer.Elapsed += new ElapsedEventHandler(OnTimer);
            timer.Start();

            // Initialize and start the file system watcher 1
            watcher = new FileSystemWatcher();
            watcher.Path = documentsPath;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastAccess
                | NotifyFilters.LastWrite
                | NotifyFilters.FileName
                | NotifyFilters.DirectoryName;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;

            // Initialize and start the file system watcher 2
            watcher2 = new FileSystemWatcher();
            watcher2.Path = monitorPath2;
            watcher2.IncludeSubdirectories = true;
            watcher2.NotifyFilter = NotifyFilters.LastAccess
                | NotifyFilters.LastWrite
                | NotifyFilters.FileName
                | NotifyFilters.DirectoryName;
            watcher2.Filter = "*.*";
            watcher2.Changed += new FileSystemEventHandler(OnChanged);
            watcher2.Created += new FileSystemEventHandler(OnChanged);
            watcher2.Deleted += new FileSystemEventHandler(OnChanged);
            watcher2.Renamed += new RenamedEventHandler(OnRenamed);
            watcher2.EnableRaisingEvents = true;
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Net Serv Stopped.");
            timer.Stop();
            watcher.EnableRaisingEvents = false;
            watcher2.EnableRaisingEvents = false;
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            eventLog1.WriteEntry("Net Serv Timer Tick", EventLogEntryType.Information);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            int colonIndex = 0;
            string logon = string.Empty;
            string userSID = string.Empty;
            string sid = string.Empty;
            string userName = GetCurrentUser();
            string machineName = GetMachineName();
            string userNameActual = GetCurrentUserActual(e.FullPath);
            colonIndex = userNameActual.IndexOf(":");
            logon = userNameActual.Substring(0, colonIndex - 1).Trim();
            userSID = userNameActual.Substring(colonIndex + 1).Trim();
            sid = SIDAccountName(userSID);
            string logEntry = $"{DateTime.Now},{e.ChangeType},{sid},{userName},{e.FullPath}";
            string logEntryActual = $"{DateTime.Now},{e.ChangeType},{sid},{userName},{e.FullPath}";
            LogToFile(logEntry);
            LogToFile(logEntryActual);
            EventLog.WriteEntry(logEntry, EventLogEntryType.Information);
            EventLog.WriteEntry(logEntryActual, EventLogEntryType.Information);
        }

        // Rename event
        private void OnRenamed(object source, RenamedEventArgs e)
        {
            int colonIndex = 0;
            string logon = string.Empty;
            string userSID = string.Empty;
            string sid = string.Empty;
            string userName = GetCurrentUser();
            string userNameActual = GetCurrentUserActual(e.FullPath);
            colonIndex = userNameActual.IndexOf(":");
            logon = userNameActual.Substring(0, colonIndex).Trim();
            userSID = userNameActual.Substring(colonIndex + 1).Trim();
            sid = SIDAccountName(userSID);
            string logEntry = $"{DateTime.Now},{sid},{userName},{e.OldFullPath},{e.FullPath}";
            LogToFile(logEntry);
            eventLog1.WriteEntry(logEntry, EventLogEntryType.Information);
        }

        // Convert SID to User Account Name
        public static string SIDAccountName(string sid)
        {
            try
            {
                SecurityIdentifier secID = new SecurityIdentifier(sid);
                NTAccount account = (NTAccount)secID.Translate(typeof(NTAccount));
                return account.ToString();
            }
            catch (Exception ex)
            {
                return $"Error converting SID: {ex.Message}";
            }
        }

        // Get User SID
        private string GetCurrentUser()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                // return identity.Name;
                return identity.User.ToString();
            }
        }

        // Get the security identifier (SID) for the User 
        private string GetMachineName()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                return identity.User.ToString();
            }
        }

        /*
         * Searches the Security Event Log for event ID 4663, which is related
         * to file system access. It filters events within the last 5 minutes
         * and matches the file path to find the user responsible for the 
         * file operation.
         */
        private string GetCurrentUserActual(string filePath)
        {
            // Windows logon name
            string currentUser = WindowsIdentity.GetCurrent().Name;

            // SID of the user
            string userSID = WindowsIdentity.GetCurrent().User.ToString();

            // Dual part string [Logon name | User SID]
            string logonSID = currentUser + ":" + userSID;

            // If the current user is an Administrator group, try to find the actual user
            if (currentUser.Contains("Administrators"))
            {
                string actualUser = GetSecurityLogUser(filePath);
                if (!string.IsNullOrEmpty(actualUser))
                {
                    currentUser = actualUser;
                    return currentUser;
                }
            }
            else if (currentUser.Contains("NT AUTHORITY\\SYSTEM") || currentUser.Contains("NT AUTHORITY"))
            {
                string actualUser = GetSecurityLogUser(filePath);
                if (!string.IsNullOrEmpty(actualUser))
                {
                    currentUser = actualUser;
                    return currentUser;
                }
            }

            return currentUser = logonSID;
        }

        /*
         * Modified to check if the user is part of the "Administrators" group. 
         * If so, it attempts to find the actual user from the Security Event Log.
         */
        private string GetSecurityLogUser(string filePath)
        {
            string userName = string.Empty;

            try
            {
                var eventLog = new EventLog("Security");
                var entries = eventLog.Entries.Cast<EventLogEntry>()
                    .Where(entry => entry.InstanceId == 4663 && entry.TimeGenerated > DateTime.Now.AddMinutes(-5))
                    .OrderByDescending(entry => entry.TimeGenerated);

                foreach (var entry in entries)
                {
                    if (entry.ReplacementStrings != null && entry.ReplacementStrings.Length > 8)
                    {
                        string objectName = entry.ReplacementStrings[6]; // Object Name (File Path)
                        if (objectName.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            userName = entry.ReplacementStrings[5]; // Account Name
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry($"Error retrieving user from security log: {ex.Message}", EventLogEntryType.Error);
            }

            return userName;
        }

        private void LogToFile(string logEntry)
        {
            using (StreamWriter writer = new StreamWriter(logFile, true))
            {
                writer.WriteLine(logEntry);
            }
        }

        private void eventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
        {

        }
    }
}
