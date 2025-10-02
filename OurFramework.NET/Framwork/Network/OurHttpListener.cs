using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OurFramework.NET.Framwork.Network
{
    public class OurHttpListener
    {
        private readonly List<string> _prefixes = [];
        private readonly TcpListener _tcp;

        public void  AddPrefix(string prefix)
        {
            if(string.IsNullOrEmpty(prefix))
                throw new ArgumentException("prefix cannot  be null or empty ",nameof(prefix));

            _prefixes.Add(prefix);
        }
    }
}
