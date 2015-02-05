using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Smithers.Serialization
{
    public class MemoryManager : IDisposable
    {
        object _lockObject = new object();

        public class MemoryManagedFrame
        {
            public MemoryFrame frame;
            public int index;
        }

        Queue<MemoryManagedFrame> _writableMemory;
        Queue<MemoryManagedFrame> _serializeableFrames;

        private MemoryManagedFrame[] _frames;

        // TODO: different way of signalling that the serialization thread is finished
        MemoryManagedFrame _endSerializationFrame;
        public MemoryManagedFrame EndSerializationFrame { get { return _endSerializationFrame; } }

        public MemoryManager(int nMemoryFrames)
        {
            _frames = new MemoryManagedFrame[nMemoryFrames];
            _writableMemory = new Queue<MemoryManagedFrame>(nMemoryFrames);
            _serializeableFrames = new Queue<MemoryManagedFrame>(nMemoryFrames);

            for (int i = 0; i < _frames.Length; i += 1)
            {
                _frames[i] = new MemoryManagedFrame();
                _frames[i].frame = new MemoryFrame();
                _frames[i].index = -1;
                
                _writableMemory.Enqueue(_frames[i]);
            }

            _endSerializationFrame = new MemoryManagedFrame();
            
        }

        public int nWritableBuffers()
        {
            lock (_lockObject)
            {
                return _writableMemory.Count;
            }
        }

        public void ClearFrames()
        {
            /*
            foreach (MemoryFrame frame in _frames)
            {
                frame.Clear();
            
             * */
        }

        public void Dispose()
        {
            _writableMemory.Clear();
            _serializeableFrames.Clear();
            _lockObject = null;
            _frames = null;

        }

        public MemoryManagedFrame GetWritableBuffer()
        {
            lock (_lockObject)
            {
                if (_writableMemory.Count == 0)
                {
                    return null;
                }
                else
                {
                    return _writableMemory.Dequeue();
                }
            }
        }

        public MemoryManagedFrame GetSerializableFrame()
        {
            lock (_lockObject)
            {
                if (_serializeableFrames.Count == 0)
                {
                    return null;
                }
                else
                {
                    return _serializeableFrames.Dequeue(); ;
                }
            }
        }

        public void SetFrameAsWritable(MemoryManagedFrame frame)
        {
            lock (_lockObject)
            {
                _writableMemory.Enqueue(frame);
            }
        }


        public void EnqueuSerializationTask(MemoryManagedFrame frameToSerialize)
        {
            lock (_lockObject)
            {
                _serializeableFrames.Enqueue(frameToSerialize);
            }
        }

        public void StopSerialization()
        {
            lock (_lockObject)
            {
                _serializeableFrames.Enqueue(_endSerializationFrame);
            }
        }
    }
}
