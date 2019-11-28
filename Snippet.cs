using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    public class Snippet
    {
        private int[] code;
        private List<Body> trackedBodyFrames;

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
            trackedBodyFrames = new List<Body>();
        }

        public List<Body> TrackedBodyFrames
        {
            get { return trackedBodyFrames; }
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
            return "Start: " + Beginning + ", End: " + Ending +", Duration: "+ Duration +", Code: " + Code;
        }

        public void AddTrackedBody(Body trackedBody)
        {
            this.trackedBodyFrames.Add(trackedBody);
        }

        public void PrintXml()
        {
            string fileName = Code.ToString() + "_" + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".xml";
            Debug.WriteLine(fileName);
            using (XmlWriter writer = XmlWriter.Create(fileName))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Snippet");
                writer.WriteElementString("Owas", this.Code.ToString());
                writer.WriteElementString("FrameCount", trackedBodyFrames.Count.ToString());

                foreach (Body body in this.TrackedBodyFrames)
                {
                    if (body.IsTracked)
                    {
                        writer.WriteStartElement("Frame");

                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        var jointPoints = new Dictionary<JointType, Point>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            writer.WriteStartElement(jointType.ToString());

                            Joint joint = joints[jointType];

                            if (joint.TrackingState == TrackingState.Tracked)
                            {
                                writer.WriteElementString("X", joint.Position.X.ToString());
                                writer.WriteElementString("Y", joint.Position.Y.ToString());
                            }

                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }
    }
}
