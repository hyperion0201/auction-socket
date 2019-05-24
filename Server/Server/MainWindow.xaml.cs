using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Timers;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.IO;

using Timer = System.Timers.Timer;

namespace Server {

    #region ResultModel to hold the auction results
    public class ResultModel {
        public string Email { get; set; }
        public string Product { get; set; }

    }
    #endregion
    public partial class MainWindow : Window {


        #region Global Variables Declaration

        private const int port = 8080; 
        public Socket server = null;
        public string data = null;
        private int maxClient = 0;
        private int queueTime = 30;
        private Thread handler = null;
        private int connectedAmount = 0;
        private List<ClientModel> clientList = null;
        private List<Product> productList = null;
        private List<ResultModel> winClients = null;
        Timer timeTracker = null;
        
        private int endAuctionFlag = 0;
        public static MainWindow Instance { get; private set; }
        #endregion
        public MainWindow() {
            InitializeComponent();
            Instance = this;
        }
       
        private void StartServer(int maxClient) {
            clientList = new List<ClientModel>();
            productList = new List<Product>();
            this.maxClient = maxClient;
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

            IPHostEntry host = Dns.GetHostEntry("THINKPAD-L540");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEP = new IPEndPoint(ipAddress, port);
            server = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(localEP);
            
            status.Text = $"Server started on {localEP}\nWaiting for connection...";
            
            new Thread(() => StartListening(server, maxClient)).Start();
            
            
        }

       
        private void StartListening(Socket socket, int maxClient) {
            try {
                socket.Listen(maxClient);
                Accept(socket);
            }
          catch (SocketException e) {
                Debug.WriteLine(e.Message);
            }
        }

        private void OnSend(Socket socket, string s) {
            byte[] msg = Encoding.ASCII.GetBytes(s);
            socket.Send(msg);
        }

        
        private  void Accept(Socket socket) {
            int timeFlag = 1;
           
            while (true) {
                    Socket accepted = socket.Accept();
                
               Instance.Dispatcher.Invoke(()=> timeText.Text = $"{queueTime}");
                    if (connectedAmount < maxClient && timeFlag==1) {
                    connectedAmount++;

                    Instance.queueTime = 30;
                    if (Instance.timeTracker!=null) {
                        Instance.timeTracker.Stop();
                        Instance.timeTracker = null;
                    }
                     timeTracker = new Timer();
                    timeTracker.Interval = 1000;
                    timeTracker.Elapsed += (sender, e) => {
                        Instance.queueTime--;
                        Instance.Dispatcher.Invoke(() => Instance.timeText.Text = $"{Instance.queueTime}");

                        if (Instance.queueTime == 0) {
                            timeTracker.Stop();
                            Instance.endAuctionFlag = 1;

                            //Send out of time msg
                            MessageBox.Show("Time is over.");

                            timeFlag = 0;

                        }
                    };
                    timeTracker.Start();

                    // send connect response
                    byte[] okmsg = Encoding.ASCII.GetBytes("Connected to server.");
                    accepted.Send(okmsg);

                    //create new thread

                     handler = new Thread(() => {
                        HandleIncomingClient(accepted);
                    });
                    handler.Start();
                    
                }
                else {
                    // send kick msg to client
                    byte[] msg = Encoding.ASCII.GetBytes("Kicked from server.");
                    accepted.Send(msg);
                    accepted.Shutdown(SocketShutdown.Both);
                    accepted.Close();
                    return;
                }
 
            }
          
        }
        

        private void GetProductIndex(List<Product> list, string s) {
            string[] arr = s.Split('/');
            for (int i =0;i<list.Count;i++) {
                if (arr[0] == list[i].ID) {
                    productList[i].auctioner.Add(new AuctionModel() {
                        AuctionEmail = arr[1],
                        AuctionCost = arr[2]
                    });
                    return;
                };
               
            }
            
        }
        private static void HandleIncomingClient(Socket socket) {
            //int actTime = 60;
            
            string clientEmail = "";
            string data = null;
            byte[] bytes = new byte[1024];
            int bytesRec = socket.Receive(bytes);
            data = Encoding.ASCII.GetString(bytes, 0, bytesRec); // email str receive from client
            clientEmail = data;
           
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

            //bypass next receving from client
            byte[] msg = Encoding.ASCII.GetBytes(data);
            socket.Send(msg);
            // send product file to client
            byte[] product = File.ReadAllBytes(System.IO.Path.GetFullPath("product.txt"));
            socket.Send(product);

                try {

                // receive new auctionpacket as string : email, id, cost
                    int auctByte = socket.Receive(bytes);
                
                    
                    string newAuct = Encoding.ASCII.GetString(bytes, 0, auctByte);
                Instance.Dispatcher.Invoke(() => Instance.GetProductIndex(
                    Instance.productList, newAuct
                    ));
                
                    Instance.Dispatcher.Invoke(() => Instance.AddAuctionToBoard(newAuct));
                }

                catch (SocketException se) {
                    Debug.WriteLine(se.Message);
                }
            // send result
            while(true) {
                if (Instance.endAuctionFlag == 1) {
                    // create winning list
                    Instance.Dispatcher.Invoke(() => Instance.CreateWinningList());

                    // check if current email in winning list?
                    bool isWin = Instance.Dispatcher.Invoke(() => Instance.IsWinning(clientEmail));
                    if (isWin) {
                        // send a toast to winner
                        Instance.Dispatcher.Invoke(() => Instance.OnSend(socket, "You win!"));
                        //update log
                        

                    } else {
                        Instance.Dispatcher.Invoke(() => Instance.OnSend(socket, "You lose!"));
                    }

                    // receive payment information

                    int paymentByte = socket.Receive(bytes);
                    string paymentStr = Encoding.ASCII.GetString(bytes, 0, paymentByte);
                    if (paymentStr!="null/null/null") {
                        Instance.Dispatcher.Invoke(() => Instance.OnSend(socket, "Payment successful!"));
                    } 
                    break;
                }
                    
            }
           

        }

        private bool IsWinning(string email) {
            for (int i =0;i<winClients.Count;i++) {
                if (email == winClients[i].Email) return true;
            }
            return false;
        }
         
        private int GetWinner(List<AuctionModel> auctioner) {
            int lastauction = Convert.ToInt32(auctioner[auctioner.Count - 1].AuctionCost);
            for (int i =0;i<auctioner.Count-1;i++) {
                if (Convert.ToInt32(auctioner[i].AuctionCost) == lastauction) return i;
            }
            return auctioner.Count - 1;
        }
       
        private void CreateWinningList() {
            winClients = new List<ResultModel>();
            for (int i = 0; i < productList.Count; i++) {
                if (productList[i].auctioner.Count > 0) {
                    // sort auctioner asc
                    productList[i].auctioner.OrderBy(product => product.AuctionCost).ToList();

                    int winnerindex = GetWinner(productList[i].auctioner);

                    winClients.Add(new ResultModel() {
                        Email = productList[i].auctioner[winnerindex].AuctionEmail,
                        Product = productList[i].ProductName
                    });


                }
            }
        }
        private void StartServer(object sender, RoutedEventArgs e) {
            //get number from txtbox
            try {
                int amount = Convert.ToInt32(amountClientTextBox.Text);
                StartServer(amount);
                startServerbtn.IsEnabled = false;
                stopServerbtn.IsEnabled = true;
            }
            catch(FormatException fe) {
                MessageBox.Show($"{fe.Message}");
            }
            
        }
        private void AddAuctionToBoard(string data) {
            TextBlock t = CreateTextBlock(data);
            t.Margin = new Thickness(20, 0, 0, 0);
            auctionRegion.Children.Add(t);
        }
        private void UpdateUI(string email) {
            var text = new TextBlock {
                Text = $"{email} connected on {server.LocalEndPoint}"
            };
            connectlogBoard.Children.Add(text);
        }

        private TextBlock CreateTextBlock(string s) {
            string[] arr = s.Split('/');
            var t = new TextBlock {
                Text = $"{arr[1]} choose product ID {arr[0]} with new cost : {arr[2]}"
            };
            return t;
        }
        private void Window_Closed(object sender, EventArgs e) {
          
            if (server == null) return;
            server.Close();
            handler.Abort();
            Application.Current.Shutdown();
            
        }

        private void StopServer(object sender, RoutedEventArgs e) {
            if (server!=null) {
                // try to shutdown socket
                try {
                   // server.Shutdown(SocketShutdown.Both);
                    server.Close();
                    startServerbtn.IsEnabled = true;
                    stopServerbtn.IsEnabled = false;
                    // change status text
                    status.Text = $"Server stopped.";
                    if (timeTracker != null) timeTracker.Stop();

                }
               catch (SocketException se) {
                    MessageBox.Show($"{se.Message}");
                }
            }
        }

        private void ClearBoard(object sender, RoutedEventArgs e) {
            auctionRegion.Children.Clear();
            connectlogBoard.Children.Clear();
        }
    }
}

/*
 * 
 */