using System.ComponentModel;


namespace LexisNexis.Evolution.Overdrive
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            serviceInstaller1.FailureActions.Add(new FailureAction(RecoverAction.Restart, 5000));
        }
    }
}
