using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace WallTec.CoreCom.Server
{
    public  class CoreComBackgroundService : BackgroundService
    {
        private CoreComService _coreComService;

        public CoreComBackgroundService(CoreComService coreComService)
        {
            _coreComService = coreComService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("CoreComBackgroundService is starting.");

            //stoppingToken.Register(() =>
            //    _logger.LogDebug($" GracePeriod background task is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                //every 60 minutes = 3 600 000
                await Task.Delay(3600000, stoppingToken);

                Console.WriteLine($"CoreComBackgroundService task doing background work.");

                //remove history post from db
               await _coreComService.RemoveHistory();


            }

            Console.WriteLine($"CoreComBackgroundService background task is stopping.");
        }
    }
}
