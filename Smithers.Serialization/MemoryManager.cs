using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Smithers.Serialization
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
    /// <typeparam name="MemoryManagedFrame">
    /// The type of frame to manage. The type has to be new()-able 
    /// since the MemoryManager is in charge of allocating the memory for it.
    /// </typeparam>
    public class MemoryManager<MemoryManagedFrame> : IDisposable
        where MemoryManagedFrame : new()
    {

        private MemoryManagedFrame[] _frames;
        Queue<MemoryManagedFrame> _writableMemory;
        Queue<MemoryManagedFrame> _serializeableFrames;
        object _lockObject;

        // TODO: different way of signalling that the serialization thread is finished
        MemoryManagedFrame _endSerializationFrame;
        public MemoryManagedFrame EndSerializationFrame { get { return _endSerializationFrame; } }


        /// <summary>
        /// Constructs a new MemoryManager with the given amount of buffers/frames to manage.
        /// </summary>
        /// <param name="nMemoryFrames"></param>
        public MemoryManager(int nMemoryFrames)
        {
            _frames = new MemoryManagedFrame[nMemoryFrames];
            _writableMemory = new Queue<MemoryManagedFrame>(nMemoryFrames);
            _serializeableFrames = new Queue<MemoryManagedFrame>(nMemoryFrames);

            for (int i = 0; i < _frames.Length; i += 1)
            {
                _frames[i] = new MemoryManagedFrame();
                _writableMemory.Enqueue(_frames[i]);
            }

            _endSerializationFrame = new MemoryManagedFrame();
            _lockObject = new object();
            
        }

        // TODO: Think about removing the notion of clearing a frame
        public void ClearFrames()
        {

        }

        /// <summary>
        /// Frees the allocated resources
        /// </summary>
        public void Dispose()
        {
            _writableMemory.Clear();
            _serializeableFrames.Clear();
            _lockObject = null;
            _frames = null;
        }

        /// <summary>
        /// How many buffers are currently free? 
        /// </summary>
        /// <returns></returns>
        public int nWritableBuffers()
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
        public int nSerializableBuffers()
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
        public MemoryManagedFrame GetWritableBuffer()
        {
            lock (_lockObject)
            {
                if (_writableMemory.Count == 0)
                {
                    return default(MemoryManagedFrame);
                }
                else
                {
                    return _writableMemory.Dequeue();
                }
            }
        }

        /// <summary>
        /// Queries and returns a frame that is in need of serialization.
        /// </summary>
        /// <returns>A buffer that should be serialized if one is available, null otherwise</returns>
        public MemoryManagedFrame GetSerializableFrame()
        {
            lock (_lockObject)
            {
                if (_serializeableFrames.Count == 0)
                {
                    return default(MemoryManagedFrame);
                }
                else
                {
                    return _serializeableFrames.Dequeue(); ;
                }
            }
        }

        /// <summary>
        /// Marks a buffer as being in need of serialization.
        /// </summary>
        /// <param name="frameToSerialize">The frame to be enqueued for serialization</param>
        public void EnqueuSerializationTask(MemoryManagedFrame frameToSerialize)
        {
            lock (_lockObject)
            {
                _serializeableFrames.Enqueue(frameToSerialize);
            }
        }

        /// <summary>
        /// Marks a buffer as being available for writing.
        /// 
        /// The Application should call this after a frame has been serialized, in order
        /// to be able to reuse the buffer.
        /// </summary>
        /// <param name="frame">The frame to be marked as free</param>
        public void SetFrameAsWritable(MemoryManagedFrame frame)
        {
            lock (_lockObject)
            {
                _writableMemory.Enqueue(frame);
            }
        }

        // TODO: Remove this
        public void StopSerialization()
        {
            lock (_lockObject)
            {
                _serializeableFrames.Enqueue(_endSerializationFrame);
            }
        }
    }
}
