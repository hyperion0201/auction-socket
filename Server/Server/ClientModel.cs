using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
namespace Server {
    public class ClientModel {
        public string Email { get; set; }
        public Socket ClientSocket { get; set; }

    }
}
