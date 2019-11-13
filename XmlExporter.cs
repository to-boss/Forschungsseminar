using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    public class XmlExporter
    {
        private XmlWriter writer;

        public XmlExporter()
        {

        }

        public void StartExport(Snippet snippet)
        {
            using(writer = XmlWriter.Create("test.xml"))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Snippet");
                writer.WriteElementString("OWAS",snippet.Code.ToString());

                foreach(Body body in snippet.TrackedBodyFrames)
                {
                    if (body.IsTracked)
                    {
                        writer.WriteStartElement("frame");

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
