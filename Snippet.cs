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
        /// <summary>
        /// OWAS Code as an array
        /// </summary>
        private int[] code;

        /// <summary>
        /// List of bodies tracked, one for each frame
        /// </summary>
        private List<Body> trackedBodyFrames;

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

        /// <summary>
        /// List of bodies tracked, one for each frame
        /// </summary>
        public List<Body> TrackedBodyFrames
        {
            get { return trackedBodyFrames; }
        }

        /// <summary>
        /// Point where the snippet begins
        /// </summary>
        public TimeSpan Beginning { get; set; }

        /// <summary>
        /// Point where the snippet ends
        /// </summary>
        public TimeSpan Ending { get; set; }

        /// <summary>
        /// The duration of the snippet
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return (Ending - Beginning);
            }
        }

        /// <summary>
        /// OWAS-Code of the back
        /// </summary>
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

        /// <summary>
        /// OWAS-Code of the arms
        /// </summary>
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

        /// <summary>
        /// OWAS-Code of the legs
        /// </summary>
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

        /// <summary>
        /// full OWAS-Code
        /// </summary>
        public int Code
        {
            get
            {
                return int.Parse(code[0].ToString() + code[1].ToString() + code[2].ToString());
            }
        }

        /// <summary>
        /// Adds a trackedBody to the list of trackedBodies
        /// </summary>
        /// <param name="trackedBody"></param>
        public void AddTrackedBody(Body trackedBody)
        {
            this.trackedBodyFrames.Add(trackedBody);
        }

        /// <summary>
        /// Prints the snippet as a XML
        /// </summary>
        public void PrintXml()
        {
            string fileName = Code.ToString() + "_" + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".xml";

            using (XmlWriter writer = XmlWriter.Create(fileName))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Snippet");
                writer.WriteElementString("Owas", this.Code.ToString());
                writer.WriteElementString("Beginning",this.Beginning.ToString());
                writer.WriteElementString("Ending", this.Ending.ToString());
                writer.WriteElementString("Duration", this.Duration.ToString());
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
