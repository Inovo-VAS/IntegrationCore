using Lnksnk.Core.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.Scheduling
{
    public class Schedules
    {
        private static Schedules schdls = new Schedules();

        public static Schedules SCHEDULES() {
            return schdls;
        }

        internal Dictionary<String, Schedulor> schdlors = new Dictionary<string, Schedulor>();

        public Schedulor Get(String schdlname) {
            return this[schdlname];
        }

        public Schedulor this[string key] {
            get
            {
                Schedulor schdl = null;
                lock (schdlors)
                {
                    if (schdlors.ContainsKey(key)) {
                        schdl = schdlors[key];
                    }
                }
                return schdl;
            }
        }

        public bool ScheduleExists(string schdlalias) {
            bool hasSchldr = false;
            lock (schdlors) {
                hasSchldr = schdlors.ContainsKey(schdlalias);
            }
            return hasSchldr;
        }

        public bool RegisterSchedule(string schdlalias, params Object[] schdlparams) {
            bool schdlregistered = false;
            lock (schdlors) {
                if (!schdlors.ContainsKey(schdlalias))
                {
                    Schedulor schdlr = new Schedulor(this,schdlalias);
                    schdlors.Add(schdlalias, schdlr);
                    schdlregistered = true;
                }
                else {
                    schdlregistered = false;
                } 
            }
            return schdlregistered;
        }

        public bool RemoveSchedule(string schdlalias) {
            bool schlremoved = true;
            Schedulor schdlr = null;
            lock (schdlors) {
                if (schdlors.ContainsKey(schdlalias))
                {
                    schdlr = schdlors[schdlalias];                    
                }
                else {
                    schlremoved = false;
                }
            }

            if (schdlr != null) {
                schdlr.Dispose();
                schdlr = null;
            }
            return schlremoved;
        }
    }
}
