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
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Client {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 



    public partial class MainWindow : Window {
        private const int port = 8080;
        private Socket client = null;
        private string _email = null;
        private bool isSent = false;
       
        // Thread signal.  
        List<Product> productList = null;
        public static MainWindow Instance { get; private set; }
        public MainWindow() {   
            InitializeComponent();

            Instance = this;

        }

       
        private string OnReceive(Socket socket) {
            byte[] bytes = new byte[1024];
            int recMsg = socket.Receive(bytes);
            string text = Encoding.ASCII.GetString(bytes, 0, recMsg);
            return text;
        }
        private void UpdateTimeUI(string s) {
            timeText.Text = s;
        }
        
        private void StartClient(string email) {
            productList = new List<Product>();
            byte[] bytes = new byte[1024];
            try {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  

                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP  socket.    
                 client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try {
                    // Connect to Remote EndPoint  
                    client.Connect(remoteEP);

                    checkText.Text = $"{email} connected on {remoteEP}";
                    // create timer thread
                    int timeRemain = 30;

                    Instance.Dispatcher.Invoke(() => Instance.UpdateTimeUI($"{timeRemain}"));
                    DispatcherTimer timeTracker = new DispatcherTimer();
                    timeTracker.Interval = TimeSpan.FromSeconds(1);
                    timeTracker.Tick += (sender, e) => {
                        timeRemain--;
                        Instance.Dispatcher.Invoke(() => Instance.UpdateTimeUI($"{timeRemain}"));
                        if (timeRemain == 0) {
                            timeTracker.Stop();
                            // send an anonymous packet
                            OnSend(client, null);
                            Instance.sendAuctingbtn.IsEnabled = false;
                           
                            // receive result from client

                            string rsReceive = OnReceive(client);
                            TextBlock result = new TextBlock {
                                Text = $"Result : {rsReceive}"
                            };
                            result.Margin = new Thickness(20, 20, 0, 0);
                            clientRegion.Children.Add(result);


                            if (rsReceive=="You win!") {
                                var auctionResult = new AuctionResult();
                                if (auctionResult.ShowDialog() == true) {
                                    var bankID = auctionResult.bankID;
                                    var cardID = auctionResult.cardID;
                                    byte[] payment = Encoding.ASCII.GetBytes($"{bankID}/{cardID}");
                                    client.Send(payment);
                                    string paymentRs = OnReceive(client);
                                    clientRegion.Children.Add(new TextBlock {
                                        Text = paymentRs,
                                        Margin = new Thickness(20,20,0,0)
                                    });
                                }
                            }
                            else {
                                OnSend(client, null);
                            }
                            
                        }

                    };

                    // receive kick msg or not
                    int byteKickRec = client.Receive(bytes);
                   string firstmsg = Encoding.ASCII.GetString(bytes, 0, byteKickRec);
                    if (firstmsg!=null) {
                        timeTracker.Start();
                        MessageBox.Show(firstmsg);
                    }
                    
                    byte[] msg = Encoding.ASCII.GetBytes($"{email}");
              

                    // Send email through the socket.    
                    int bytesSent = client.Send(msg);

                    // Receive the response from the remote device.    
                    int bytesRec = client.Receive(bytes);
                    TextBlock res = new TextBlock();
                    res.Text = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (res.Text == "Duplicate email.") {
                        clientRegion.Children.Add(res);
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        return;
                    }
                    

                    // receive product
                    StreamWriter sw = new StreamWriter("product.txt");
                    int productRec = client.Receive(bytes);
                    string productStream = Encoding.ASCII.GetString(bytes, 0, productRec) ;
                    sw.Write(productStream);
                    productList = Product.ParseProduct(productStream);
                    productBoard.ItemsSource = productList;
                    productCombobox.ItemsSource = productList;

                   
                    

                } catch (ArgumentNullException ane) {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                } catch (SocketException se) {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                    checkText.Text = "There was an error when connecting to server.";
                } catch (Exception e) {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private void OnSend(Socket socket, AuctionPacket auctPacket) {
            if (auctPacket == null) {
                byte[] sendMsg = Encoding.ASCII.GetBytes("null/null/null");
                socket.Send(sendMsg);
                return;
            }
            
            if (socket.Connected==true) {
                //get data from packet
               
                byte[] sendMsg = Encoding.ASCII.GetBytes($"{auctPacket.ID}/{auctPacket.Email}/{auctPacket.Cost}");
                socket.Send(sendMsg);
            }
        }
      
        private void Window_Loaded(object client, RoutedEventArgs e) {
            // request email for textbox
            string email = "";
            var connectScreen = new Connect();
            if (connectScreen.ShowDialog() == true) {
                email = connectScreen.Email;
                _email = email;
            }
            StartClient(email);
        }

        private void SendAuction_Click(object client, RoutedEventArgs e) {

            double newPrice = int.Parse(newPriceTextBox.Text);
            var item = productCombobox.SelectedItem as Product;
            if (double.Parse(item.BeginCost) >= newPrice)
            {
                MessageBox.Show("You can't pay a lower price than origin price.");
                newPriceTextBox.Focus();
            }
            else
            {
                AuctionPacket packetSend = new AuctionPacket()
                {
                    Email = _email,
                    ID = item.ID,
                    Cost = newPriceTextBox.Text
                };

                OnSend(this.client, packetSend);
                sendAuctingbtn.IsEnabled = false;
                Product p = GetProductFromID(packetSend.ID);
                productList.Remove(p);
                productBoard.ItemsSource = null;
                productBoard.ItemsSource = productList;
                isSent = true;
            }
        }

        private Product GetProductFromID(string id) {
            for (int i =0;i<productList.Count;i++) {
                if (productList[i].ID == id) return productList[i];
            }
            return new Product();
        }
        
    }
}
