using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.IO
{
    class BaseWriter : TextWriter
    {
        private BaseRW baseRW;

        public BaseWriter(BaseRW baseRW)
        {
            this.baseRW = baseRW;
        }

        public override Encoding Encoding => this.baseRW.Encoding;

        public override string NewLine => this.baseRW.NewLine;

        public override async Task WriteAsync(char[] buffer, int index, int count)
        {
            await Task.Run(() => {
                this.writeBuffer(buffer, index, count);
            });
        }

        private void writeBuffer(char[] buffer, int index, int count)
        {
            this.baseRW.WriteBuffer(buffer, index, count);
        }

        public override void Write(string value)
        {
            if (value == null || value.Length == 0) return;
            this.Write(value.ToCharArray());
        }

        public override void Write(char[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return;
            this.Write(buffer, 0, buffer.Length);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null || count == 0 || (buffer.Length - index) <= 0) return;
            this.WriteAsync(buffer, index, count).Wait();
        }

        public async override Task WriteAsync(char value)
        {
            await Task.Run(() =>
            {
                this.Write(value);
            });
        }

        public async override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => {
                this.Write(buffer);
            }, cancellationToken);
        }

        public async override Task WriteAsync(StringBuilder value, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => {
                this.Write(value);
            }, cancellationToken);
        }

        public async override Task WriteAsync(string value)
        {
            await Task.Run(() => {
                this.Write(value);
            });
        }

        public override void WriteLine()
        {
            this.Write(this.NewLine);
        }

        public override void WriteLine(bool value)
        {
            Write(value);
            this.WriteLine();
        }

        public override void WriteLine(char value)
        {
            this.Write(value);
            this.WriteLine();
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            this.Write(buffer, index, count); this.WriteLine();
        }

        public override void WriteLine(char[] buffer)
        {
            this.Write(buffer); this.WriteLine();
        }

        public override void WriteLine(decimal value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(double value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(float value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(int value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(long value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(object value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            this.Write(buffer); this.WriteLine();
        }

        public override void WriteLine(string value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(StringBuilder value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(uint value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(ulong value)
        {
            this.Write(value); this.WriteLine();
        }

        public override void WriteLine(string format, object arg0)
        {
            this.WriteLine(format, arg0); this.WriteLine();
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            this.Write(format, arg0, arg1); this.WriteLine();
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            this.Write(format, arg0, arg1, arg2); this.WriteLine();
        }

        public override void WriteLine(string format, params object[] arg)
        {
            this.Write(format, arg); this.WriteLine();
        }

        public async override Task WriteLineAsync()
        {
            await this.WriteAsync(this.NewLine);
        }

        public async Task WriteLineAsync(params object[] arg)
        {
            await Task.Run(() =>
            {
                if (arg != null && arg.Length > 0)
                {
                    foreach (var a in arg)
                    {
                        if (a != null)
                        {
                            if (a is string)
                            {
                                this.Write((string)a);
                            }
                            else if (a is char)
                            {
                                this.Write((char)a);
                            }
                            else
                            {
                                this.Write(a.ToString());
                            }
                        }
                    }
                }
                this.WriteLine();
            });
        }

        public async override Task WriteLineAsync(char value)
        {
            await this.WriteLineAsync((object)value);
        }

        public async override Task WriteLineAsync(string value)
        {
            await this.WriteLineAsync((string)value);
        }

        public async override Task WriteLineAsync(char[] buffer, int index, int count)
        {
            await Task.Run(() => {
                this.Write(buffer, index, count); this.WriteLine();
            });
        }

        public async override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => { this.WriteLine(buffer); }, cancellationToken);
        }

        public async override Task WriteLineAsync(StringBuilder value, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => { this.WriteLine(value); }, cancellationToken);
        }

        public override void Close()
        {
            this.Flush();
        }

        public override void Write(bool value)
        {
            this.Write(value.ToString());
        }

        private char[] ch = new char[1];
        public override void Write(char value)
        {
            lock (this.ch)
            {
                this.ch[0] = value;
                base.Write(this.ch);
            }
        }

        public override void Write(decimal value)
        {
            this.Write(value.ToString());
        }

        public override void Write(double value)
        {
            this.Write(value.ToString());
        }

        public override void Write(float value)
        {
            this.Write(value.ToString());
        }

        public override void Write(int value)
        {
            this.Write(value.ToString());
        }

        public override void Write(long value)
        {
            this.Write(value.ToString());
        }

        public override void Write(uint value)
        {
            this.Write(value.ToString());
        }

        public override void Write(ulong value)
        {
            this.Write(value.ToString());
        }

        public override void Write(StringBuilder value)
        {
            if (value != null && value.Length > 0)
            {
                this.Write(value.ToString(0, value.Length));
            }
        }

        public override void Write(object value)
        {
            if (value == null) return;
            this.Write(value.ToString());
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            if (buffer != null && buffer.Length > 0)
            {
                this.Write(buffer.ToArray());
            }
        }

        public override void Write(string format, object arg0)
        {
            this.Write(format, arg: new object[] { arg0 });
        }

        public override void Write(string format, object arg0, object arg1)
        {
            this.Write(format, arg: new object[] { arg0, arg1 });
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            this.Write(format, arg: new object[] { arg0, arg1, arg2 });
        }

        public override void Write(string format, params object[] arg)
        {
            if (format != null && format.Length > 0 && arg != null && arg.Length > 0)
            {
                this.Write(string.Format(format, args: arg));
            }
        }

        public override void Flush()
        {
            this.FlushAsync().Wait();
        }

        public async override Task FlushAsync()
        {
            await Task.Run(() => {
                this.baseRW.FlushBuffer();
            });
        }
    }
}
