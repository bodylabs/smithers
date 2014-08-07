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
using Smithers.Reading.FrameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace Smithers.Visualization
{
    public interface ISkeletonPresenter : FrameReaderCallbacks
    {
        Canvas Canvas { set; }
        Image Underlay { set; }
        ProjectionMode ProjectionMode { set; }
        CoordinateMapper CoordinateMapper { set; }
        bool ShowBody { set; }
        bool ShowHands { set; }
    }

    /// <summary>
    /// Presents a skeleton on a cavnas.
    /// </summary>
    public class SkeletonPresenter : ISkeletonPresenter
    {
        /// <summary>
        /// The default drawing color.
        /// </summary>
        static Color DEFAULT_COLOR = Colors.Red;
        static Color INFERRED_JOINT_COLOR = Colors.Yellow;

        /// <summary>
        /// The default circle radius.
        /// </summary>
        static double DEFAULT_ELLIPSE_RADIUS = 20;

        /// <summary>
        /// The default circle radius for hands
        /// </summary>
        static double DEFAULT_HANDS_ELLIPSE_RADIUS = 120;

        /// <summary>
        /// The default line thickness.
        /// </summary>
        static double DEFAULT_LINE_THICKNESS = 8;

        bool _showBody;
        bool _showHands;

        Dictionary<JointType, Ellipse> _joints;
        Dictionary<Tuple<JointType, JointType>, Line> _bones;
        Dictionary<JointType, Ellipse> _hands;

        /// <summary>
        /// The canvas to draw onto.
        /// </summary>
        public Canvas Canvas { get; set; }

        /// <summary>
        /// The image the canvas sits over. The presented skeleton is scaled
        /// along with this image.
        /// </summary>
        public Image Underlay { get; set; }

        /// <summary>
        /// The projection mode the presenter should use.
        /// </summary>
        public ProjectionMode ProjectionMode { get; set; }

        /// <summary>
        /// The coordinate mapper the presenter should use to translate from
        /// camera coordinates to color/depth coordinates.
        /// </summary>
        public CoordinateMapper CoordinateMapper { get; set; }

        /// <summary>
        /// The color to use when drawing the skeleton.
        /// </summary>
        public Color Color
        {
            set
            {
                Brush brush = new SolidColorBrush(value);

                foreach (Ellipse item in _joints.Values)
                    item.Fill = brush;

                foreach (Line item in _bones.Values)
                    item.Stroke = brush;

                foreach (Ellipse item in _hands.Values)
                {
                    item.Stroke = brush;
                    item.StrokeThickness = 4;
                }
            }
        }

        /// <summary>
        /// The radius of each joint.
        /// </summary>
        public double JointRadius
        {
            set
            {
                foreach (Ellipse item in _joints.Values)
                {
                    item.Width = value;
                    item.Height = value;
                }
            }
        }

        /// <summary>
        /// The radius of each hand.
        /// </summary>
        public double HandRadius
        {
            set 
            {
                foreach (Ellipse item in _hands.Values)
                {
                    item.Width = item.Height = value;
                }
            }
        }

        /// <summary>
        /// The thickness of each bone.
        /// </summary>
        public double BoneThickness
        {
            set
            {
                foreach (Line item in _bones.Values)
                    item.StrokeThickness = value;
            }
        }

        public SkeletonPresenter(Canvas canvas, Skeleton skeleton)
        {
            this.Canvas = canvas;

            _joints = new Dictionary<JointType, Ellipse>();
            foreach (JointType jointType in skeleton.Joints)
                _joints[jointType] = new Ellipse();

            _bones = new Dictionary<Tuple<JointType, JointType>, Line>();
            foreach (Tuple<JointType, JointType> bone in skeleton.Bones)
                _bones[bone] = new Line();

            _hands = new Dictionary<JointType, Ellipse>();
            _hands[JointType.HandLeft] = new Ellipse();
            _hands[JointType.HandRight] = new Ellipse();

            this.Color = SkeletonPresenter.DEFAULT_COLOR;
            this.JointRadius = SkeletonPresenter.DEFAULT_ELLIPSE_RADIUS;
            this.BoneThickness = SkeletonPresenter.DEFAULT_LINE_THICKNESS;

            this.HandRadius = SkeletonPresenter.DEFAULT_HANDS_ELLIPSE_RADIUS;
        }

        public SkeletonPresenter(Canvas canvas) : this(canvas, Skeleton.KinectSkeleton) { }
        public SkeletonPresenter(Skeleton skeleton) : this(null, skeleton) { }
        public SkeletonPresenter() : this(null, Skeleton.KinectSkeleton) { }

        public bool ShowBody
        {
            get
            {
                return _showBody;
            }
            set
            {
                _showBody = value;
                this.ClearSkeletons();
            }
        }

        public bool ShowHands
        {
            get
            {
                return _showHands;
            }
            set
            {
                _showHands = value;
                this.ClearHands();
            }
        }

        private Boolean PointIsValid(Point point)
        {
            return !double.IsNaN(point.X) && !double.IsNaN(point.Y) && !double.IsInfinity(point.X) && !double.IsInfinity(point.Y);
        }

        private Point PositionForJoint(Joint joint)
        {
            // 1. Project the joint position into color / depth coordinates
            Point position = this.ProjectionMode.ProjectCameraPoint(joint.Position, this.CoordinateMapper);

            // 2. Scale from color / depth coordinates to canvas coordinates
            position.X *= this.Underlay.ActualWidth / this.Underlay.Source.Width;
            position.Y *= this.Underlay.ActualHeight / this.Underlay.Source.Height;

            // 3. Translate from color / depth coordinates to canvas coordinates to cope with margins and padding
            position.X += (this.Canvas.ActualWidth - this.Underlay.ActualWidth) / 2.0f;
            position.Y += (this.Canvas.ActualHeight - this.Underlay.ActualHeight) / 2.0f;

            return position;
        }

        /// <summary>
        /// Draw a single hand
        /// </summary>
        /// <param name="hand"></param>
        private void DrawHand(Joint hand)
        {
            if (hand.TrackingState == TrackingState.NotTracked) return;

            Ellipse shape = _hands[hand.JointType];

            Point newPosition = PositionForJoint(hand);
            if (!PointIsValid(newPosition))
            {
                if (this.Canvas.Children.Contains(shape)) this.Canvas.Children.Remove(shape);
                return;
            }

            Canvas.SetLeft(shape, newPosition.X - shape.Width / 2);
            Canvas.SetTop(shape, newPosition.Y - shape.Width / 2);

            if (!this.Canvas.Children.Contains(shape)) this.Canvas.Children.Add(shape);
        }

        /// <summary>
        /// Draws an ellipse to the specified joint.
        /// </summary>
        /// <param name="joint">The joint represented by the ellipse.</param>
        private void DrawJoint(Joint joint)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;

            if (!_joints.ContainsKey(joint.JointType)) return;

            Ellipse shape = _joints[joint.JointType];

            Point newPosition = PositionForJoint(joint);

            if (!PointIsValid(newPosition))
            {
                if (this.Canvas.Children.Contains(shape)) this.Canvas.Children.Remove(shape);
                return;
            }

            Canvas.SetLeft(shape, newPosition.X - shape.Width / 2);
            Canvas.SetTop(shape, newPosition.Y - shape.Width / 2);

            if (joint.TrackingState == TrackingState.Inferred)
                shape.Fill = new SolidColorBrush(SkeletonPresenter.INFERRED_JOINT_COLOR);
            else
                shape.Fill = new SolidColorBrush(SkeletonPresenter.DEFAULT_COLOR);

            if (!this.Canvas.Children.Contains(shape)) this.Canvas.Children.Add(shape);
        }

        /// <summary>
        /// Draws a line connecting the specified joints.
        /// </summary>
        /// <param name="first">The first joint (start of the line).</param>
        /// <param name="second">The second joint (end of the line)</param>
        private void DrawBone(Joint first, Joint second)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            Line shape = _bones[new Tuple<JointType, JointType>(first.JointType, second.JointType)];

            Point firstPosition = PositionForJoint(first);
            Point secondPosition = PositionForJoint(second);

            if (!PointIsValid(firstPosition) || !PointIsValid(secondPosition))
            {
                if (this.Canvas.Children.Contains(shape)) this.Canvas.Children.Remove(shape);
                return;
            }

            shape.SetValue(Line.X1Property, firstPosition.X);
            shape.SetValue(Line.Y1Property, firstPosition.Y);
            shape.SetValue(Line.X2Property, secondPosition.X);
            shape.SetValue(Line.Y2Property, secondPosition.Y);

            if (!this.Canvas.Children.Contains(shape)) this.Canvas.Children.Add(shape);
        }

        /// <summary>
        /// Draw hands using bigger radius for the eclipse
        /// </summary>
        /// <param name="body"></param>
        private void DrawHands(Body body)
        {
            if (body == null) return;

            DrawHand(body.Joints[JointType.HandLeft]);
            DrawHand(body.Joints[JointType.HandRight]);
        }

        /// <summary>
        /// Draw the body on the canvas, using the current projection mode, and
        /// scaling to the current underlay.
        /// </summary>
        /// <param name="body"></param>
        private void DrawBody(Body body)
        {
            if (body == null) return;

            foreach (Tuple<JointType, JointType> bone in _bones.Keys)
                DrawBone(body.Joints[bone.Item1], body.Joints[bone.Item2]);

            foreach (Joint joint in body.Joints.Values)
                DrawJoint(joint);
        }

        /// <summary>
        /// Removes the skeleton from the canvas.
        /// </summary>
        private void ClearSkeletons()
        {
            foreach (Shape item in _joints.Values)
                this.Canvas.Children.Remove(item);

            foreach (Shape item in _bones.Values)
                this.Canvas.Children.Remove(item);
        }

        /// <summary>
        /// Removes the hand from the canvas
        /// </summary>
        private void ClearHands()
        {
            foreach (Shape item in _hands.Values)
                this.Canvas.Children.Remove(item);
        }

        public void FrameArrived(LiveFrame frame)
        {
            Body body = frame.FirstBody;

            if (body == null)
            {
                this.ClearSkeletons();
                this.ClearHands();
            }
            else
            {
                if (this.ShowBody) this.DrawBody(body);
                if (this.ShowHands) this.DrawHands(body);
            }
        }
    }
}
