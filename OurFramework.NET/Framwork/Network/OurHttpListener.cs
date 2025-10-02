using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OurFramework.NET.Framwork.Network
{
    public class OurHttpListener
    {
        private readonly List<string> _prefixes = [];
        private TcpListener? _tcp;
        private CancellationTokenSource? _cts;

        public bool IsListening => _tcp != null;

        public void AddPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("prefix cannot  be null or empty ", nameof(prefix));

            _prefixes.Add(prefix);
        }

        public void Start()
        {
            if (_tcp != null) throw new InvalidOperationException("Alrady Started!");
            if (_prefixes.Count == 0) throw new InvalidOperationException("No Prefix Added");

            var first = _prefixes[0];
            var uri = new Uri(first);

            var ipAddress = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) switch
            {
                true => IPAddress.Loopback,
                _ => IPAddress.Parse(first),
            };

            _tcp = new TcpListener(ipAddress, uri.Port);
            _tcp.Start();


            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            if (_tcp == null) return;

            while (!ct.IsCancellationRequested) 
            {




            }
    }

    
}
