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
        private Task? _acceptLoop;

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

            _cts = new CancellationTokenSource();

            _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (_tcp == null) return;
            _cts.Cancel();
            _tcp.Stop();


            if (_acceptLoop != null)
            {
                try
                {
                   await _acceptLoop.ConfigureAwait(false);
                }
                catch
                {

                  
                }
            }


            _tcp = null;
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            if (_tcp == null) return;

            while (!ct.IsCancellationRequested)
            {
                TcpClient client = await _tcp.AcceptTcpClientAsync();

                var stream = client.GetStream();
                stream.ReadTimeout = 5000;
                stream.WriteTimeout = 5000;

                var httpContext = 

                using var reader = new StreamReader(stream,Encoding.UTF8,leaveOpen:true,detectEncodingFromByteOrderMarks:false);

                var readLine = await reader.ReadLineAsync();


            }
        }
    }

}


public class OurHttpListenerContext
{

}