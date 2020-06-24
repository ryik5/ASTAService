using System.ServiceProcess;

namespace ASTAWebClient
{
    public partial class AstaServiceLocal : ServiceBase
    {

        IServiceManageable _serviceManagable;
        public AstaServiceLocal(IServiceManageable serviceManagable)
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