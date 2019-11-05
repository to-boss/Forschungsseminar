using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    class Snippet
    {
        private int[] code;

        public Snippet()
        {
            code = new int[3];
        }

        /// <summary>
        /// Create a Snippet with begging at currentPlaybackTime
        /// </summary>
        /// <param name="currentPlaybackTime"></param>
        public Snippet(TimeSpan currentPlaybackTime)
        {
            code = new int[3];
            Beginning = currentPlaybackTime;
        }

        public TimeSpan Beginning { get; set; }

        public TimeSpan Ending { get; set; }

        public TimeSpan Duration
        {
            get
            {
                return (Ending - Beginning);
            }
        }

        public int CodeBack
        {
            get
            {
                return code[0];
            }
            set
            {
                code[0] = value;
            }
        }

        public int CodeArms
        {
            get
            {
                return code[1];
            }
            set
            {
                code[1] = value;
            }
        }

        public int CodeLegs
        {
            get
            {
                return code[2];
            }
            set
            {
                code[2] = value;
            }
        }

        public int Code
        {
            get
            {
                return int.Parse(code[0].ToString() + code[1].ToString() + code[2].ToString());
            }
        }

        public string InfoAsString()
        {
            return "Start: " + Beginning + ", End: " + Ending +", Duration: "+ Duration;
        }
    }
}
