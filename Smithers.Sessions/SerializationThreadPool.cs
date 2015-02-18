using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Smithers.Sessions
{
    /// <summary>
    /// This class manages a fixed amount of threads that all query a MemoryManager for serializable buffers.
    /// </summary>
    /// <typeparam name="TMemoryManagedFrame">The type of frame that is stored to disk</typeparam>
    public class SerializationThreadPool<TMemoryManagedFrame>
        where TMemoryManagedFrame : new()
    {
        /// <summary>
        /// The serialization callback definition
        /// </summary>
        public delegate void SerializeFrameToDiskDelegate(TMemoryManagedFrame frame);

        private object _lockObject;
        private bool _shouldStop;
        private MemoryManager<TMemoryManagedFrame> _memoryManager;
        private int _nThreads;
        private Thread[] _threads;

        // Serialization callback
        private SerializeFrameToDiskDelegate _serializeOneFrameToDisk;

        private int waited;
        private int serialized;

        /// <summary>
        /// Constructs a new SerializationThreadPool with the given arguments
        /// </summary>
        /// <param name="nThreads">The number of threads that will query the MemoryManager</param>
        /// <param name="memoryManager">The MemoryManager the threads will query for serializable frames</param>
        /// <param name="serializeOneFrameToDisk">
        ///     The serialization callback that is invoked on the serializable frames
        /// </param>
        public SerializationThreadPool(int nThreads, 
                                       MemoryManager<TMemoryManagedFrame> memoryManager,
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

        /// <summary>
        /// Starts all the threads
        /// </summary>
        public void StartSerialization()
        {
            foreach (Thread thread in _threads)
            {
                thread.Start();
            }
        }

        /// <summary>
        /// Signals to the threads, that no more frames will arrive.
        /// The threads continue serializing until the MemoryManager 
        /// does not deliver new frames to serialize.
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
                TMemoryManagedFrame frameToSerialize = _memoryManager.GetSerializableFrame();

                if (frameToSerialize == null)
                {
                    if (_shouldStop)
                    {
                        return;
                    }
                    else
                    {
                        System.Threading.Interlocked.Increment(ref waited);
                        Thread.Yield();
                    }
                }
                else
                {
                    _serializeOneFrameToDisk(frameToSerialize);
                    System.Threading.Interlocked.Increment(ref serialized);
                    _memoryManager.SetFrameAsWritable(frameToSerialize);
                }
            }
        }
        
    }
}
