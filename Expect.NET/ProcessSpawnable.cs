﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Expect
{
    /// <summary>
    /// Represents spawnable shell command
    /// </summary>
    public class ProcessSpawnable : ISpawnable
    {
        private IProcess _process;
        private Task<string> _errorRead = null;
        private Task<string> _stdRead = null;

        /// <summary>
        /// Initializes new ProcessSpawnable instance to handle shell command process
        /// </summary>
        /// <param name="filename">filename to be run</param>
        /// <param name="arguments">arguments to be passed to process</param>
        public ProcessSpawnable(string filename, string arguments)
        {
            Process p = new Process();
            p.StartInfo.FileName = filename;
            p.StartInfo.Arguments = arguments;

            _process = new ProcessAdapter(p);
        }

        /// <summary>
        /// Initializes new ProcessSpawnable instance to handle shell command process
        /// </summary>
        /// <param name="filename">filename to be run</param>
        public ProcessSpawnable(string filename)
            : this(filename, "")
        { }

        /// <summary>
        /// Initializes new ProcessSpawnable instance to handle shell command process
        /// </summary>
        /// <param name="process">process to be run</param>
        public ProcessSpawnable(Process process)
        {
            _process = new ProcessAdapter(process);
        }

        /// <summary>
        /// Prepares and starts process
        /// </summary>
        public void Init()
        {
            if (_process.StartInfo.FileName == null || _process.StartInfo.FileName.Length == 0)
            {
                throw new ArgumentException("FileName cannot be empty string", "_process.StartInfo.FileName");
            }
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;

            _process.Start();


        }

        /// <summary>
        /// Writes to process StandardInput stream
        /// </summary>
        /// <param name="command">specify what should be written to process</param>
        public void Write(string command)
        {
            if (_errorRead == null || _errorRead.IsCanceled || _errorRead.IsCompleted || _errorRead.IsFaulted)
            {
                _process.StandardError.DiscardBufferedData();
            }
            if (_stdRead == null || _stdRead.IsCanceled || _stdRead.IsCompleted || _stdRead.IsFaulted)
            {
                _process.StandardOutput.DiscardBufferedData();
            }
            _process.StandardInput.Write(command);
        }

        /// <summary>
        /// Reads in asynchronous way from both standard input and standard error streams. 
        /// </summary>
        /// <returns>text read from streams</returns>
        public async Task<string> ReadAsync()
        {
            List<Task<string>> tasks = new List<Task<string>>();
            RecreateErrorReadTask();
            RecreateStdReadTask();
            tasks.Add(_errorRead);
            tasks.Add(_stdRead);

            var ret = await Task<string>.WhenAny<string>(tasks).ConfigureAwait(false);
            return await ret.ConfigureAwait(false);
        }

        private void RecreateErrorReadTask()
        {
            if (_errorRead == null || _errorRead.IsCanceled || _errorRead.IsCompleted || _errorRead.IsFaulted)
            {
                char[] tmp = new char[256];
                _errorRead = CreateStringAsync(tmp, _process.StandardError.ReadAsync(tmp, 0, 256));
            }
        }

        private void RecreateStdReadTask()
        {
            if (_stdRead == null || _stdRead.IsCanceled || _stdRead.IsCompleted || _stdRead.IsFaulted)
            {
                char[] tmp = new char[256];
                _stdRead = CreateStringAsync(tmp, _process.StandardOutput.ReadAsync(tmp, 0, 256));
            }
        }

        private async Task<string> CreateStringAsync(char[] c, Task<int> n)
        {
            return new string(c, 0, await n.ConfigureAwait(false));
        }


        /// <summary>
        /// Reads in synchronous way from both standard input and standard error streams. 
        /// </summary>
        /// <returns>text read from streams</returns>
        public string Read()
        {
            StreamReaderThread outputThread = new StreamReaderThread(_process.StandardOutput);
            StreamReaderThread errorThread = new StreamReaderThread(_process.StandardError);
            Thread t1 = new Thread(new ThreadStart(outputThread.Read));
            Thread t2 = new Thread(new ThreadStart(errorThread.Read));
            while (t1.IsAlive && t2.IsAlive)
            {
                Thread.Yield();
            }
            if (t1.IsAlive)
            {
                t1.Abort();
            }
            if (t2.IsAlive)
            {
                t2.Abort();
            }
            return errorThread.Output + outputThread.Output;
        }

        private class StreamReaderThread
        {
            internal string Output { get; private set; }
            private StreamReader stream;

            internal StreamReaderThread(StreamReader stream)
            {
                this.stream = stream;
            }

            internal void Read()
            {
                try
                {
                    int maxSize = 4096;
                    char[] tmp = new char[maxSize];
                    int n = stream.Read(tmp, 0, maxSize);
                    Output = new string(tmp, 0, n);
                }
                catch (ThreadAbortException)
                {
                    Output = "";
                }
            }
        } 
    
    }


}