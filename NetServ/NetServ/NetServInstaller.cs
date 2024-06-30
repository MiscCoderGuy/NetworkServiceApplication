using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace NetServ
{
    [RunInstaller(true)]
    public partial class NetServInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public NetServInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller = new ServiceInstaller();
            serviceInstaller.ServiceName = "NetServ";
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.Description = "A service that monitors the App Data & Windows System Log directories and logs activities.";

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
