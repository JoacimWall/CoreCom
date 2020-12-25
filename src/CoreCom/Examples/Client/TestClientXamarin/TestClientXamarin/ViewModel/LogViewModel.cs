using System;
using System.Collections.ObjectModel;
using WallTec.CoreCom.Sheard.Models;

namespace TestClientXamarin.ViewModel
{
    public class LogViewModel : BaseViewModel
    {
        public LogViewModel()
        {
            App.ServiceCoreCom._coreComClient.OnLogEventOccurred += CoreComClient_OnLogEventOccurred;
            LogEvents = new ObservableCollection<LogEvent>();
        }

        private void CoreComClient_OnLogEventOccurred(object sender, LogEvent e)
        {
            LogEvents.Add(e);
        }
        private ObservableCollection<LogEvent> _logEvents;
        public ObservableCollection<LogEvent> LogEvents
        {
            get { return _logEvents; }
            set { SetProperty(ref _logEvents, value); }
        }
    }
}
