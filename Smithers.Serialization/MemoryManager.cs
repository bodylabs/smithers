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
        Queue<MemoryFrame> _writableMemory;
        Queue<MemoryFrame> _serializeableFrames;

        private MemoryFrame[] _frames;
        public int _framesConsideredForWritingToDisk = 0;

        public MemoryManager(int nMemoryFrames)
        {
            _frames = new MemoryFrame[nMemoryFrames];
            _writableMemory = new Queue<MemoryFrame>(nMemoryFrames);
            _serializeableFrames = new Queue<MemoryFrame>(nMemoryFrames);

            for (int i = 0; i < _frames.Length; i += 1)
            {
                _frames[i] = new MemoryFrame();
                _writableMemory.Enqueue(_frames[i]);
            }
        }

        public void ClearFrames()
        {
            foreach (MemoryFrame frame in _frames)
            {
                frame.Clear();
            }
            _framesConsideredForWritingToDisk = 0;
        }

        public void Dispose()
        {
            _writableMemory.Clear();
            _serializeableFrames.Clear();
            _lockObject = null;
            _framesConsideredForWritingToDisk = 0;
            foreach (MemoryFrame frame in _frames)
            {
                frame.Clear();
            }
            _frames = null;

        }

        public MemoryFrame GetWritableBuffer()
        {
            lock (_lockObject)
            {
                if (_writableMemory.Count == 0)
                {
                    return null;
                }
                else
                {
                    MemoryFrame memoryBlockToWriteTo = _writableMemory.Dequeue();
                    return memoryBlockToWriteTo;
                }
            }
        }

        public MemoryFrame GetSerializableFrame()
        {
            lock (_lockObject)
            {
                if (_serializeableFrames.Count == 0)
                {
                    return null;
                }
                else
                {
                    MemoryFrame frameToSerialize = _serializeableFrames.Dequeue();
                    return frameToSerialize;
                }
            }
        }

        public void OnFrameSerialized(MemoryFrame frame)
        {
            lock (_lockObject)
            {
                _writableMemory.Enqueue(frame);
            }
        }


        public void EnqueuSerializationTask(MemoryFrame frameToSerialize)
        {
            lock (_lockObject)
            {
                _serializeableFrames.Enqueue(frameToSerialize);
            }
        }
    }
}
