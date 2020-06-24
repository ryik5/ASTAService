using System.ServiceProcess;

namespace ASTAWebClient
{
    public partial class AstaWebClient : ServiceBase
    {

        IServiceManageable _serviceManagable;
        public AstaWebClient(IServiceManageable serviceManagable)
        {
            InitializeComponent();
            _serviceManagable = serviceManagable;
        }


        protected override void OnStart(string[] args)
        {
            _serviceManagable.OnStart();
        }

        protected override void OnStop()
        {
            _serviceManagable.OnStop();
        }
    }
}