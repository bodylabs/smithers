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
using System.Diagnostics;
using System.Timers;

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

    public class GUIUpdateEventArgs : EventArgs
    {
        public double MinFPS { get; set; }
        public double AverageFPS { get; set; }
        public double MaxTimeDeleta { get; set; }
        public double PercentageBuffer { get; set; }
        public String Blabla { get; set; }

        public GUIUpdateEventArgs(double minFPS, double maxTimeDelta, double averageFPS, double percentage_buffer, string message)
        {
            this.MinFPS = minFPS;
            this.AverageFPS = averageFPS;
            this.MaxTimeDeleta = maxTimeDelta;
            this.PercentageBuffer = percentage_buffer;
            this.Blabla = message;
        }
    }



    /// <summary>
    /// This task is performed by the thread pool
    /// </summary>
    class SerializationTask : Smithers.Sessions.PoolTask
    {
      private MemoryManager<MemoryManagedFrame> _internal_memory_manager;
      private SerializeFrameToDiskDelegate _serializeOneFrameToDisk;

      /// <summary>
      /// The serialization callback definition
      /// </summary>
      public delegate void SerializeFrameToDiskDelegate(MemoryManagedFrame frame);


      public SerializationTask(
        MemoryManager<MemoryManagedFrame> internal_memory_manager,
        SerializeFrameToDiskDelegate serialisation_delegate)
      {
        _internal_memory_manager = internal_memory_manager;
        _serializeOneFrameToDisk = serialisation_delegate;
      }

      public override bool IsTaskEmpty() 
      {
        return _internal_memory_manager.Capacity == _internal_memory_manager.NbFreeBuffers();
      }

      public override void PerformNextTask()
      {
        MemoryManagedFrame frameToSerialize = _internal_memory_manager.GetSerializableFrame();
        if (frameToSerialize == null)
          return;

        _serializeOneFrameToDisk(frameToSerialize);
        frameToSerialize.Frame.Clear();

        _internal_memory_manager.SetFrameAsWritable(frameToSerialize);
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

        Task<CalibrationRecord> _calibration;
        object _lockObject = new object();
        
        MemoryManager<MemoryManagedFrame> _memoryManager = new MemoryManager<MemoryManagedFrame>();
        SerializationThreadPool<MemoryManagedFrame> _serializationThreadPool;
        private const int SERIALIZATION_THREAD_COUNT = 8;

        int _frameCount = 0;
        bool _captureStopRequested = false;

        FrameReader _reader;
        FrameSerializer _serializer = new FrameSerializer();

        /// <summary>
        /// Timer that fires the updateGUI event every GUI_UPDATE_RATE_IN_MS 
        /// </summary>
        Thread _guiTimer;
        private const int GUI_UPDATE_RATE_IN_MS = 1000;

        /// <summary>
        /// List of Timestamps recording when the Frames came in from the kinect
        /// </summary>
        List<DateTime> _frameTimes;
        /// <summary>
        /// List of Timestamp deltas in milliseconds
        /// </summary>
        List<double> _frameTimeDeltas;



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

        /// <summary>
        /// Is fired every GUI_UPDATE_RATE_IN_MS to update the GUI´s Min- and AverageFPS labels
        /// </summary>
        public event EventHandler<GUIUpdateEventArgs> updateGUI;


        /// <summary>
        /// Messages of the current state of the capture
        /// </summary>
        private string _current_state;


        

        public SessionManager(TSession session)
        {
            _session = session;

            // NOTE: The memory manager and the SerializationThreadPool can´t be initialised here or it will use the default maxframecount of 50

            my_times = new List<DateTime>();
            my_times_after = new List<TimeSpan>();
            _timestampCollection = new List<double[]>();

            _frameTimes = new List<DateTime>();
            _frameTimeDeltas = new List<double>();

            _guiTimer = new Thread(GuiInformationUpdate);
            _guiTimer.Start();

            _serializationThreadPool = new SerializationThreadPool<MemoryManagedFrame>(
              SERIALIZATION_THREAD_COUNT,
              new SerializationTask(_memoryManager, SaveOneFrameToDisk));
            _serializationThreadPool.StartPool();

            
        }

        void Dispose()
        {
          _guiTimer.Abort();
        }

        /// <summary>
        /// Timer callback that computes the min and average FPS in the past GUI_UPDATE_RATE_IN_MS - window.
        /// </summary>
        private void GuiInformationUpdate()
        {
          while (true)
          {
            if (updateGUI != null)
            {
              double maxTimeDelta = 0;
              double minFPS = 0;
              double averageFPS = 0;
              double precentage_buffer = 100 * ((double)_memoryManager.NbBuzyBuffers() / _memoryManager.Capacity);
              lock(this._lockObject)
              {
                if (_frameTimeDeltas.Count > 0)
                {
                  maxTimeDelta = _frameTimeDeltas.Max();
                  minFPS = (1000.0 / maxTimeDelta);
                  averageFPS = (1000.0 / _frameTimeDeltas.Average());

                  _frameTimeDeltas.Clear();
                  _frameTimes.Clear();
                }
              }

              GUIUpdateEventArgs args = new GUIUpdateEventArgs(minFPS, maxTimeDelta, averageFPS, precentage_buffer, _current_state == null ? "Ready": (string)_current_state.Clone());
              updateGUI(this, args);


            }
            Thread.Sleep(GUI_UPDATE_RATE_IN_MS);
          }
        }

        /// <summary>
        /// Signal received in order to change the size of the buffer
        /// </summary>
        /// <param name="size"></param>
        public void SetBufferSize(long size)
        {
          _memoryManager.Capacity = size;
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
        }

        /// <summary>
        /// Move to the next shot which needs to be completed. When a specific
        /// shot is provided, that logic is used instead.
        /// </summary>
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
                _nextShot = _session.Shots.Find(x => !x.Completed);
            }

            // NOTE: We need a new MemoryManager and a new Threadpool every time a new Shot comes 
            //       in, because the user might have changed the amount of buffers and because 
            //       joined Threads cannot be reused
            if (_nextShot != null)
            {
                _memoryManager.Capacity = _nextShot.ShotDefinition.MemoryFrameCount;
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

            if (ShotBeginning != null)
                ShotBeginning(this, new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot));


            Thread.Sleep(300);
            _capturingShot = _nextShot;
            _capturingShot.StartTime = DateTime.Now;
        }

        public virtual bool ValidateShot(out string message)
        {
            message = null;
            return true;
        }


        // TODO: Remove these once all timing stuff is done
        List<DateTime> my_times;
        List<TimeSpan> my_times_after;
        List<double[]> _timestampCollection;

        public virtual void FrameArrived(LiveFrame frame)
        {
          // stop requested, we do not enter here anymore
          if (_captureStopRequested)
            return;


            DateTime now = DateTime.Now;
            my_times.Add(now);

            if (_frameTimes.Count > 0)
            {
                TimeSpan span = now - _frameTimes.Last<DateTime>();
                double deltaInMS = span.TotalMilliseconds;
                _frameTimeDeltas.Add(deltaInMS);
            }
            _frameTimes.Add(now);

            bool bufferAvailable = true;
            // When the first frame arrive, start the calibration operation. This won't work
            // if we try to do it right after calling _sensor.Open().
            if (_calibration == null)
            {
                _calibration = Calibrator.CalibrateAsync(_reader.Sensor);
            }

            if (_capturingShot == null)
            {
                // We don´t currently capture, so we can return immediately
                // Console.WriteLine("_capturingShot == null");
                return;
            }

            int nFramesToCapture = _capturingShot.ShotDefinition.FramesToCapture;
            MemoryManagedFrame frameToWriteTo = _memoryManager.GetWritableBuffer();

            if (frameToWriteTo == null)
            {
                bufferAvailable = false;
                _current_state = "stopping (buffer full)";
                Console.WriteLine("There is no memory to write the frame data to. The capture ends now.");
            } 
            else
            {
                _current_state = "capturing";
                // We successfully received a buffer, now we can fill the frame data into the buffer
                bufferAvailable = true;
                frameToWriteTo.Frame.Clear();

                DateTime start = DateTime.Now;
                double[] timestamps = new double[6];
                if (_capturingShot.ShotDefinition.SerializationFlags.SerializeColor)
                {
                  frameToWriteTo.Frame.UpdateColor(frame, _serializer);
                }
                timestamps[0] = (DateTime.Now - start).Milliseconds;

                start = DateTime.Now;
                if (_capturingShot.ShotDefinition.SerializationFlags.SerializeDepth)
                {
                  frameToWriteTo.Frame.UpdateDepth(frame, _serializer);
                }
                timestamps[1] = (DateTime.Now - start).Milliseconds;

                start = DateTime.Now;
                if (_capturingShot.ShotDefinition.SerializationFlags.SerializeDepthMapping)
                {
                  frameToWriteTo.Frame.UpdateDepthMapping(frame, _serializer);
                }
                timestamps[2] = (DateTime.Now - start).Milliseconds;

                start = DateTime.Now;
                if (_capturingShot.ShotDefinition.SerializationFlags.SerializeInfrared)
                {
                  frameToWriteTo.Frame.UpdateInfrared(frame, _serializer);
                }
                timestamps[3] = (DateTime.Now - start).Milliseconds;

                start = DateTime.Now;
                if (_capturingShot.ShotDefinition.SerializationFlags.SerializeSkeleton)
                {
                  frameToWriteTo.Frame.UpdateSkeleton(frame, _serializer);
                }
                timestamps[4] = (DateTime.Now - start).Milliseconds;

                start = DateTime.Now;
                frameToWriteTo.Frame.UpdateBodyIndex(frame, _serializer);
                timestamps[5] = (DateTime.Now - start).Milliseconds;          

                lock (_lockObject)
                {
                    frameToWriteTo.Index = _frameCount++;
                    frameToWriteTo.ArrivedTime = DateTime.Now;
                }
                
                _timestampCollection.Add(timestamps);


                // The framedata was stored into the buffer, now we can save it to disk
                _writingShot = _capturingShot;
                _memoryManager.EnqueuSerializationTask(frameToWriteTo);
            }
            my_times_after.Add(DateTime.Now - my_times.Last<DateTime>());
/*
            Trace.WriteLine("Took " + my_times_after.Last().Milliseconds +"ms to write the frame data to the buffers");
            if (my_times_after.Count == 1)
            {
                Trace.WriteLine("Frame came in at " + my_times.Last().ToString("O"));
            }
            else if (my_times_after.Count == 2)
            {
                Trace.WriteLine("Frame came in at " + my_times.Last().ToString("O"));
            }
            */

            // Check if the user pressed the stop button or if the amount of frames to capture is reached
            bool continueCapture = _frameCount < nFramesToCapture || nFramesToCapture == 0;

            if (continueCapture && bufferAvailable)
            {
                // Continue the capture
                return;
            }

            _captureStopRequested = true;
            _current_state += " - finishing capture";

            new Thread(() =>
            {
              StopCapture();
            }).Start();
        }

        private void FinishShot()
        {
            _writingShot.Completed = true;
            // TODO: Is this metadatafile needed?

            /*
            string metadataPath = Path.Combine(_session.SessionPath, Session<TMetadata, TShot, TShotDefinition, TSavedItem>.METADATA_FILE);

            await Task.Run(() => JSONHelper.Instance.Serialize(_session.GetMetadata(), metadataPath));
            */

            _frameCount = 0;

            var ea2 = new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_writingShot);


            if (ShotSavedSuccess != null)
                ShotSavedSuccess(this, ea2);
            
            // We are finished writing the frames to disk
            _writingShot = null;

            PrepareForNextShot();

            if (_nextShot == null)
            {
                if (LastShotFinished != null)
                    LastShotFinished(this, ea2);
            }

            
            // TODO: Remove this when all the timing stuff has been sorted out
            
            Trace.WriteLine("first frame = " + my_times_after.First().Milliseconds);
            
            /*
            foreach (DateTime d in my_times)
            {
              Trace.WriteLine("frame arrived at time " + d.ToString("O"));
            }
            */

            foreach (TimeSpan d in my_times_after)
            {
              Trace.WriteLine("time span = " + d.Milliseconds);
            }

            foreach (double[] timestamps in _timestampCollection)
            {
                Trace.WriteLine("Color: " + timestamps[0] + "ms, Depth: " + timestamps[1] +
                                "ms, DepthMapping: " + timestamps[2] + "ms,  Infrared: " + timestamps[3] +
                                "ms, Skeleton: " + timestamps[4] + "ms, BodyIndex: " + timestamps[5] + "ms");
            }

            my_times.Clear();
            my_times_after.Clear();

            _captureStopRequested = false;
            _current_state = "Ready to capture";

        }



        public void StopCapture()
        {
            if (_capturingShot != null)
            {
                _captureStopRequested = true;
                _current_state = "Stop required / finishing capture";


                // (2) Move to the next shot
                string message;
                if (!ValidateShot(out message))
                {
                  _frameCount = 0;
                  var ea2 = new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot, message);

                  _capturingShot = null;

                  if (ShotCompletedError != null)
                    ShotCompletedError(this, ea2);

                  return;
                }

                var ea = new SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot, message);

                _capturingShot = null;

                // Blocks until everything is written to disk
                while (_memoryManager.NbFreeBuffers() < _memoryManager.Capacity)
                {
                  Thread.Sleep(1);
                }

                if (ShotCompletedSuccess != null)
                  ShotCompletedSuccess(this, ea);

                FinishShot();

                

            }
            else
            {
                Console.WriteLine("There is no Capture to stop");  
            }
        }


        #region Serialization

        protected virtual IEnumerable<IWriter> WritersForFrame(TShot shot, MemoryFrame frame, int frameIndex)
        {
            var writers = new List<IWriter>();

            // Add Writers depending on the ShotDefinition´s SerializationFlags
            SerializationFlags serializationFlags = shot.ShotDefinition.SerializationFlags;

            if (serializationFlags.SerializeColor && frame.Color != null) 
                writers.Add(new ColorFrameWriter(frame, _serializer));
            if (serializationFlags.SerializeDepthMapping && frame.MappedDepth != null) 
                writers.Add(new DepthMappingWriter(frame, _serializer));
            if (serializationFlags.SerializeDepth && frame.Depth != null) 
                writers.Add(new DepthFrameWriter(frame, _serializer));
            if (serializationFlags.SerializeInfrared && frame.Infrared != null) 
                writers.Add(new InfraredFrameWriter(frame, _serializer));
            if (serializationFlags.SerializeSkeleton && frame.Skeleton != null) 
                writers.Add(new SkeletonWriter(frame, _serializer));

            return writers;
        }

        protected virtual string GeneratePath(TShot shot, MemoryFrame frame, TimeSpan deltaTime, int frameIndex, IWriter writer)
        {
            string folderName = writer.Type.Name;

            string shotName = string.Format("s{0:D3}", _session.Shots.IndexOf(shot) + 1);

            // 0 -> Frame_001, 1 -> Frame_002, etc.
            string frameName = string.Format("frame_{0:D5}", frameIndex + 1);

            string timestamp_name = deltaTime.ToString(@"hh\.mm\.ss\.fff");
            string fileName = string.Format(
                "{0}{1}{2}__{3}{4}",
                shotName,
                shotName == null ? "" : "_",
                frameName,
                timestamp_name,
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


        public async void SaveOneFrameToDisk(MemoryManagedFrame memoryBlockToSerialize) 
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
            memoryBlockToSerialize.Frame.Clear();
        }

        protected virtual IEnumerable<Tuple<IWriter, TSavedItem>> PrepareWritersForOneFrame(TShot shot, MemoryManagedFrame frame)
        {
            var preparedWriters = new List<Tuple<IWriter, TSavedItem>>();
            MemoryFrame memoryFrame = frame.Frame;
            int index = frame.Index;
            TimeSpan deltaTime = frame.ArrivedTime - shot.StartTime;

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

            foreach (IWriter writer in WritersForFrame(shot, memoryFrame, index))
            {
                preparedWriters.Add(new Tuple<IWriter, TSavedItem>(
                    writer,
                    new TSavedItem()
                    {
                        Type = writer.Type,
                        Timestamp = writer.Timestamp,
                        Path = GeneratePath(shot, memoryFrame, deltaTime, index, writer),
                    }
                ));
            }

            return preparedWriters;
        }

        #endregion 
    
    }
}
