﻿using System;
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

                    checkText.Text = $"Client connected on {remoteEP}";
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
                            Instance.sendAuctingbtn.IsEnabled = false;

                            MessageBox.Show("Out of time.");
                            // call onreceive
                            string rs = OnReceive(client);
                            MessageBox.Show(rs);
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

                    // enable timer

                    //wait for result notification

                    
                    
                    // Release the socket.    
                    

                } catch (ArgumentNullException ane) {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                } catch (SocketException se) {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                    checkText.Text = "Socket error.";
                } catch (Exception e) {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        private void OnSend(Socket socket, AuctionPacket auctPacket) {
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
            var item = productCombobox.SelectedItem as Product;
            AuctionPacket packetSend = new AuctionPacket() {
                Email = _email,
                ID = item.ID,
                Cost = newPriceTextBox.Text
            };
          
       
            OnSend(this.client, packetSend);
        }

        
    }
}
