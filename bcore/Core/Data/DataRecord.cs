using System;
using System.Collections.Generic;
using System.Text;

namespace Lnksnk.Core.Data
{
    public class DataRecord
    {
        private DataReader dataReader = null;

        public DataRecord(DataReader dataReader)
        {
            this.dataReader = dataReader;
        }

        public Object this[string key]
        {
            get
            {
                return this.dataReader.GetData(key);
            }
        }

        public Object FieldValue(string key) {
            return this[key];
        }

        public String FieldName(int index)
        {
            return this.dataReader.Columns!=null&& index>=0 && index < this.dataReader.Columns.Length? this.dataReader.Columns[index]:"";
        }

        public Object[] Data => this.dataReader.Data;

        public string[] Colums => this.dataReader.Columns;

        private bool last = false;
        public bool Last()
        {
            return this.last;
        }

        public bool First()
        {
            return this.first;
        }

        private bool started = true;
        private bool first = false;

        internal bool PrepNextRecord()
        {
            if (this.dataReader.NextRec())
            {
                if (this.started)
                {
                    this.first = true;
                    this.started = false;
                }
                else
                {
                    this.first = false;
                }
                return true;
            }
            else
            {
                this.last = true;
            }
            return false;

        }
    }
}
