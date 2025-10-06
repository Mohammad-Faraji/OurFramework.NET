using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OurFramework.NET.Framwork.Network
{
    public class OurHttpListener
    {
        private readonly List<string> _prefixes = [];
        private TcpListener? _tcp;
        private CancellationTokenSource? _cts;
        private Task? _acceptLoop;

        private readonly Channel<OurHttpListenerContext> _poolContexts = Channel.CreateUnbounded<OurHttpListenerContext>();

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


                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true, detectEncodingFromByteOrderMarks: false);

                var requestLine = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(requestLine)) continue;

                var parts = requestLine?.Split(separator: ' ');
                var method = parts?[0];
                var path = parts?[1];
                var protocol = parts?[2];

                string line;
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
                {
                    var idx = line.IndexOf(value: ':');
                    if (idx <= 0) continue;

                    var name = line[..idx].Trim();
                    var value = line[(idx + 1)..].Trim();
                    headers[name] = value;


                }


                byte[]? bodyBytes = null;
                if (headers.TryGetValue("Content-Length", out var lenstr) &&
                    int.TryParse(lenstr, out var len) && len > 0)
                {
                    bodyBytes = new byte[len];

                    var read = 0;
                    char[] buffer = new char[len];

                    var chrsread = await reader.ReadBlockAsync(buffer, index: 0, len);
                    bodyBytes = Encoding.UTF8.GetBytes(buffer, 0, chrsread);

                    //await stream.ReadAsync(bodyBytes, offset: 0, contentLength, ct).ConfigureAwait(false);

                }

                string bodyText = bodyBytes != null ? Encoding.UTF8.GetString(bodyBytes) : string.Empty;

                var request = new OurHttpListenerRequest(method ?? "GET", path ?? "/", protocol ?? "HTTP/1.1", headers
                    , bodyBytes != null ? new ReadOnlyMemory<byte>(bodyBytes) : ReadOnlyMemory<byte>.Empty);

                var response = new OurHttpListenerResponse(stream);
                var httpContext = new OurHttpListenerContext(request, response);

                await _poolContexts.Writer.WriteAsync(httpContext);
            }
        }

        internal  ValueTask<OurHttpListenerContext> GetNewRequest()
        {
          return   _poolContexts.Reader.ReadAsync();
        }
    }




}


public class OurHttpListenerContext
{
    public OurHttpListenerRequest Request { get; set; }
    public OurHttpListenerResponse Response { get; set; }
    public OurHttpListenerContext(OurHttpListenerRequest request, OurHttpListenerResponse response)
    {
        request = request;
        Response = response;
    }
}

public class OurHttpListenerRequest
{
    public string HttpMethod { get; set; }
    public string Path { get; set; }
    public string Protocol { get; set; }
    public IReadOnlyDictionary<string, string> Headers { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }

    public OurHttpListenerRequest(string httpMethod, string path,string protocol, IReadOnlyDictionary<string, string> headers, ReadOnlyMemory<byte> body)
    {
        HttpMethod = httpMethod;
        Path = path;
        Protocol = protocol;
        Headers = headers;
        Body = body;
    }

 
}

public class OurHttpListenerResponse
{
    private readonly Stream _netWork;
    private bool _closed;

    private readonly MemoryStream _buffer = new MemoryStream();

    public OurHttpListenerResponse(Stream netWork)
    {
        _netWork = netWork;
    }

    public int StatusCode { get; internal set; }

    internal async Task CloseAsync()
    {
      _closed = true;
    }
    internal void  WriteAsync(string response)
    {
        if (_closed)
            throw new InvalidOperationException(message: "Resonse Already Closed");

        var bytes = Encoding.UTF8.GetBytes(response);
        _buffer.Write(bytes, 0, bytes.Length);
    }


    //public string HttpMethod { get; set; }
    //public string Path { get; set; }
    //public string Protocol { get; set; }
    //public IReadOnlyDictionary<string, string> Headers { get; set; }
    //public ReadOnlyMemory<byte> Body { get; set; }

}