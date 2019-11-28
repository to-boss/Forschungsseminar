using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    class BoxBody : INotifyPropertyChanged
    {
        private ulong trackingId;

        public BoxBody(Body body)
        {
            this.trackingId = body.TrackingId;
        }

        public ulong TrackindId
        {
            get { return this.trackingId; }
            set
            {
                if (this.trackingId != value)
                {
                    this.trackingId = value;
                    this.NotifyPropertyChanged("TrackingId");
                }
            }
        }

        public string Name
        {
            get
            {
                return "Body "+ trackingId % 1000;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
