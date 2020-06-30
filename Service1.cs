using System.ServiceProcess;

namespace ASTAWebClient
{
    public partial class AstaWebClient : ServiceBase
    {

        IServiceManageable service;
        public AstaWebClient(IServiceManageable service)
        {
            InitializeComponent();
            this.service = service;
        }


        protected override void OnStart(string[] args)
        {
            service.OnStart();
        }

        protected override void OnStop()
        {
            service.OnStop();
        }
    }
}