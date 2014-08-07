// Copyright (c) 2014, Body Labs, Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
// COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
// BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS
// OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
// AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY
// WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

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
