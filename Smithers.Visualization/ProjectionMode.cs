using Microsoft.Kinect;
using Smithers.Reading.FrameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Smithers.Visualization
{
    public class ProjectionMode
    {
        public static readonly ProjectionMode COLOR_IMAGE = new ProjectionMode(Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT, 1063.23, 1063.84, false);
        public static readonly ProjectionMode DEPTH_IMAGE = new ProjectionMode(Frame.DEPTH_INFRARED_WIDTH, Frame.DEPTH_INFRARED_HEIGHT, 360, 360, true);
        public static readonly ProjectionMode INFRARED_IMAGE = DEPTH_IMAGE;

        public double Width { get; private set; }
        public double Height { get; private set; }
        public double Alpha { get; private set; }
        public double Beta { get; private set; }

        private bool _useDepthMapper;

        ProjectionMode(double width, double height, double alpha, double beta, bool useDepthMapper)
        {
            this.Width = width;
            this.Height = height;
            this.Alpha = alpha;
            this.Beta = beta;
            _useDepthMapper = useDepthMapper;
        }

        /// <summary>
        /// Project the camera space point using the given sensor coordinate mapper.
        /// </summary>
        /// <param name="inPoint"></param>
        /// <param name="coordinateMapper"></param>
        /// <returns></returns>
        public Point ProjectCameraPoint(CameraSpacePoint inPoint, CoordinateMapper coordinateMapper)
        {
            if (_useDepthMapper)
            {
                DepthSpacePoint depthPoint = coordinateMapper.MapCameraPointToDepthSpace(inPoint);
                return new Point(depthPoint.X, depthPoint.Y);
            }
            else
            {
                ColorSpacePoint colorPoint = coordinateMapper.MapCameraPointToColorSpace(inPoint);
                return new Point(colorPoint.X, colorPoint.Y);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left">Min x in pixels</param>
        /// <param name="right">Max x in pixels</param>
        /// <param name="bottom">Min y in pixels</param>
        /// <param name="top">Max y in pixels</param>
        /// <param name="near">Near clipping plane in world units</param>
        /// <param name="far">Far clipping plane in world units</param>
        /// <param name="alpha">Supposed field of view width of camera</param>
        /// <param name="beta">Supposed field of view height of camera</param>
        /// <returns></returns>
        private static Matrix3D CreatePerspectiveProjectionMatrix(double left, double right, double bottom, double top, double near, double far, double alpha, double beta)
        {
            Matrix3D projectionMatrix = new Matrix3D();

            projectionMatrix.M11 = 2.0 * alpha / (right - left);
            projectionMatrix.M22 = 2.0 * beta / (top - bottom);
            projectionMatrix.M31 = -0.01;
            projectionMatrix.M32 = 0.03;
            projectionMatrix.M33 = (far + near) / (near - far);
            projectionMatrix.M34 = -1.0;
            projectionMatrix.OffsetZ = near * far / (near - far);
            projectionMatrix.M44 = 0;

            return projectionMatrix;
        }

        /// <summary>
        /// Get a perspective camera for this projection.
        /// </summary>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public MatrixCamera CreatePerspectiveCamera(double near, double far)
        {
            Matrix3D projectionMatrix = CreatePerspectiveProjectionMatrix(0, this.Width, 0, this.Height, near, far, this.Alpha, this.Beta);
            return new MatrixCamera(Matrix3D.Identity, projectionMatrix);
        }
    }
}
