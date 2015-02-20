using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpioTech.netNUT.upsmon.Shared
{
    /// <summary>
    /// Special Exception class to show an exception that is Fatal to the running of upsmon
    /// </summary>
    public class UPSMonFatalException : Exception
    {
        /// <summary>
        /// Create a new Fatal Exception with the given message
        /// </summary>
        /// <param name="message">What went wrong?!?</param>
        public UPSMonFatalException(string message)
            : base(message)
        { }
    }
}
