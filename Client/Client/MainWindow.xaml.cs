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

namespace Client {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 



    public partial class MainWindow : Window {
        private const int port = 8080;
        // Thread signal.  

        public static MainWindow Instance { get; private set; }
        public MainWindow() {
            InitializeComponent();

            Instance = this;

        }
       
        private void StartClient(string email) {
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
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try {
                    // Connect to Remote EndPoint  
                    sender.Connect(remoteEP);

                    checkText.Text = $"Client connected on {remoteEP}";

                    
                    // Encode the data string into a byte array.    
                    byte[] msg = Encoding.ASCII.GetBytes($"{email}\n");
                    // get data

                    // Send the data through the socket.    
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.    
                    int bytesRec = sender.Receive(bytes);
                    TextBlock res = new TextBlock();
                    res.Text = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    clientRegion.Children.Add(res);
                    // Release the socket.    
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

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

        private void SendReq(object sender, RoutedEventArgs e) {
           
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // request email for textbox
            string email = "";
            var connectScreen = new Connect();
            if (connectScreen.ShowDialog() == true) {
                email = connectScreen.Email;
            }
            StartClient(email);
        }
    }
}
