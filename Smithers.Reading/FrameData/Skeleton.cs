using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Reading.FrameData
{
    /// <summary>
    /// A collection of bones.
    /// </summary>
    public class Skeleton
    {
        /// <summary>
        /// The standard Kinect skeleton.
        /// </summary>
        public static readonly Skeleton KinectSkeleton = new Skeleton(new Tuple<JointType, JointType>[]{
            new Tuple<JointType, JointType>(JointType.Head, JointType.Neck),
            new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder),
            new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft),
            new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight),
            new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid),
            new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft),
            new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight),
            new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft),
            new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight),
            new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft),
            new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight),
            new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft),
            new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight),
            new Tuple<JointType, JointType>(JointType.HandLeft, JointType.ThumbLeft),
            new Tuple<JointType, JointType>(JointType.HandRight, JointType.ThumbRight),
            new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase),
            new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft),
            new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight),
            new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft),
            new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight),
            new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft),
            new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight),
            new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft),
            new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight),
        });

        /// <summary>
        /// Return all the bones in the skeleton.
        /// </summary>
        public IEnumerable<Tuple<JointType, JointType>> Bones { get; private set; }

        /// <summary>
        /// Convenience method. Return all the joints in the skeleton.
        /// </summary>
        public IEnumerable<JointType> Joints { get; private set; }

        public Skeleton(Tuple<JointType, JointType>[] bones)
        {
            Bones = bones.ToList();

            HashSet<JointType> joints = new HashSet<JointType>();
            foreach (Tuple<JointType, JointType> bone in bones) {
                joints.Add(bone.Item1);
                joints.Add(bone.Item2);
            }
            Joints = joints.ToList();
        }
    }
}
