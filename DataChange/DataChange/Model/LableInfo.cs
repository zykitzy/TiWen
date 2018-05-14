using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataChange
{
    public class LableInfo : INotifyPropertyChanged
    {
        private string lableid;
        public event PropertyChangedEventHandler PropertyChanged;
        public string LableID
        {
            get
            {
                return lableid;
            }
            set
            {
                lableid = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LableID"));
            }
        }
        private string lablevalue;
        public string LableValue
        {
            get
            {
                return lablevalue;
            }
            set
            {
                lablevalue = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LableValue"));
            }
        }
        private string convretvalue;
        public string ConvertValue
        {
            get
            {
                return convretvalue;
            }
            set
            {
                convretvalue = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ConvertValue"));
            }
        }

    }
}
