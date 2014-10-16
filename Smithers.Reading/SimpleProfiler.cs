using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Smithers.Reading
{

    /// <summary>
    /// Encapsulate Profiling Result
    /// </summary>
    public class ProfilingResult
    {
        public decimal? ElapsedMilliseconds { get; set; } 
    }

    /// <summary>
    /// Simple Profiler with support for getting frame rate
    /// Callback function should accept ProfilingResult as parameter
    /// 
    /// Usage: 
    ///     using(SimpleProfilerFactory.Profile("xxx", result => {
    ///           Console.WriteLine(result.Label + " runs in [" + result.ElapsedMilliseconds + " ms]");})
    ///     { 
    ///             ...
    ///             code snippet
    ///             ...
    ///     }
    /// </summary>
    public static class SimpleProfilerFactory
    {
        /// <summary>
        /// Return a SimpleProfiler object which collect profiling infomation during its lifecycle
        /// and pass the result to a custom prcoessing callback function 
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IDisposable Profile(Action<ProfilingResult> callback)
        {
            return new SimpleProfiler(callback);
        }

        public static IDisposable ProfileToConsole(string label)
        {
            return new SimpleProfiler(result => {
                Console.WriteLine(label + " runs in [" + result.ElapsedMilliseconds + " ms]");
            });
        }
    }

    /// <summary>
    /// Disposable SimpleProfiler to profile a segment of code
    /// </summary>
    public class SimpleProfiler : IDisposable
    {
        readonly Stopwatch _stopWatch = new Stopwatch();
        private Action<ProfilingResult> _callback;

        public SimpleProfiler(Action<ProfilingResult> callback)
        {
            _callback = callback;

            _stopWatch.Start();
        }

        /// <summary>
        /// Dispose the SimpleProfiler and invoke callback
        /// </summary>
        void IDisposable.Dispose()
        {
            _stopWatch.Stop();
            if (_callback != null)
            {
                _callback(new ProfilingResult()
                {
                    ElapsedMilliseconds = _stopWatch.ElapsedMilliseconds
                });
            }
        }
    }
}
