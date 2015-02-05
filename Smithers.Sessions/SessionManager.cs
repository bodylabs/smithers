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

using Smithers.Reading;
using Smithers.Reading.FrameData;
using Smithers.Reading.Calibration;
using Smithers.Serialization;
using Smithers.Serialization.Writers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Smithers.Sessions
{
    public class SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem> : EventArgs
        where TShotDefinition : ShotDefinition
        where TSavedItem : SavedItem
        where TShot : Shot<TShotDefinition, TSavedItem>
    {
        public TShot Shot { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }

        public SessionManagerEventArgs(TShot shot = null, string errorMessage = null, Exception exception = null)
        {
            this.Shot = shot;
            this.ErrorMessage = errorMessage;
            this.Exception = exception;
        }
    }

    public class SessionManager<TSession, TMetadata, TShot, TShotDefinition, TSavedItem> : FrameReaderCallbacks
        where TShotDefinition : ShotDefinition
        where TSavedItem : SavedItem, new()
        where TShot : Shot<TShotDefinition, TSavedItem>, new()
        where TMetadata : class
        where TSession : Session<TMetadata, TShot, TShotDefinition, TSavedItem>
    {
        TSession _session;

        TShot _nextShot;
        TShot _capturingShot;
        TShot _writingShot;

        Thread _serializationThread;
        MemoryManager<MemoryManagedFrame> _memoryManager;
        int _frameCount = 0;
        bool _stopButtonClicked = false;
        bool _finishingShot = false;

        object _lockObject = new object();
        Task<CalibrationRecord> _calibration;

        FrameReader _reader;

        FrameSerializer _serializer = new FrameSerializer();

        /// <summary>
        /// Fires when ready for a new shot.
        /// </summary>
        public event EventHandler<SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ReadyForShot;

        /// <summary>
        /// Fires before each shot is taken.
        /// </summary>
        public event EventHandler<SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotBeginning;

        /// <summary>
        /// Fires after each shot is taken, but before frames are written to disk.
        ///
        /// If an error occurs, ErrorMessage will be set.
        /// </summary>
        public event EventHandler<SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotCompletedSuccess;

        /// <summary>
        /// Fires after each shot is taken, but before frames are written to disk.
        ///
        /// If an error occurs, ErrorMessage will be set.
        /// </summary>
        public event EventHandler<SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotCompletedError;

        /// <summary>
        /// Fires after each shot is written to disk.
        /// 
        /// If an error occurs, ErrorMessage and Exception will be set.
        /// </summary>
        public event EventHandler<SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotSavedSuccess;

        /// <summary>
        /// Fires after each shot is written to disk.
        /// 
        /// If an error occurs, ErrorMessage and Exception will be set.
        /// </summary>
        public event EventHandler<SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotSavedError;

        /// <summary>
        /// Fires after the last shot is successfully written to disk.
        /// </summary>
        public event EventHandler<SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> LastShotFinished;

        public SessionManager(TSession session)
        {
            _session = session;

            // NOTE: The memory manager can´t be initialised here or it will use the default maxframecount of 50
            // _memoryManager = new MemoryManager(session.MaximumFrameCount);
            _serializationThread = new Thread(SerializationLoop);
            // _serializationThread.Start();
        }

        /// <summary>
        /// Get ready for the first shot.
        /// </summary>
        public void AttachToReader(FrameReader reader)
        {
            if (_reader != null)
                throw new InvalidOperationException("We've already attached to a reader!");

            _reader = reader;
            _reader.AddResponder(this);

            // We fire this event here to avoid doing actual work in the constructor,
            // and to give the caller a chance to register for the ReadyForShot event.
            
            // TODO: Readd this?
            // PrepareForNextShot();
        }

        /// <summary>
        /// Move to the next shot which needs to be completed. When a specific
        /// shot is provided, that logic is used instead.
        /// </summary>
        /// <returns></returns>
        public virtual void PrepareForNextShot(TShot shot = null)
        {
            if (shot != null) {
                if (!_session.Shots.Contains(shot))
                    throw new ArgumentException("Shot does not belong to this session");
                else if (shot.Completed)
                    throw new ArgumentException("Shot is already completed");

                _nextShot = shot;
            }
            else
            {
                if (_serializationThread.IsAlive)
                {
                    _serializationThread.Join();
                }
                
                _nextShot = _session.Shots.Find(x => !x.Completed);

                if (_nextShot != null)
                {
                    _memoryManager = new MemoryManager<MemoryManagedFrame>(_nextShot.ShotDefinition.MemoryFrameCount);
                    _serializationThread = new Thread(SerializationLoop);
                    _serializationThread.Start();
                }
            }

            if (_nextShot != null && ReadyForShot != null)
                ReadyForShot(this, new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_nextShot));
        }

        public virtual void CaptureShot()
        {
            if (_capturingShot != null)
                throw new InvalidOperationException("We're in the middle of capturing a shot!");
            else if (_writingShot != null)
                throw new InvalidOperationException("We're in the middle of writing a shot!");
            else if (_nextShot == null)
            {
                throw new InvalidOperationException("Capture is already finished");
            }

            _capturingShot = _nextShot;

            if (ShotBeginning != null)
                ShotBeginning(this, new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot));
        }

        public void SerializationLoop()
        {
            while (true)
            {
                MemoryManagedFrame frameToSerialize = _memoryManager.GetSerializableFrame();
                if (frameToSerialize == null)
                {
                    // TODO: wait some time (it is not suspected that serializing 
                    //       is ever faster than the 30fps of the Kinect coming in)
                    Thread.Sleep(0);
                    
                }
                else if (ReferenceEquals(frameToSerialize, _memoryManager.EndSerializationFrame))
                {
                    Console.WriteLine("Returning from Serialization Thread");
                    return;
                }
                else 
                {
                   // 
                    new Thread(() =>
                        {
                            SaveOneFrameToDisk(frameToSerialize);
                            _memoryManager.SetFrameAsWritable(frameToSerialize);
                            Console.WriteLine(_memoryManager.nWritableBuffers());
                        }).Start();
                    
                }
            }
        }

        public virtual bool ValidateShot(out string message)
        {
            message = null;
            return true;
        }
        
        public virtual void FrameArrived(LiveFrame frame)
        {
            bool bufferAvailable = true;
            // When the first frame arrive, start the calibration operation. This won't work
            // if we try to do it right after calling _sensor.Open().
            if (_calibration == null)
            {
                _calibration = Calibrator.CalibrateAsync(_reader.Sensor);
            }

            if (_capturingShot == null)
            {
               //  Console.WriteLine("_capturingShot == null");
                return;
            }

            // (1) Serialize frame data

            // Grab a reference to the current caturing shot because this can apparently
            // be accessed from a different thread or something
            // TODO: Check with the real thread pool later
            TShot capturingShotReference = _capturingShot;
            int nFramesToCapture = capturingShotReference.ShotDefinition.FramesToCapture;

            MemoryManagedFrame frameToWriteTo = _memoryManager.GetWritableBuffer();
            if (frameToWriteTo == null)
            {
                bufferAvailable = false;
                Console.WriteLine("There is no memory to write the frame data to, the capture ends now.");
            } 
            else
            {
                bufferAvailable = true;
                frameToWriteTo.Frame.Update(frame, _serializer);
                lock (_lockObject)
                {
                    frameToWriteTo.Index = _frameCount++;
                }

                _writingShot = capturingShotReference;
                _memoryManager.EnqueuSerializationTask(frameToWriteTo);
            }


            bool stopCapture = false;
            if (nFramesToCapture == -1)
            {
                stopCapture = _stopButtonClicked;
            }
            else 
            {
                stopCapture = _frameCount >= nFramesToCapture;
            }

            
            if (!stopCapture && bufferAvailable) return;    // Keep receiving frames

            // The Capture should be stopped, wait for serialization to end and prepare for new Shot to come in
            // (2) Move to the next shot

            // Signal to the serialization thread that it should finish serializing

            lock (_lockObject)
            {
                if (_finishingShot)
                {
                    return;
                } 
                else
                {
                    _finishingShot = true;
                }
            }
            _memoryManager.StopSerialization();

            string message;
            if (!ValidateShot(out message))
            {
                _memoryManager.ClearFrames();
                _frameCount = 0;
                var ea2 = new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot, message);

                _capturingShot = null;

                if (ShotCompletedError != null)
                    ShotCompletedError(this, ea2);

                return;
            }

            var ea = new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot, message);

            _writingShot = capturingShotReference;
            _capturingShot = null;

            // Wait for serialization to finish
            // TODO: Change this to manage a thread pool
            _serializationThread.Join();


            if (ShotCompletedSuccess != null)
                ShotCompletedSuccess(this, ea);

            FinishShot();
        }

        private void FinishShot()
        {
            _writingShot.Completed = true;

            /*
            string metadataPath = Path.Combine(_session.SessionPath, Session<TMetadata, TShot, TShotDefinition, TSavedItem>.METADATA_FILE);

            await Task.Run(() => JSONHelper.Instance.Serialize(_session.GetMetadata(), metadataPath));
            */

            // Clear the frames to make sure we don't use them again
            _memoryManager.ClearFrames();
            _frameCount = 0;

            var ea2 = new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_writingShot);


            if (ShotSavedSuccess != null)
                ShotSavedSuccess(this, ea2);

            // TODO: Set Writing Shot to Null once all the threads have joined. Not possible right now because we spawn
            //       a thread for each frame. Once a proper thread pool is implemented we can query that to see when we 
            //       are finished serializing.
            
            // _writingShot = null;

            PrepareForNextShot();

            // TODO: Maybe start new Serialization Thread elsewhere
            _serializationThread = new Thread(SerializationLoop);
            _serializationThread.Start();

            if (_nextShot == null)
            {
                if (LastShotFinished != null)
                    LastShotFinished(this, ea2);
            }
            _finishingShot = false;
        }

        public void StopCapture()
        {
            _stopButtonClicked = true;
        }

        protected virtual IEnumerable<IWriter> WritersForFrame(MemoryFrame frame, int frameIndex)
        {
            var writers = new List<IWriter>();

            // TODO: We probably need everyting except the Depth Mapping, but everything should be 
            // configurable in the GUI
            writers.Add(new ColorFrameWriter(frame, _serializer));                
            writers.Add(new DepthMappingWriter(frame, _serializer));
            writers.Add(new DepthFrameWriter(frame, _serializer));
            writers.Add(new InfraredFrameWriter(frame, _serializer));
            writers.Add(new SkeletonWriter(frame, _serializer));

            return writers;
        }

        protected virtual string GeneratePath(TShot shot, MemoryFrame frame, int frameIndex, IWriter writer)
        {
            string folderName = writer.Type.Name;

            string shotName = string.Format("Shot_{0:D3}", _session.Shots.IndexOf(shot) + 1);

            // 0 -> Frame_001, 1 -> Frame_002, etc.
            string frameName = string.Format("Frame_{0:D3}", frameIndex + 1);

            string fileName = string.Format(
                "{0}{1}{2}{3}",
                shotName,
                shotName == null ? "" : "_",
                frameName,
                writer.FileExtension
            );

            return Path.Combine(folderName, fileName);
        }

        public void DeleteShot(TShot shot)
        {
            if (!_session.Shots.Contains(shot))
                throw new ArgumentException("Shot does not belong to this session");

            foreach (SavedItem item in shot.SavedItems)
            {
                string path = Path.Combine(_session.SessionPath, item.Path);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            shot.SavedItems.Clear();
        }


        private async void SaveOneFrameToDisk(MemoryManagedFrame memoryBlockToSerialize) 
        {
            // Perform calibration if we haven't already
            CalibrationRecord record = await _calibration;

            var preparedWriters = PrepareWritersForOneFrame(_writingShot, memoryBlockToSerialize);

            foreach (Tuple<IWriter, TSavedItem> preparedWriter in preparedWriters)
            {
                IWriter writer = preparedWriter.Item1;
                TSavedItem savedItem = preparedWriter.Item2;

                string path = Path.Combine(_session.SessionPath, savedItem.Path);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    writer.Write(stream);
                    stream.Close();
                }

                _writingShot.SavedItems.Add(savedItem);
            }
        }

        protected virtual IEnumerable<Tuple<IWriter, TSavedItem>> PrepareWritersForOneFrame(TShot shot, MemoryManagedFrame frame)
        {
            var preparedWriters = new List<Tuple<IWriter, TSavedItem>>();
            MemoryFrame memoryFrame = frame.Frame;
            int index = frame.Index;

            if (index == 0)
            {
                IWriter calibrationWriter = new CalibrationWriter(_calibration.Result);

                preparedWriters.Add(new Tuple<IWriter, TSavedItem>(
                    calibrationWriter,
                    new TSavedItem()
                    {
                        Type = calibrationWriter.Type,
                        Timestamp = calibrationWriter.Timestamp,
                        Path = calibrationWriter.Type.Name + calibrationWriter.FileExtension,
                    }
                ));
            }

            // TODO: Remove this completely after testing with the new Thread Scheme
            // Console.WriteLine("blablabla {0}", index);

            foreach (IWriter writer in WritersForFrame(memoryFrame, index))
            {
                preparedWriters.Add(new Tuple<IWriter, TSavedItem>(
                    writer,
                    new TSavedItem()
                    {
                        Type = writer.Type,
                        Timestamp = writer.Timestamp,
                        Path = GeneratePath(shot, memoryFrame, index, writer),
                    }
                ));
            }

            return preparedWriters;
        }
    }
}
