﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    public class AuctionModel {
        public string AuctionEmail { get; set; }
        public string AuctionCost { get; set; }
    }
    public class Product :INotifyPropertyChanged {
        private string _id;
        private string _productname;
        private string _begincost;
        public List<AuctionModel> auctioner = new List<AuctionModel>();
        public string ID {
            get { return _id; } set {
                _id = value;
                NotifyChange("ID");
            } }
        public string ProductName { get { return _productname; } set {
                _productname = value;
                NotifyChange("ProductName");
            } }
        public string BeginCost { get { return _begincost; } set {
                _begincost = value;
                NotifyChange("BeginCost");
            } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyChange(string v) {
            if (PropertyChanged!=null) {
                PropertyChanged(this, new PropertyChangedEventArgs(v));
            }
        }
    }
}
