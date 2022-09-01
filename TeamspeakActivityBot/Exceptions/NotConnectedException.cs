using System;

namespace TeamspeakActivityBot.Exceptions
{
    public class NotConnectedException : Exception
    {
        public NotConnectedException() : base() { }
        public NotConnectedException(string message) : base(message) { }
        public NotConnectedException(string message, Exception inner) : base(message, inner) { }
    }
}
