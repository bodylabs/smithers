﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Smithers.Serialization;

namespace Smithers.Sessions
{
    /// <summary>
    /// The MemoryManager class is responsible for providing the Application with a free buffer
    /// every time a frame arrives. 
    /// When a frame has been written to, it can be enqueued for serialization.
    /// After the frame has been serialized, it has to be set free for writing again.
    /// </summary>
    /// 
    /// <remarks>
    /// MemoryManager has to ensure that multiple threads can access the writable and serializable
    /// in a thread safe manner.
    /// </remarks>
    /// 
    /// <typeparam name="TMemoryManagedFrame">
    /// The type of frame to manage. The type has to be new()-able 
    /// since the MemoryManager is in charge of allocating the memory for it.
    /// </typeparam>
    public class MemoryManager<TMemoryManagedFrame> : IDisposable
        where TMemoryManagedFrame : new()
    {
        private Stack<TMemoryManagedFrame> _writableMemory;
        private Stack<TMemoryManagedFrame> _serializeableFrames;
        
        private Int64 _capacity;
        public Int64 Capacity 
        {
          get 
          {
            return _capacity;
          }
          set
          {
            lock (_lockObject)
            {
              if (_serializeableFrames.Count > 0 || _writableMemory.Count != _capacity)
                throw new InvalidOperationException("Cannot change the size of the container while all write buffers are not flushed out");
              if (_writableMemory.Count > value)
              {
                while(_writableMemory.Count != value)
                {
                  _writableMemory.Pop();
                }

              }
              else if (_writableMemory.Count < value)
              {
                while (_writableMemory.Count != value)
                {
                  _writableMemory.Push(new TMemoryManagedFrame());
                }
              }
              _capacity = value;
            }
          }
        }

        object _lockObject = new object();

        /// <summary>
        /// Constructs a new MemoryManager with the given amount of buffers/frames to manage.
        /// </summary>
        /// <param name="nMemoryFrames"></param>
        public MemoryManager(int nMemoryFrames = 200)
        {
            _writableMemory = new Stack<TMemoryManagedFrame>(nMemoryFrames);
            _serializeableFrames = new Stack<TMemoryManagedFrame>(nMemoryFrames);

            for (int i = 0; i < nMemoryFrames; i++)
            {
              _writableMemory.Push(new TMemoryManagedFrame());
            }

            _capacity = nMemoryFrames; // and not Capacity = nMemoryFrames
            
        }

        /// <summary>
        /// Frees the allocated resources
        /// </summary>
        public void Dispose()
        {
            _writableMemory.Clear();
            _serializeableFrames.Clear();
            _lockObject = null;
        }

        /// <summary>
        /// How many buffers are currently free? 
        /// </summary>
        /// <returns></returns>
        public int NbFreeBuffers()
        {
            lock (_lockObject)
            {
                return _writableMemory.Count;
            }
        }

        /// <summary>
        /// How many buffers are currently enqueued for serialization?
        /// </summary>
        /// <returns></returns>
        public int NbBuzyBuffers()
        {
            lock (_lockObject)
            {
                return _serializeableFrames.Count;
            }
        }

        /// <summary>
        /// Queries and returns a writable buffer if one is available. 
        /// </summary>
        /// <returns>A valid buffer if one is available, null otherwise</returns>
        public TMemoryManagedFrame GetWritableBuffer()
        {
            lock (_lockObject)
            {
                if (_writableMemory.Count == 0)
                {
                    return default(TMemoryManagedFrame);
                }
                else
                {
                    return _writableMemory.Pop();
                }
            }
        }

        /// <summary>
        /// Queries and returns a frame that is in need of serialization.
        /// </summary>
        /// <returns>A buffer that should be serialized if one is available, null otherwise</returns>
        public TMemoryManagedFrame GetSerializableFrame()
        {
            lock (_lockObject)
            {
                if (_serializeableFrames.Count == 0)
                {
                    return default(TMemoryManagedFrame);
                }
                else
                {
                  return _serializeableFrames.Pop(); ;
                }
            }
        }

        /// <summary>
        /// Marks a buffer as being in need of serialization.
        /// </summary>
        /// <param name="frameToSerialize">The frame to be enqueued for serialization</param>
        public void EnqueuSerializationTask(TMemoryManagedFrame frameToSerialize)
        {
            lock (_lockObject)
            {
                _serializeableFrames.Push(frameToSerialize);
            }
        }

        /// <summary>
        /// Marks a buffer as being available for writing.
        /// 
        /// The Application should call this after a frame has been serialized, in order
        /// to be able to reuse the buffer.
        /// </summary>
        /// <param name="frame">The frame to be marked as free</param>
        public void SetFrameAsWritable(TMemoryManagedFrame frame)
        {
            lock (_lockObject)
            {
                _writableMemory.Push(frame);
            }
        }
    }
}
