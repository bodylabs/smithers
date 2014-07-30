using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Smithers.Reading
{
    [Serializable()]
    public class DependencyException : System.Exception
    {
        public DependencyException() : base() { }
        public DependencyException(string message) : base(message) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected DependencyException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }

    public static class DependencyChecker
    {
        const string PUBLIC_PREVIEW_VERSION_PREFIX = "2.0.1407";
        const string NUIDB_SOURCE_FOLDER = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0\ExtensionSDKs\Microsoft.Kinect.Face\2.0\Redist\CommonConfiguration\x64\NuiDatabase";
        
        public static void CheckDependencies()
        {
            CheckKinectSDK();
        }

        private static void CopyAll(string SourcePath, string DestinationPath)
        {
            string[] directories = System.IO.Directory.GetDirectories(SourcePath, "*.*", SearchOption.AllDirectories);

            Parallel.ForEach(directories, dirPath =>
            {
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));
            });

            string[] files = System.IO.Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories);

            Parallel.ForEach(files, newPath =>
            {
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath));
            });
        }

        /// <summary>
        /// Copy NuiDatabase to the application folder. Should be removed once the Face API DLL include reference to NuiDatabase Folder
        /// </summary>
        public static void EnsureNuiDatabase()
        {
            if (!Directory.Exists(NUIDB_SOURCE_FOLDER))
            {
                throw new DependencyException("Could not locate Microsoft NUI database. Please make sure the Kinect SDK is installed correctly.");
            }

            string exeFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string nuiDbTargetFolder = Path.Combine(exeFolder, "NuiDatabase");

            if (!Directory.Exists(nuiDbTargetFolder))
            {
                DependencyChecker.CopyAll(NUIDB_SOURCE_FOLDER, nuiDbTargetFolder);
            }
        }

        public static void CheckKinectSDK()
        {
            try
            {
                // If the SDK is removed but the app somehow is started anyway,
                // this line will raise a FileNotFoundException.
                Assembly assembly = Assembly.GetAssembly(typeof(Microsoft.Kinect.KinectSensor));
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);

                if (!info.FileVersion.StartsWith(PUBLIC_PREVIEW_VERSION_PREFIX))
                {
                    throw new DependencyException("Incompatible Kinect v2 SDK detected. Please make sure you are using the latest SDK");
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                throw new DependencyException("Kinect 2 SDK is not installed.");
            }
        }
    }
}
