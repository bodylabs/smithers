﻿// Copyright (c) 2014, Body Labs, Inc.
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
        const string KINECT_FACE_ASSEMBLY_PUBLIC_PREVIEW_PATH = @"C:\Program Files\Microsoft SDKs\Kinect\v2.0-PublicPreview1407\Assemblies\Microsoft.Kinect.Face.dll";
        const string KINECT_FACE_ASSEMBLY_07_30_PATH = @"C:\Program Files\Microsoft SDKs\Kinect\MainV2\Assemblies\Microsoft.Kinect.Face.dll";

        public static void CheckDependencies()
        {
            CheckKinectSDK();
            LoadKinectFaceAssembly();
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

        public static void LoadKinectFaceAssembly()
        {
            string exeFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string targetPath = Path.Combine(exeFolder, "Microsoft.Kinect.Face.dll");

            string sourcePath = "";

            if (!File.Exists(targetPath))
            {
                if (File.Exists(KINECT_FACE_ASSEMBLY_PUBLIC_PREVIEW_PATH))
                {
                    sourcePath = KINECT_FACE_ASSEMBLY_PUBLIC_PREVIEW_PATH;

                }
                else if(File.Exists(KINECT_FACE_ASSEMBLY_07_30_PATH))
                {
                    sourcePath = KINECT_FACE_ASSEMBLY_07_30_PATH;

                }
                else
                {
                    throw new DependencyException("Kinect Face Assembly Not Found. Please Install the Latest SDK");
                }

                File.Copy(sourcePath, targetPath);
            }

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
