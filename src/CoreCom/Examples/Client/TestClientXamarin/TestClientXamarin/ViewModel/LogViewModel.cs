using System;
using System.Collections.ObjectModel;
using MvvmHelpers;
using WallTec.CoreCom.Sheard.Models;

namespace TestClientXamarin.ViewModel
{
    public class LogViewModel : BaseViewModel
    {
        public LogViewModel()
        {
            App.ServiceCoreCom._coreComClient.OnLogEventOccurred += CoreComClient_OnLogEventOccurred;
            App.ServiceCoreCom._coreComClient.OnConnectionStatusChange += _coreComClient_OnConnectionStatusChange; 
            LogEvents = new ObservableCollection<LogEvent>();
        }

        private void _coreComClient_OnConnectionStatusChange(object sender, WallTec.CoreCom.Sheard.ConnectionStatusEnum e)
        {

            //LogEvents.Add(new LogEvent { Title = "Connection Change:", ConnectionStatus = e });
            LogEvents.Insert(0, new LogEvent { Title = "Connection Change:", ConnectionStatus = e });
            
        }

        private void CoreComClient_OnLogEventOccurred(object sender, LogEvent e)
        {
            //LogEvents.Add(e);
            LogEvents.Insert(0,  e );

        }
        private ObservableCollection<LogEvent> _logEvents;
        public ObservableCollection<LogEvent> LogEvents
        {
            get { return _logEvents; }
            set { SetProperty(ref _logEvents, value); }
        }
    }
}
