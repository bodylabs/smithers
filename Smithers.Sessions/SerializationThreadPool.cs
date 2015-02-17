using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Smithers.Sessions
{
    /// <summary>
    /// This class handles a fixed amount of Threads that take care of serializing
    /// </summary>
    public class SerializationThreadPool
    {
        public delegate void SerializeFrameToDiskDelegate(MemoryManagedFrame frame);

        private object _lockObject;
        private bool _shouldStop;
        private MemoryManager<MemoryManagedFrame> _memoryManager;
        private int _nThreads;
        private Thread[] _threads;
        private SerializeFrameToDiskDelegate _serializeOneFrameToDisk;

        private int waited;
        private int serialized;

        public SerializationThreadPool(int nThreads, 
                                       MemoryManager<MemoryManagedFrame> memoryManager,
                                       SerializeFrameToDiskDelegate serializeOneFrameToDisk) 
        {
            _lockObject = new object();
            _shouldStop = false;
            _memoryManager = memoryManager;
            _serializeOneFrameToDisk = serializeOneFrameToDisk;
            _nThreads = nThreads;
            _threads = new Thread[nThreads];

            for (int i = 0; i < _nThreads; ++i)
            {
                _threads[i] = new Thread(SerializationLoop);
            }

            waited = 0;
            serialized = 0;
        }

        public void StartSerialization()
        {
            foreach (Thread thread in _threads)
            {
                thread.Start();
            }
        }

        /// <summary>
        /// Signals to the Threads, that no more Thread will arrive.
        /// </summary>
        public void EndSerialization()
        {
          lock (_lockObject)
          {
            if (!_shouldStop)
            {
              _shouldStop = true;
            }
          }
        }

        /// <summary>
        /// Blocks the current thread until all the Serialization-Threads are Joined.
        /// </summary>
        public void WaitForSerializationEnd()
        {
            foreach (Thread thread in _threads)
            {
                thread.Join();
            }
            _shouldStop = false;
            Console.WriteLine("Serialized: {0}", serialized);
            Console.WriteLine("Waited: {0}", waited);
            Console.WriteLine("Serialized/Waited: {0}", (double)serialized / (double)waited);
        }

        /// <summary>
        /// Checks if there is a frame that needs to be written to disk. 
        /// If there is, it calls the Serialization Callback that was passed to the objects 
        /// constructor and waits for the Callback´s execution. 
        /// If there is not, if the _shouldStop flag is set, the Thread returns, otherwise
        /// it waits.
        /// </summary>
        private void SerializationLoop()
        {
            while(true)
            {
                MemoryManagedFrame frameToSerialize = _memoryManager.GetSerializableFrame();

                if (frameToSerialize == null)
                {
                    if (_shouldStop)
                    {
                        return;
                    }
                    else
                    {
                        System.Threading.Interlocked.Increment(ref waited);
                        waited++;
                        Thread.Yield();
                    }
                }
                else
                {
                    _serializeOneFrameToDisk(frameToSerialize);
                    System.Threading.Interlocked.Increment(ref serialized);
                    _memoryManager.SetFrameAsWritable(frameToSerialize);

                    /*
                    IAsyncResult result =_serializeOneFrameToDisk.BeginInvoke(frameToSerialize, null, null);
                    _serializeOneFrameToDisk.EndInvoke(result);
                    _memoryManager.SetFrameAsWritable(frameToSerialize);
                    */
                }
            }
        }
        
    }
}
