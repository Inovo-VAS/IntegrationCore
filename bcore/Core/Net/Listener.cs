using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Lnksnk.Core.Net
{
    public class Listener
    {
        private static Dictionary<string, HttpListener> mappedHttpListeners = new Dictionary<string, HttpListener>();

        private static Dictionary<string, Listener> listeners = new Dictionary<string, Listener>();

        public static void RegisterListener(string path) {
            lock (listeners)
            {
                if (!listeners.ContainsKey(path))
                {
                    listeners.Add(path, new Listener());
                }
            }
        }

        public static bool IsRegistered(string path) {
            return path!=null && !path.Equals("") && listeners.ContainsKey(path);
        }

        public static Listener Registered(string path)
        {
            Listener lstnr = null;
            lock (listeners)
            {
                if (listeners.ContainsKey(path))
                {
                    lstnr = listeners[path];
                }
            }
            return lstnr;
        }
    }
}
