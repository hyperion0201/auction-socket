using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Client {
    
        public class Product : INotifyPropertyChanged {
            private string _id;
            private string _productname;
            private string _begincost;

            public string ID {
                get { return _id; }
                set {
                    _id = value;
                    NotifyChange("ID");
                }
            }
            public string ProductName {
                get { return _productname; }
                set {
                    _productname = value;
                    NotifyChange("ProductName");
                }
            }
            public string BeginCost {
                get { return _begincost; }
                set {
                    _begincost = value;
                    NotifyChange("BeginCost");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyChange(string v) {
                if (PropertyChanged != null) {
                    PropertyChanged(this, new PropertyChangedEventArgs(v));
                }
            }
        }

    }
