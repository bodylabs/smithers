using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Smithers.Sessions
{

    public abstract class PoolTask
    {
      public abstract  bool IsTaskEmpty();
      public abstract  void PerformNextTask();
    }

    /// <summary>
    /// This class manages a fixed amount of threads that all query a MemoryManager for serializable buffers.
    /// </summary>
    /// <typeparam name="TMemoryManagedFrame">The type of frame that is stored to disk</typeparam>
    public class SerializationThreadPool<TMemoryManagedFrame> : IDisposable
        where TMemoryManagedFrame : new()
    {
        private object _lockObject = new object();
        private bool _shouldStop;
        private bool _stopped;
        private int _nThreads;
        private Thread[] _threads;
      
        private int waited;
        private int serialized;

        private PoolTask _task;

        /// <summary>
        /// Constructs a new SerializationThreadPool with the given arguments
        /// </summary>
        /// <param name="nThreads">The number of threads that will query the MemoryManager</param>
        /// <param name="memoryManager">The MemoryManager the threads will query for serializable frames</param>
        /// <param name="serializeOneFrameToDisk">
        ///     The serialization callback that is invoked on the serializable frames
        /// </param>
        public SerializationThreadPool(int nThreads, PoolTask task) 
        {
            _shouldStop = false;
            _stopped = true;
            _task = task;
            _nThreads = nThreads;
           
            waited = 0;
            serialized = 0;

            _CreatePool();
        }

        private void _CreatePool()
        {
          _threads = new Thread[_nThreads];
          for (int i = 0; i < _nThreads; ++i)
          {
            _threads[i] = new Thread(TaskPoolLoop);
            _threads[i].Priority = ThreadPriority.BelowNormal;
          }
        }

        public void Dispose()
        {
          _shouldStop = true;
          WaitStopPool();
        }


        /// <summary>
        /// Starts all the threads
        /// </summary>
        public void StartPool()
        {
            foreach (Thread thread in _threads)
            {
                thread.Start();
            }
            _stopped = false;
        }

        /// <summary>
        /// Signals to the threads, that no more frames will arrive.
        /// The threads continue serializing until the MemoryManager 
        /// does not deliver new frames to serialize.
        /// </summary>
        public void StopPool()
        {
          if (_stopped)
            return;
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
        public void WaitStopPool()
        {
            foreach (Thread thread in _threads)
            {
                thread.Join();
            }
            _stopped = true;
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
        private void TaskPoolLoop()
        {
            while(true)
            {
                if (_task.IsTaskEmpty())
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
                  _task.PerformNextTask();
                }
            }
        }
        
    }
}
