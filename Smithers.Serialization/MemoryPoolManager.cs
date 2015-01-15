using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Smithers.Serialization
{
    public delegate void SerializeFrameToDiskCallback(int frameToSerialize);

    public class MemoryPoolManager
    {
        AutoResetEvent _threadSignaller = new AutoResetEvent(false);
        object _locker = new object();
        Queue<int> _writableMemory;
        Queue<int> _serializeableFrames;

        public MemoryFrame[] _frames;
        public int _framesConsideredForWritingToDisk = 0;

        Thread _serializationThread;

        SerializeFrameToDiskCallback _serializeFrameToDisk;

        public MemoryPoolManager(SerializeFrameToDiskCallback serializeFrameToDisk, int nMemoryFrames) 
        {
            _serializeFrameToDisk = serializeFrameToDisk;
            _frames = new MemoryFrame[nMemoryFrames];
            _writableMemory = new Queue<int>(nMemoryFrames);
            _serializeableFrames = new Queue<int>(nMemoryFrames);

            for (int i = 0; i < _frames.Length; i += 1)
            {
                _frames[i] = new MemoryFrame();
                _writableMemory.Enqueue(i);
            }

            _serializationThread = new Thread(SerializationLoop);
            _serializationThread.Start();
        }

        public void WriteToFreeBuffer(Reading.FrameData.LiveFrame frame, FrameSerializer serializer)
        {
            if (_writableMemory.Count == 0)
            {
                Console.WriteLine("There is no memory to write the frame data to, frame is dismissed!");
                return;
            } 
            else
            {
                int memoryBlockToWriteTo = _writableMemory.Dequeue();
                _frames[memoryBlockToWriteTo].Update(frame, serializer);
                EnqueuSerializationTask(memoryBlockToWriteTo);
            }
        }

        public void EnqueuSerializationTask(int memoryBlockIndex)
        {
            lock (_locker)
            {
                _serializeableFrames.Enqueue(memoryBlockIndex);
            }
            _threadSignaller.Set();
        }

        private void SerializationLoop()
        {
            while (true)
            {
                int memoryBlockToSerialize = -1;

                lock (_locker)
                {
                    if (_serializeableFrames.Count > 0)
                    {
                        memoryBlockToSerialize = _serializeableFrames.Dequeue();

                        // We use -1 as the signal for the thread to stop, so when all frames are 
                        // written to disk, -1 is enqueued and the thread stops here;
                        if (memoryBlockToSerialize == -1) return;
                    }
                }

                if (memoryBlockToSerialize > -1)
                {
                    _serializeFrameToDisk(memoryBlockToSerialize);

                    lock (_locker)
                    {
                        _writableMemory.Enqueue(memoryBlockToSerialize);
                    }
                }
                else
                {
                    // Wait/Block until threadSignaller.Set() is called again
                    _threadSignaller.WaitOne();
                }
            }
        }
    }
}
