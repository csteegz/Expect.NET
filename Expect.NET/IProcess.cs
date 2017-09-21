using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ExpectNet
{
    public interface IProcess
    {
        ProcessStartInfo StartInfo { get; }
        StreamReader StandardOutput { get; }
        StreamReader StandardError { get; }
        StreamWriter StandardInput { get; }

        void Start();
    }
}
