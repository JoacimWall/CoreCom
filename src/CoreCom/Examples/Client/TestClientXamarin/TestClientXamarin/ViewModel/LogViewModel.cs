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
            App.ServiceCoreCom._coreComClient.OnLogErrorOccurred += _coreComClient_OnLogErrorOccurred;
            LogEvents = new ObservableCollection<LogEvent>();
        }

        private void _coreComClient_OnLogErrorOccurred(object sender, LogError e)
        {
            LogEvents.Insert(0, new LogEvent { Description = e.Description, TimeStampUtc = e.TimeStampUtc });
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
