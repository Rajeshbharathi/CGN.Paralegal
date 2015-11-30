using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;


namespace LexisNexis.Evolution.Overdrive
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            //This is done through designer now
            //serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;

            serviceInstaller1.FailureActions.Add(new FailureAction(RecoverAction.Restart, 5000));
        }
    }
}
