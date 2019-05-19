using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.IO;

namespace Server {

    public partial class MainWindow : Window {
       
        private const int port = 8080;
        public Socket server = null;
        public string data = null;
        private List<ClientModel> clientList = null;
        private List<Product> productList = null;
        public static MainWindow Instance { get; private set; }
        public MainWindow() {
            InitializeComponent();
            Instance = this;
        }
       
        private void StartServer(int maxClient) {
            clientList = new List<ClientModel>();
            productList = new List<Product>();
            // load product

            StreamReader sr = new StreamReader("product.txt");
            int productCount = File.ReadAllLines("product.txt").Length;
            for (int i =0;i<productCount;i++) {
                string[] productArr = sr.ReadLine().Split('/');
                productList.Add(new Product() {
                    ID = productArr[0],
                    ProductName = productArr[1],
                    BeginCost = productArr[2]
                });
            }

            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEP = new IPEndPoint(ipAddress, port);
            server = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(localEP);
            
            status.Text = "Waiting for connection...";
            new Thread(() => StartListening(server, maxClient)).Start();
        }

        private void StartListening(Socket socket, int maxClient) {
            socket.Listen(maxClient);
            Accept(socket);
        }
        private void Accept(Socket socket) {
            while(true) {
               
                Socket accepted = socket.Accept();
               
                //create new thread
                Thread handler = new Thread(() => {
                    HandleIncomingClient(accepted);
                });
                handler.Start();
                
            }
          
        }
        private static void HandleIncomingClient(Socket socket) {
            // begin receive
            string data = null;
            byte[] bytes = new byte[1024];
            while (true) {
                bytes = new byte[1024];
                int bytesRec = socket.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (data.IndexOf("\n") > -1) {
                    break;  
                }
               
               
            }
           // handle email 
           for (int i =0;i<Instance.clientList.Count;i++) {
                if (data == Instance.clientList[i].Email) {
                    // send error response to client
                    byte[] errMsg = Encoding.ASCII.GetBytes("Duplicate email.");
                    socket.Send(errMsg);
                    return;
                }
            }
            
            Instance.Dispatcher.Invoke(() => Instance.UpdateUI(data));
            Instance.Dispatcher.Invoke(() => Instance.clientList.Add(new ClientModel() {
                Email = data,
                ClientSocket = socket
            }));

            //echo back to client
            byte[] msg = Encoding.ASCII.GetBytes(data);
            socket.Send(msg);
        }

        private void StartServer(object sender, RoutedEventArgs e) {
            //get number from txtbox
            int amount = Convert.ToInt32(amountClientTextBox.Text);
            StartServer(amount);
        }
        private void UpdateUI(string data) {
            var text = new TextBlock();
            text.Text = data;
            dashboardRegion.Children.Add(text);
        }
        private void Window_Closed(object sender, EventArgs e) {
            server.Close();
          
        }
    }
}
