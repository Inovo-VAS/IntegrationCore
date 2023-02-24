using Lnksnk.Core.IO;
using Bcoring.ES6.Expressions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lnksnk.Core.Net.Web
{
    
    public class ResourcePath : ResourceBase
    {
        internal ResourcePath(BaseHandler basehndlr) : base(basehndlr)
        {
            this.pathReaderRef = this;
        }

        private bool firstPath = false;
        public bool FirstPath => this.firstPath;

        private bool lastPath = false;
        public bool LastPath => this.lastPath;

        private string path="";
        public string Path => this.path;

        public string PathExt { get { return this.path.LastIndexOf(".") == -1 ? "" : this.path.Substring(this.path.LastIndexOf(".")); } }

        private string mimetype = "";
        public string Mimetype => this.mimetype;

        private bool textContent = false;
        public bool TextContent => this.textContent;

        private ResourcePath pathReaderRef = null;

        private Dictionary<string, object> pathsettings = new Dictionary<string, object>();

        public Dictionary<string, object> Settings => this.pathsettings;

        private Object resourceAsmObj = null;
        private Type resourceAsmType = null;
        private Assembly resourceAsm = null;
        
        internal virtual bool PrepNextPath(string path, string mimetype, bool firstPath, bool lastPath,bool textContent, Dictionary<string, object> pathsettings) {
            this.path = path;
            if (this.resourceAsmType != null)
            {
                if (this.RequestHandler.activeMap.ContainsKey(this.resourceAsmType.Name)) {
                    this.RequestHandler.activeMap[this.resourceAsmType.Name] = null;
                    this.RequestHandler.activeMap.Remove(this.resourceAsmType.Name);
                }
                this.resourceAsmType = null;
            }
            if (this.resourceAsmObj != null)
            {
                if (this.resourceAsmObj is IDisposable)
                {
                    ((IDisposable)this.resourceAsmObj).Dispose();
                }
                this.resourceAsmObj = null;
            }
            if (this.resourceAsm != null) { 
            }
            if (Mimetypes.TextExtension(this.path))
            {
                var asmtypepath = this.path.Substring(0, this.path.Length - (this.path.Length-this.path.LastIndexOf(".")));
                asmtypepath = asmtypepath.Replace("/", ".");
                if (asmtypepath.StartsWith("."))
                {
                    asmtypepath = asmtypepath.Substring(1);
                }
                if ((this.resourceAsm = this.RequestHandler.LoadAsm(asmtypepath)) != null)
                {
                    if ((this.resourceAsmType = this.RequestHandler.LoadAsmType(this.resourceAsm, asmtypepath)) != null)
                    {
                        if (typeof(Widget).IsAssignableFrom(this.resourceAsmType))
                        {
                            this.resourceAsmObj = Activator.CreateInstance(this.resourceAsmType, this.RequestHandler, this);
                        }
                        else {
                            this.resourceAsmObj = this.resourceAsmType.GetConstructor(new Type[] { }).Invoke(null);
                        }
                        if (this.RequestHandler.activeMap.ContainsKey(this.resourceAsmType.Name))
                        {
                            this.RequestHandler.activeMap[this.resourceAsmType.Name] = null;
                            this.RequestHandler.activeMap.Remove(this.resourceAsmType.Name);
                        }
                        if (this.resourceAsmObj != null)
                        {
                            this.RequestHandler.activeMap.Add(this.resourceAsmType.Name, this.resourceAsmObj);
                        }
                    }
                    else
                    {
                        this.resourceAsm = null;
                    }
                }
                else {
                    Type tp = (this.resourceAsm = Assembly.GetExecutingAssembly()).GetType(asmtypepath, false, true);
                    if (tp == null)
                    {
                        this.resourceAsm = null;
                    }
                    else {
                        this.resourceAsmType = tp;
                        this.resourceAsmObj = this.resourceAsmType.GetConstructor(null);
                    }
                }
            }
            if (this.resourceAsm == null && this.resourceAsmObj == null && this.resourceAsmType == null) {
                if ((this.resourceAsm = Assembly.GetExecutingAssembly()) != null)
                {
                    if ((this.resourceAsmType = this.RequestHandler.LoadAsmType(this.resourceAsm,typeof(Widget).FullName)) != null)
                    {
                        if (typeof(Widget).IsAssignableFrom(this.resourceAsmType))
                        {
                            this.resourceAsmObj = Activator.CreateInstance(this.resourceAsmType, this.RequestHandler, this);
                            if (this.RequestHandler.activeMap.ContainsKey(this.resourceAsmType.Name))
                            {
                                this.RequestHandler.activeMap[this.resourceAsmType.Name] = null;
                                this.RequestHandler.activeMap.Remove(this.resourceAsmType.Name);
                            }
                            this.RequestHandler.activeMap.Add(this.resourceAsmType.Name, this.resourceAsmObj);
                        }
                    }
                }
            }
            this.firstPath = firstPath;
            this.lastPath = lastPath;
            this.mimetype = mimetype;
            this.textContent = textContent;
            if (this.pathsettings.Count > 0)
            {
                this.pathsettings.Clear();
            }
            if (pathsettings != null && pathsettings.Count > 0)
            {
                foreach(var kv in pathsettings)
                {
                    this.pathsettings.Add(kv.Key, kv.Value);
                }
            }
            return true;
        }

        public override async Task<bool> WriteAsync(WriteAsync writeAsync, bool all = false, bool binread = false) {
            if (!binread) {
                binread = !this.textContent;
            }
            return await base.WriteAsync(writeAsync, all: all,binread:binread);
        }
        ~ResourcePath()
        {
            if (this.tmpstrm != null)
            {
                this.tmpstrm.Close();
                this.tmpstrm = null;
            }
            if (this.zip != null)
            {
                this.zip.Dispose();
                this.zip = null;
            }
            if (this.resourceAsmObj != null) {
                if (this.resourceAsmObj is IDisposable) {
                    ((IDisposable)this.resourceAsmObj).Dispose();
                }
                this.resourceAsmObj = null;
            }
            if (this.resourceAsmType != null) {
                this.resourceAsmType = null;
            }
            if (this.resourceAsm != null) {
                this.resourceAsm = null;
            }
        }

        public override void DoneReadingBinary(Exception e)
        {
            if (this.tmpstrm != null)
            {
                this.tmpstrm.Close();
                this.tmpstrm = null;
            }
            if (this.zip != null)
            {
                this.zip.Dispose();
                this.zip = null;
            }
        }

        public override async Task<ReadOnlySequence<Byte>> ReadByteAsync()
        {
            await Task.Run(() =>
            {
                    if (this.BinaryReader == null && !this.textContent)
                    {
                        var pths = (this.path.Equals("/") ? "" : this.path).Split("/");
                        var pthsroot = "";
                        var pthsrootzip = "";
                        foreach (var ps in pths)
                        {
                            if (pthsrootzip.Equals("") && !ps.Equals("") && (System.IO.File.Exists(this.RequestHandler.RootPath() + (pthsroot.EndsWith("/") ? pthsroot : (pthsroot + "/")) + ps + ".zip")))
                            {
                                pthsrootzip = (pthsroot.EndsWith("/") ? pthsroot : (pthsroot + "/")) + ps + ".zip";
                                pthsroot = "";
                            }
                            else
                            {
                                pthsroot += (pthsroot.EndsWith("/") ? "" : "/") + ps;
                            }
                        }

                        if (pthsrootzip.Equals(""))
                        {
                            if (System.IO.File.Exists(this.RequestHandler.RootPath() + pthsroot))
                            {
                                this.BinaryReader = new System.IO.BinaryReader(new System.IO.FileStream(this.RequestHandler.RootPath() + pthsroot, FileMode.Open, FileAccess.Read));
                            }
                        }
                        else if (!pthsrootzip.Equals("") && !pthsroot.Equals(""))
                        {
                            if (System.IO.File.Exists(this.RequestHandler.RootPath() + pthsrootzip))
                            {
                                this.zip = ZipFile.OpenRead(this.RequestHandler.RootPath() + pthsrootzip);

                                foreach (var zipe in zip.Entries)
                                {
                                    if (zipe.FullName.Equals(pthsroot.Substring(1)))
                                    {
                                        this.BinaryReader = new System.IO.BinaryReader(this.tmpstrm = zipe.Open());
                                        break;
                                    }
                                }
                                if (this.BinaryReader == null)
                                {
                                    this.zip.Dispose();
                                    this.zip = null;
                                }
                            }
                        }
                    }
            });
            return await base.ReadByteAsync();
        }

        private ZipArchive zip = null;
        private Stream tmpstrm = null;

        public override void DoneReadingStream(Exception e)
        {
            if (this.tmpstrm != null)
            {
                this.tmpstrm.Close();
                this.tmpstrm = null;
            }
            if (this.zip != null)
            {
                this.zip.Dispose();
                this.zip = null;
            }
        }

        public override async Task<ReadOnlySequence<Char>> ReadCharAsync()
        {
            await Task.Run(() =>
            {
                if (this.StreamReader == null && this.textContent)
                {
                    var pths = (this.path.Equals("/") ? "" : this.path).Split("/");
                    var pthsroot = "";
                    var pthsrootzip = "";
                    foreach (var ps in pths)
                    {
                        if (pthsrootzip.Equals("") && !ps.Equals("") && (System.IO.File.Exists(this.RequestHandler.RootPath() + (pthsroot.EndsWith("/") ? pthsroot : (pthsroot + "/")) + ps + ".zip")))
                        {
                            pthsrootzip = (pthsroot.EndsWith("/") ? pthsroot : (pthsroot + "/")) + ps + ".zip";
                            pthsroot = "";
                        }
                        else
                        {
                            pthsroot += (pthsroot.EndsWith("/") ? "" : "/") + ps;
                        }
                    }

                    if (pthsrootzip.Equals(""))
                    {
                        if (Environment.Environment.ENV().ContainsTextReaderCall(pthsroot))
                        {
                            Func<TextReader> readercall = Environment.Environment.ENV().TextReaderCall(pthsroot);
                            this.AddNextReadingSource(readercall.Invoke());
                        }
                        else
                        {
                            if ((System.IO.File.Exists(this.RequestHandler.RootPath() + pthsroot)))
                            {
                                this.AddNextReadingSource(new System.IO.StreamReader(this.RequestHandler.RootPath() + pthsroot));
                            }
                        }
                    }
                    else if (!pthsrootzip.Equals("") && !pthsroot.Equals(""))
                    {
                        if (System.IO.File.Exists(this.RequestHandler.RootPath() + pthsrootzip))
                        {
                            this.zip = ZipFile.OpenRead(this.RequestHandler.RootPath() + pthsrootzip);
                            foreach (var zipe in zip.Entries)
                            {
                                if (zipe.FullName.Equals(pthsroot.Substring(1)))
                                {
                                    this.AddNextReadingSource(new System.IO.StreamReader(this.tmpstrm = zipe.Open(), Encoding.UTF8));
                                    break;
                                }
                            }
                            if (this.StreamReader == null)
                            {
                                this.zip.Dispose();
                                this.zip = null;
                            }
                        }
                    }
                    
                    if (this.StreamReader == null)
                    {
                        if (this.resourceAsmObj != null)
                        {
                            MethodInfo resObjExecMeth = null;
                            foreach (var resMeth in this.resourceAsmType.GetMethods())
                            {
                                if (resObjExecMeth == null)
                                {
                                    if (resMeth.Name.Equals("Execute" + this.resourceAsmType.Name))
                                    {
                                        resObjExecMeth = resMeth;
                                        break;
                                    }
                                }
                            }
                            if (resObjExecMeth != null)
                            {
                                //Task.Run(() =>
                                //{
                                    resObjExecMeth.Invoke(this.resourceAsmObj, new object[] { this });
                                //});
                            }
                        }
                    }
                    this.DoneAddingInitialResourcePath(this.StreamReader != null);
                }
            });

            return await base.ReadCharAsync();
        }

        public override void Print(params object[] ss)
        {
            if (this.resourceAsmObj != null)
            {
                if (this.StreamReader == null)
                {
                    this.AddNextReadingSource();
                }
            }
            if (ss != null && ss.Length > 0)
            { 

                base.Print(ss:ss);
            }
        }

        public override void Println(params object[] ss)
        {
            if (this.resourceAsmObj != null)
            {
                if (this.StreamReader == null)
                {
                    this.AddNextReadingSource();
                }
            }
            base.Println(ss:ss);
        }

        public virtual void DoneAddingInitialResourcePath(bool initialResourceAdded){}
    }
}
