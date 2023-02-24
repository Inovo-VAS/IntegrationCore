using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.Background
{
    public class Service : IHostedService, IDisposable
    {
        private Action startEvent = null;
        public Action StartEvent { get { return this.startEvent; } set { this.startEvent = value; } }

        private Action stopEvent = null;
        public Action StopEvent { get { return this.stopEvent; } set { this.stopEvent = value; } }
        public async Task StartAsync(CancellationToken stoppingToken)
        {
            if (this.startEvent != null)
            {
                await Task.Run(this.startEvent);
            }
        }

        public async Task StopAsync(CancellationToken stoppingToken)
        {
            if (this.stopEvent != null)
            {
                await Task.Run(this.stopEvent);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                disposedValue = true;
            }
        }

        ~Service()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
