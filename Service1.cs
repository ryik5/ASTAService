using System.ServiceProcess;

namespace ASTAWebClient
{
    public partial class AstaWebClient : ServiceBase
    {

        IServiceManageable serviceManagable;
        public AstaWebClient(IServiceManageable serviceManagable)
        {
            InitializeComponent();
            this.serviceManagable = serviceManagable;
        }


        protected override void OnStart(string[] args)
        {
            serviceManagable.OnStart();
        }

        protected override void OnStop()
        {
            serviceManagable.OnStop();
        }
    }
}