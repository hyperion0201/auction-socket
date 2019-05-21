using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for AuctionResult.xaml
    /// </summary>
    public partial class AuctionResult : Window
    {
        public string bankID = "";
        public string cardID = "";

        public AuctionResult()
        {
            InitializeComponent();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            Regex bankIDRx = new Regex(@"^[A-Z]*$");
            Regex cardIDRx = new Regex(@"^[0-9]*$");

            if (bankIDTextbox.Text.Length != 3 || bankIDRx.IsMatch(bankIDTextbox.Text) == false)
            {
                MessageBox.Show("Wrond Bank ID! Bank ID must have 3 character (A to Z)");
                bankIDTextbox.Focus();
            }
            else
            {
                if (cardIDTextbox.Text.Length != 10 || cardIDRx.IsMatch(cardIDTextbox.Text) == false)
                {
                    MessageBox.Show("Wrong Card ID! Card ID must have 10 number(0 to 9)");
                    cardIDTextbox.Focus();
                }
                else
                {
                    bankID = bankIDTextbox.Text;
                    cardID = cardIDTextbox.Text;

                    this.DialogResult = true;
                }
            }
        }
    }
}
