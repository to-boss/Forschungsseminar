using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    public class BodiesArrivedEventArgs: EventArgs
    {
        private readonly List<Body> bodies;

        public BodiesArrivedEventArgs(List<Body> bodies)
        {
            this.bodies = bodies;
        }

        public List<Body> Bodies
        {
            get { return bodies; }
        }
    }
}
