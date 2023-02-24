using Bcoring.ES6.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bcoring.ES6.Core;
using Bcoring.ES6;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Lnksnk.Core.IO
{
    public interface ActiveSourceFinder {

        public Type FindActiveSourceType(string sourcePath, params object[] args);
        public Task<System.IO.TextReader> FindActiveSourceAsync(string sourcePath,params object[]args);
        public Object InvokeActiveSourceType(Type sourceType, params object[] args);
    }

    public delegate void Interrupting(params object[] args);

    public delegate JSValue Requiring(string path, params object[] args);


    public delegate Task<int> ReadingNextChars(char[] chars, int index, int len,bool dispose=false);

    public class ActiveReader : System.IO.TextReader , IModuleResolver
    {
        private ActiveReader prntAtvRdr = null;

        private ActiveSourceFinder sourceFinder = null;
        public ActiveSourceFinder SourceFinder { 
            get => this.prntAtvRdr==null?this.sourceFinder:this.prntAtvRdr.SourceFinder; 
            set {
                if (this.prntAtvRdr != null) return;
                if (sourceFinder != value) { this.sourceFinder = value; }
            }
        }

        private List<PassiveRW> passiveRWs = new List<PassiveRW>();
        private List<ReadingNextChars> readingNextChars = new List<ReadingNextChars>();
        private bool disposeReader = false;

        private static readonly char[][] atvchars = new char[][] { "<@".ToCharArray(), "@>".ToCharArray() };
        private static readonly char[][] psvchars = new char[][] { "<".ToCharArray(), ">".ToCharArray() };

        private int[] atvcharsi = new int[] { 0, 0 };
        private char prvatvc = (char)0;
        private char[] atvchrs = new char[81920];
        private int atvbufi = 0;
        private int avtbufl = 0;
        private bool foundCode = false;
        private bool foundCdeTxt = false;
        private char cdeTxtPar = (char)0;
        private bool hasCode = false;
        private List<char[]> code = null;
        private char[] cdechrs = null;
        private int cdechrsi = 0;
        private int cdechrsl = 0;

        private Dictionary<long, List<char[]>> cntntBufferMap = null;
        private List<char[]> cntntBuffer = null;
        private char[] cntntchrs = null;
        private int cntntchrsi = 0;
        private int cntntchrsl = 0;


        private BlockingRW remainingContentRW = null;
        
        private Dictionary<String, Object> activeMap = null;

        private Interrupting interruptAction = null;

        internal ActiveReader(bool disposeReader = false, Dictionary<String, Object> activeMap = null, Interrupting interruptAction = null) : this(null,disposeReader,activeMap,interruptAction)
        {
        }

        internal ActiveReader(ActiveReader prntAtvRdr = null, bool disposeReader = false):this(prntAtvRdr,disposeReader,prntAtvRdr==null?null:prntAtvRdr.activeMap,prntAtvRdr==null?null:prntAtvRdr.interruptAction) { 
        }

        internal ActiveReader(ActiveReader prntAtvRdr = null, bool disposeReader = false, Dictionary<String, Object> activeMap = null, Interrupting interruptAction = null)
        {
            this.disposeReader = disposeReader;
            this.activeMap = activeMap;
            this.interruptAction = interruptAction;
            this.prntAtvRdr = prntAtvRdr;
            this.remainingContentRW = new BlockingRW(new BlockingCollection<char[]>(), true);
        }

        public void InsertReadingSource(int index, params object[] readingSource)
        {
            if (readingSource != null && readingSource.Length > 0)
            {
                if (index == 0 || index > 0 && index < this.readingNextChars.Count)
                {
                    ReadingNextChars rdnxtchrs = null;
                    foreach (var rdngSrc in readingSource)
                    {
                        if (rdngSrc is System.IO.StreamReader)
                        {
                            rdnxtchrs = new ReadingNextChars(async (char[] buffer, int index, int len, bool dispose) =>
                            {
                                var streamReader = (System.IO.StreamReader)rdngSrc;
                                var bufmxl = buffer == null ? 0 : (index + (buffer.Length - index) >= len ? len : ((buffer.Length - index)));
                                var bufrl = 0;
                                if (bufmxl > 0 && !streamReader.EndOfStream)
                                {
                                    if (!dispose)
                                    {
                                        bufrl = await streamReader.ReadAsync(buffer, index, bufmxl);
                                    }
                                }
                                if (dispose)
                                {
                                    streamReader.Dispose();
                                }
                                return bufrl;
                            });
                        }
                        else if (rdngSrc is System.IO.TextReader)
                        {
                            rdnxtchrs = new ReadingNextChars(async (char[] buffer, int index, int len, bool dispose) =>
                            {
                                var streamReader = (System.IO.TextReader)rdngSrc;
                                var bufmxl = buffer == null ? 0 : (index + (buffer.Length - index) >= len ? len : ((buffer.Length - index)));
                                var bufrl = 0;
                                if (!dispose)
                                {
                                    bufrl = await streamReader.ReadAsync(buffer, index, bufmxl);
                                }
                                if (dispose)
                                {
                                    streamReader.Dispose();
                                }
                                return bufrl;
                            });
                        }
                        else if (rdngSrc is ReadingNextChars)
                        {
                            rdnxtchrs = (ReadingNextChars)rdngSrc;
                        }
                        if (rdnxtchrs != null)
                        {
                            this.readingNextChars.Insert(index++, rdnxtchrs);
                            rdnxtchrs = null;
                        }
                    }
                }
            }
        }

        public void AddReadingSource(params object[] readingSource)
        {
            this.InsertReadingSource(this.readingNextChars.Count, readingSource: readingSource);
        }

        private string Code()
        {
            var cde = "";
            this.Code(ref cde);
            return cde;
        }

        private void Code(ref string cde)
        {
            if (this.code != null && this.code.Count > 0)
            {
                foreach (var ce in this.code)
                {
                    foreach(var c in ce) {
                        cde += (c+"");
                    }
                }
            }
        }

        ~ActiveReader()
        {
            this.Dispose(false);
        }

        private char[] internchars = new char[8192];
        private int intrnchrsl = 0;
        private int intrnchrsi = 0;

        
        private void internalActiveRead(char[] chars, int index, int len, ref int actualLenRead) {
            var chrsl = chars == null ? 0 : index < chars.Length ? ((chars.Length - index) >= len ? len : (chars.Length - index)) + index : 0;
            actualLenRead = 0;
            while (index < chrsl)
            {
                if (this.intrnchrsl == 0 || this.intrnchrsl > 0 && intrnchrsi == intrnchrsl)
                {
                    if (this.intrnchrsi > 0)
                    {
                        this.intrnchrsi = 0;
                    }
                    if (!this.remainingContentRW.IsCompleted && this.remainingContentRW.Count == 0)
                    {
                        if ((this.intrnchrsl = this.readProcessedChars(this.internchars, 0, this.internchars.Length).Result) == 0)
                        {
                            if (this.TopReader==this)
                            {
                                this.EvalCode();
                            } else
                            {
                                this.DirectEval();
                            }
                            try
                            {
                                if (!this.remainingContentRW.IsCompleted)
                                {
                                    this.intrnchrsl = this.remainingContentRW.Reader.Read(this.internchars, 0, this.internchars.Length);
                                } else
                                {
                                    this.intrnchrsl = 0;
                                    break;
                                }
                            }
                            catch (Exception) { 
                                this.intrnchrsl = 0;
                                break;
                            }
                        }
                    }
                    else if (!this.remainingContentRW.IsCompleted&&this.remainingContentRW.Count > 0)
                    {
                        this.intrnchrsl = this.remainingContentRW.Reader.Read(this.internchars, 0, this.internchars.Length);
                    }
                    else if(this.remainingContentRW.IsCompleted) {
                        this.intrnchrsl = 0;
                        break;
                    }
                }

                while ((index < chrsl) && (this.intrnchrsi < this.intrnchrsl))
                {
                    if ((chrsl - index) >= (this.intrnchrsl - this.intrnchrsi)) {
                        System.Array.Copy(this.internchars, this.intrnchrsi, chars, index, (this.intrnchrsl - this.intrnchrsi));
                        actualLenRead += (this.intrnchrsl - this.intrnchrsi);
                        index += (this.intrnchrsl - this.intrnchrsi);
                        this.intrnchrsi += (this.intrnchrsl - this.intrnchrsi);
                    } else if ((chrsl - index) < (this.intrnchrsl - this.intrnchrsi))
                    {
                        System.Array.Copy(this.internchars, this.intrnchrsi, chars, index, (chrsl - index));
                        actualLenRead += (chrsl - index);
                        this.intrnchrsi += (chrsl - index);
                        index += (chrsl - index);
                    }
                }
            }
        }

        private bool interrupted = false;

        public static void PrepActiveArgs(out object[] oargs, params object[] args)
        {
            oargs = null;
            if (args != null && args.Length > 0)
            {
                oargs = new object[args.Length];
                var i = 0;
                foreach (var arg in args)
                {
                    if (arg == null)
                    {
                        oargs[i++] = null;
                    }
                    else
                    {
                        if (arg is Bcoring.ES6.Core.JSValue)
                        {
                            var jsval = ((Bcoring.ES6.Core.JSValue)arg);
                            if (jsval.IsNull)
                            {
                                oargs[i++] = null;
                            }
                            else if (jsval.Is(Bcoring.ES6.Core.JSValueType.Object))
                            {
                                if (jsval.IsIterable())
                                {
                                    var list = new List<object>();
                                    populateList(ref list, jsval);
                                    oargs[i++] = list;
                                }
                                else
                                {
                                   var obj=jsval.Value;
                                    if (obj is JSObject)
                                    {
                                        var dictionary = new Dictionary<string, object>();
                                        populateDictionary(ref dictionary,(JSObject)obj);
                                        oargs[i++] = dictionary;
                                    } else
                                    {
                                        oargs[i++]=obj;
                                    }
                                }
                            }
                            else if (jsval.Is(Bcoring.ES6.Core.JSValueType.String) || jsval.Is(Bcoring.ES6.Core.JSValueType.Boolean) || jsval.Is(Bcoring.ES6.Core.JSValueType.Integer) || jsval.Is(Bcoring.ES6.Core.JSValueType.Double) || jsval.Is(Bcoring.ES6.Core.JSValueType.Date))
                            {
                                oargs[i++] = jsval.Value;
                            }
                        }
                        else
                        {
                            oargs[i++] = arg;
                        }
                    }
                }
               
            }
        }        
        private void Interrupt(params object[] args) {
            if (this.interruptAction != null) {
                if (args != null && args.Length > 0)
                {
                    var oargs = new object[args.Length];
                    var i = 0;
                    foreach(var arg in args)
                    {
                        if (arg == null)
                        {
                            oargs[i++] = null;
                        }
                        else {
                            if (arg is Bcoring.ES6.Core.JSValue) {
                                var jsval = ((Bcoring.ES6.Core.JSValue)arg);
                                if (jsval.IsNull)
                                {
                                    oargs[i++] = null;
                                }
                                else if (jsval.Is(Bcoring.ES6.Core.JSValueType.Object))
                                {
                                    if (jsval.IsIterable())
                                    {
                                        var list = new List<object>();
                                        populateList(ref list, jsval);
                                        oargs[i++] = list;
                                    }
                                    else
                                    {
                                        var dictionary = new Dictionary<string, object>();
                                        populateDictionary(ref dictionary, jsval);
                                        oargs[i++] = dictionary;
                                    }
                                }
                                else if (jsval.Is(Bcoring.ES6.Core.JSValueType.String)|| jsval.Is(Bcoring.ES6.Core.JSValueType.Boolean)|| jsval.Is(Bcoring.ES6.Core.JSValueType.Integer)|| jsval.Is(Bcoring.ES6.Core.JSValueType.Double)|| jsval.Is(Bcoring.ES6.Core.JSValueType.Date))
                                {
                                    oargs[i++] = jsval.Value;
                                }
                            }
                            else {
                                oargs[i++] = arg;
                            }
                        }
                    }
                    this.interruptAction(args:oargs);
                    foreach (var oarg in oargs) {
                        cleanupOArg(oarg);
                    }
                    oargs = null;
                }
                else
                {
                    this.interruptAction();
                }
            }
        }

        private void cleanupOArg(object oarg) {
            if (oarg != null)
            {
                if (oarg.GetType() == typeof(Dictionary<string, object>))
                {
                    var dictionary = (Dictionary<string, object>)oarg;
                    while (dictionary.Count > 0)
                    {
                        var key = dictionary.Keys.First();
                        var val = dictionary[key];
                        cleanupOArg(val);
                        val = null;
                        dictionary.Remove(key);
                    }

                    dictionary.Clear();
                    dictionary = null;
                }
                else if (oarg.GetType() == typeof(List<object>))
                {
                    var list = (List<object>)oarg;
                    while (list.Count > 0) {
                        var val=list[0];
                        list.RemoveAt(0);
                        cleanupOArg(val);
                        val = null;
                    }
                    list.Clear();
                    list = null;
                }
                else { 
                }
            }
        }

        private static void populateDictionary(ref Dictionary<string, object> dictionary, Bcoring.ES6.Core.JSValue jsval) {
            foreach (var jskv in jsval.AsEnumerable<KeyValuePair<string, Bcoring.ES6.Core.JSValue>>())
            {
                string key = jskv.Key;
                Bcoring.ES6.Core.JSValue val = jskv.Value;
                if (val == null)
                {
                    dictionary.Add(key, null);
                }
                else
                {
                    if (val is Bcoring.ES6.Core.JSValue)
                    {
                        if (val.IsNull)
                        {
                            dictionary.Add(key, null);
                        }
                        else if (val.Is(Bcoring.ES6.Core.JSValueType.Object))
                        {
                            if (val.IsIterable())
                            {
                                var nextlist = new List<object>();
                                populateList(ref nextlist, val);
                                dictionary.Add(key, nextlist);
                            }
                            else
                            {
                                var obj = val.Value;
                                if (obj is JSObject)
                                {
                                    var nextDictionary = new Dictionary<String, Object>();
                                    populateDictionary(ref nextDictionary, (JSObject)obj);
                                    dictionary.Add(key, nextDictionary);
                                } else
                                {
                                    dictionary.Add(key, obj);
                                }
                            }
                        }
                        else if (val.Is(Bcoring.ES6.Core.JSValueType.String) || val.Is(Bcoring.ES6.Core.JSValueType.Boolean) || val.Is(Bcoring.ES6.Core.JSValueType.Integer) || val.Is(Bcoring.ES6.Core.JSValueType.Double) || val.Is(Bcoring.ES6.Core.JSValueType.Date))
                        {
                            dictionary.Add(key, val.Value);
                        }
                    }
                    else
                    {
                        dictionary.Add(key, val.Value);
                    }
                }
            }
        }

        private  static void populateList(ref List<object> list, Bcoring.ES6.Core.JSValue jsval)
        {
            var jskl = jsval.AsIterable().iterator();
            
            for(var jvivl = jskl.next(); !jvivl.done; jvivl = jskl.next())
            {
                Bcoring.ES6.Core.JSValue val = jvivl.value;
                if (val == null)
                {
                    list.Add(null);
                }
                else
                {
                    if (val is Bcoring.ES6.Core.JSValue)
                    {
                        if (val.IsNull)
                        {
                            list.Add(null);
                        }
                        else if (val.Is(Bcoring.ES6.Core.JSValueType.Object))
                        {
                            if (val.IsIterable())
                            {
                                var nextlist = new List<object>();
                                populateList(ref nextlist, val);
                                list.Add(nextlist);
                            }
                            else
                            {
                                var obj = val.Value;
                                if (obj is JSObject)
                                {
                                    var nextDictionary = new Dictionary<String, Object>();
                                    populateDictionary(ref nextDictionary,(JSObject)val);
                                    list.Add(nextDictionary);
                                } else
                                {
                                    list.Add(obj);
                                }
                            }
                        }
                        else if (val.Is(Bcoring.ES6.Core.JSValueType.String) || val.Is(Bcoring.ES6.Core.JSValueType.Boolean) || val.Is(Bcoring.ES6.Core.JSValueType.Integer) || val.Is(Bcoring.ES6.Core.JSValueType.Double) || val.Is(Bcoring.ES6.Core.JSValueType.Date))
                        {
                            list.Add(val.Value);
                        }
                    }
                    else
                    {
                        list.Add(val.Value);
                    }
                }
            }
        }

        private async Task EvalCode()
        {
            await Task.Run(() =>
            {
                this.DirectEval();
            });
        }

        private void DirectEval()
        {
            flushPassiveContent(this, ref this.prvatvc);
            if (this.cdechrsl > 0)
            {
                var tmpcdechars = new char[this.cdechrsl];
                System.Array.Copy(this.cdechrs, 0, tmpcdechars, 0, this.cdechrsl);
                (this.code == null ? (this.code = new List<char[]>()) : this.code).Add(tmpcdechars);
                this.cdechrs = null;
                this.cdechrsi = 0;
                this.cdechrsl = 0;
            }
            if (this.code != null && this.code.Count > 0)
            {
                if (this.TopReader == this)
                {
                    var cntxtglbl = new Bcoring.ES6.Core.GlobalContext();

                    cntxtglbl.ActivateInCurrentThread();

                    var cntxt = new Bcoring.ES6.Core.Context(cntxtglbl);
                    cntxtglbl.Deactivate();

                    try
                    {
                        if (this.activeMap != null)
                        {
                            foreach (var kv in this.activeMap)
                            {
                                cntxt.DefineVariable(kv.Key).Assign(JSValue.Marshal(kv.Value));
                            }
                        }

                        cntxt.DefineVariable("WritePsvContent").Assign(JSValue.Marshal((Action<long>)this.TopReader.WritePsvContent));
                        cntxt.DefineVariable("Print").Assign(JSValue.Marshal((Printing)this.TopReader.Print));
                        cntxt.DefineVariable("Println").Assign(JSValue.Marshal((Printing)this.TopReader.Println));
                        cntxt.DefineVariable("Interrupt").Assign(JSValue.Marshal((Interrupting)this.TopReader.Interrupt));
                        cntxt.DefineVariable("require").Assign(JSValue.Marshal((Requiring)new Requiring((path,args)=>{
                            JSValue result;
                            this.TopReader.Require(ref cntxt, path, out result, args: args);
                            return result;
                        })));
                        try
                        {
                            var mnmod = new Bcoring.ES6.Module("dummymod", "", cntxtglbl);
                            cntxt._module = mnmod;
                            mnmod.ModuleResolversChain.Add(this);
                            try
                            {
                                //mnmod.Script.Evaluate(cntxt);
                                //mnmod = null;
                                cntxt.Eval(this.Code());
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                            mnmod = null;

                        }
                        catch (Bcoring.ES6.Core.JSException jse)
                        {
                            if (!interrupted)
                            {
                                throw jse;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(this.Code());
                    }
                    cntxt = null;
                    cntxtglbl = null;
                }
            }
            this.remainingContentRW.Writer.Close();
        }

        private delegate void Printing(params object[] ss);

        private char[] chrstoprscs = new char[81920];
        private int chrstoprscsi = 0;
        private int chrstoprscsl = 0;

        private async Task<int> ReadNextUnprocessedChars(char[] buffer, int index, int len) {
            var bufmxl = buffer == null ? 0 : (index + (buffer.Length - index) >= len ? len : ((buffer.Length - index)));
            var bufrl = 0;
            if (bufmxl>0) {
                var nxtrdchrs = this.readingNextChars.Count > 0 ? this.readingNextChars[0] : null;
                if(nxtrdchrs!=null&&(bufrl=await nxtrdchrs(buffer, index, bufmxl)) == 0){
                    await nxtrdchrs(null, 0, 0, true);
                    this.readingNextChars.RemoveAt(this.readingNextChars.IndexOf(nxtrdchrs));
                }
            }
            return bufrl;
        }

        private async Task<int> readProcessedChars(char[] buffer, int index, int len)
        {
            var bufmxl = buffer == null ? 0 : (index + (buffer.Length - index) >= len ? len : ((buffer.Length - index)));
            var bufrl = 0;
            if (this.chrstoprscsi < this.chrstoprscsl)
            {
                foreach (var c in this.chrstoprscs.AsSpan(this.chrstoprscsi, this.chrstoprscsl- this.chrstoprscsi).ToArray())
                {
                    processChar(this, ref this.prvatvc, c, this.atvcharsi, ref bufrl, ref buffer, ref index, ref bufmxl);
                    this.chrstoprscsi++;
                    if (this.chrstoprscsi == this.chrstoprscsl)
                    {
                        this.chrstoprscsi = 0;
                        this.chrstoprscsl = 0;
                        if (bufrl > 0) {
                            return bufrl;
                        }
                        if (bufmxl > 0 && index == bufmxl)
                        {
                            return bufrl;
                        }
                        break;
                    }
                    else
                    {
                        if (bufmxl > 0 && index == bufmxl)
                        {
                            return bufrl;
                        }
                    }
                }
                if (bufrl > 0)
                {
                    return bufrl;
                }
            }

            while ((this.chrstoprscsl = await this.ReadNextUnprocessedChars(this.chrstoprscs, 0, this.chrstoprscs.Length) /*await this.streamReader.ReadAsync(this.chrstoprscs, 0, this.chrstoprscs.Length)*/) > 0)
            {
                this.chrstoprscsi = 0;
                while (this.chrstoprscsi < this.chrstoprscsl)
                {
                    foreach (var c in this.chrstoprscs.AsSpan(this.chrstoprscsi, this.chrstoprscsl - this.chrstoprscsi).ToArray())
                    {
                        if (c=='@')
                        {
                            processChar(this, ref this.prvatvc, c, this.atvcharsi, ref bufrl, ref buffer, ref index, ref bufmxl);
                        } else
                        {
                            processChar(this, ref this.prvatvc, c, this.atvcharsi, ref bufrl, ref buffer, ref index, ref bufmxl);
                        }
                        
                        this.prvatvc = c;
                        this.chrstoprscsi++;
                        if (this.chrstoprscsi == this.chrstoprscsl)
                        {
                            this.chrstoprscsi = 0;
                            this.chrstoprscsl = 0;
                            if (bufrl > 0)
                            {
                                return bufrl;
                            }
                        }
                        if (bufmxl > 0 && index == bufmxl)
                        {
                            return bufrl;
                        }
                    }
                    if (bufmxl > 0 && index == bufmxl)
                    {
                        return bufrl;
                    }
                }
            }
            return bufrl;
        }

        private static void processChar(ActiveReader strmrdr, ref char prvc, char c, int[] atvcharsi, ref int bufrl, ref char[] buffer, ref int bufi, ref int bufmxl)
        {
            if (strmrdr.unflushedPsvParsl > 0) {
                if (strmrdr.foundCode)
                {
                    foreach (var unpsvc in strmrdr.unflushedPsvPars)
                    {
                        parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                    }
                    strmrdr.unflushedPsvParsl = 0;
                    strmrdr.unflushedPsvParsi = 0;
                    strmrdr.unflushedPsvPars = null;
                }
                else {
                    foreach (var unpsvc in strmrdr.unflushedPsvPars.AsSpan(strmrdr.unflushedPsvParsi))
                    {
                        parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                        strmrdr.unflushedPsvParsi++;
                        if (bufi == bufmxl)
                        {
                            break;
                        }
                    }
                    if (strmrdr.unflushedPsvParsi == strmrdr.unflushedPsvParsl)
                    {
                        strmrdr.unflushedPsvParsi = 0;
                        strmrdr.unflushedPsvParsl = 0;
                        strmrdr.unflushedPsvPars = null;
                    }
                    if (bufi == bufmxl)
                    {
                        return;
                    }
                }
                return;
            }
            if (atvcharsi[1] == 0 && atvcharsi[0] < atvchars[0].Length)
            {
                if (strmrdr.foundCode) {
                    if (strmrdr.atvbufi < strmrdr.avtbufl)
                    {
                        foreach (var ac in strmrdr.atvchrs.AsSpan(strmrdr.atvbufi, (strmrdr.avtbufl - strmrdr.atvbufi)).ToArray())
                        {
                            parsePassiveChar(strmrdr, ref prvc, ac, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                            strmrdr.atvbufi++;
                            if (strmrdr.atvbufi == strmrdr.avtbufl)
                            {
                                strmrdr.atvbufi = 0;
                                strmrdr.avtbufl = 0;
                            }
                            if (strmrdr.unflushedPsvParsl > 0)
                            {
                                foreach (var unpsvc in strmrdr.unflushedPsvPars)
                                {
                                    parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                }
                                strmrdr.unflushedPsvParsl = 0;
                                strmrdr.unflushedPsvParsi = 0;
                                strmrdr.unflushedPsvPars = null;
                            }
                        }
                    }
                }
                else
                {
                    if (bufmxl > bufi && strmrdr.atvbufi < strmrdr.avtbufl)
                    {
                        foreach (var ac in strmrdr.atvchrs.AsSpan(strmrdr.atvbufi, (strmrdr.avtbufl-strmrdr.atvbufi)).ToArray())
                        {
                            parsePassiveChar(strmrdr, ref prvc, ac, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                            strmrdr.atvbufi++;                            
                            if (strmrdr.atvbufi == strmrdr.avtbufl)
                            {
                                strmrdr.atvbufi = 0;
                                strmrdr.avtbufl = 0;
                            }
                            if (bufi == bufmxl)
                            {
                                return;
                            }
                            if (strmrdr.unflushedPsvParsl > 0)
                            {
                                foreach (var unpsvc in strmrdr.unflushedPsvPars)
                                {
                                    parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                    strmrdr.unflushedPsvParsi++;
                                    if (bufi == bufmxl)
                                    {
                                        break;
                                    }
                                }
                                if (strmrdr.unflushedPsvParsi == strmrdr.unflushedPsvParsl) {
                                    strmrdr.unflushedPsvParsi = 0;
                                    strmrdr.unflushedPsvParsl = 0;
                                    strmrdr.unflushedPsvPars = null;
                                }
                                if (bufi == bufmxl)
                                {
                                    return;
                                }
                            }
                        }
                        return;
                    }
                }

                if (atvcharsi[0] > 0 && atvchars[0][atvcharsi[0] - 1] == prvc && atvchars[0][atvcharsi[0]] != c)
                {
                    System.Array.Copy(atvchars[0],0, strmrdr.atvchrs,0, atvcharsi[0]);
                    strmrdr.avtbufl = atvcharsi[0];
                    strmrdr.atvbufi = 0;
                    atvcharsi[0] = 0;

                    if (strmrdr.foundCode)
                    {
                        if (strmrdr.atvbufi < strmrdr.avtbufl)
                        {
                            foreach (var ac in strmrdr.atvchrs.AsSpan(strmrdr.atvbufi, (strmrdr.avtbufl - strmrdr.atvbufi)).ToArray())
                            {
                                parsePassiveChar(strmrdr, ref prvc, ac, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                strmrdr.atvbufi++;
                                if (strmrdr.atvbufi == strmrdr.avtbufl)
                                {
                                    strmrdr.atvbufi = 0;
                                    strmrdr.avtbufl = 0;
                                }
                                if (strmrdr.unflushedPsvParsl > 0)
                                {
                                    foreach (var unpsvc in strmrdr.unflushedPsvPars)
                                    {
                                        parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                    }
                                    strmrdr.unflushedPsvParsl = 0;
                                    strmrdr.unflushedPsvParsi = 0;
                                    strmrdr.unflushedPsvPars = null;
                                }
                            }
                        }
                    }
                    else {
                        if (bufmxl > bufi)
                        {
                            if (bufi < bufmxl && strmrdr.atvbufi < strmrdr.avtbufl)
                            {
                                foreach (var ac in strmrdr.atvchrs.AsSpan(strmrdr.atvbufi, (strmrdr.avtbufl - strmrdr.atvbufi)).ToArray())
                                {
                                    parsePassiveChar(strmrdr, ref prvc, ac, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                    strmrdr.atvbufi++;
                                    if (strmrdr.atvbufi == strmrdr.avtbufl)
                                    {
                                        strmrdr.atvbufi = 0;
                                        strmrdr.avtbufl = 0;
                                    }
                                    if (bufi == bufmxl)
                                    {
                                        break;
                                    }
                                    if (strmrdr.unflushedPsvParsl > 0)
                                    {
                                        foreach (var unpsvc in strmrdr.unflushedPsvPars)
                                        {
                                            parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                            strmrdr.unflushedPsvParsi++;
                                            if (bufi == bufmxl)
                                            {
                                                break;
                                            }
                                        }
                                        if (strmrdr.unflushedPsvParsi == strmrdr.unflushedPsvParsl)
                                        {
                                            strmrdr.unflushedPsvParsi = 0;
                                            strmrdr.unflushedPsvParsl = 0;
                                            strmrdr.unflushedPsvPars = null;
                                        }
                                        if (bufi == bufmxl)
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (atvchars[0][atvcharsi[0]] == c)
                {
                    atvcharsi[0]++;
                    if (atvcharsi[0] == atvchars[0].Length)
                    {
                        if (strmrdr.foundCode) {
                        } else {
                            if (bufi > 0) {
                                bufmxl = bufrl = bufi;
                            }
                        }
                        prvc = (char)0;
                    }
                    else
                    {
                        prvc = c;
                    }
                }
                else {
                    if (atvcharsi[0] > 0)
                    {
                        System.Array.Copy(atvchars[0],0, strmrdr.atvchrs,0, atvcharsi[0]);
                        strmrdr.avtbufl = atvcharsi[0];
                        atvcharsi[0] = 0;
                        strmrdr.atvbufi = 0;
                        if (strmrdr.foundCode)
                        {
                            if (strmrdr.atvbufi < strmrdr.avtbufl)
                            {
                                foreach (var ac in strmrdr.atvchrs.AsSpan(strmrdr.atvbufi, (strmrdr.avtbufl - strmrdr.atvbufi)).ToArray())
                                {
                                    parsePassiveChar(strmrdr, ref prvc, ac,ref bufrl,ref buffer,ref bufi,ref bufmxl);
                                    strmrdr.atvbufi++;
                                    if (strmrdr.atvbufi == strmrdr.avtbufl)
                                    {
                                        strmrdr.atvbufi = 0;
                                        strmrdr.avtbufl = 0;
                                    }
                                    if (strmrdr.unflushedPsvParsl > 0) {
                                        foreach (var unpsvc in strmrdr.unflushedPsvPars) {
                                            parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                        }
                                        strmrdr.unflushedPsvParsl = 0;
                                        strmrdr.unflushedPsvParsi = 0;
                                        strmrdr.unflushedPsvPars = null;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (bufmxl > bufi)
                            {
                                if (bufi < bufmxl && strmrdr.atvbufi < strmrdr.avtbufl)
                                {
                                    foreach (var ac in strmrdr.atvchrs.AsSpan(strmrdr.atvbufi, (strmrdr.avtbufl - strmrdr.atvbufi)).ToArray())
                                    {
                                        parsePassiveChar(strmrdr, ref prvc, ac, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                        strmrdr.atvbufi++;
                                        if (strmrdr.atvbufi == strmrdr.avtbufl)
                                        {
                                            strmrdr.atvbufi = 0;
                                            strmrdr.avtbufl = 0;
                                        }
                                        if (bufi == bufmxl)
                                        {
                                            break;
                                        }
                                        if (strmrdr.unflushedPsvParsl > 0)
                                        {
                                            foreach (var unpsvc in strmrdr.unflushedPsvPars)
                                            {
                                                parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                                strmrdr.unflushedPsvParsi++;
                                                if (bufi == bufmxl)
                                                {
                                                    break;
                                                }
                                            }
                                            if (strmrdr.unflushedPsvParsi == strmrdr.unflushedPsvParsl)
                                            {
                                                strmrdr.unflushedPsvParsi = 0;
                                                strmrdr.unflushedPsvParsl = 0;
                                                strmrdr.unflushedPsvPars = null;
                                            }
                                            if (bufi == bufmxl)
                                            {
                                                return;
                                            }
                                        }
                                    }
                                }
                                return;
                            }
                        }
                    }
                    prvc = c;
                    if (strmrdr.foundCode)
                    {
                        parsePassiveChar(strmrdr, ref prvc, c, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                        if (strmrdr.unflushedPsvParsl > 0)
                        {
                            foreach (var unpsvc in strmrdr.unflushedPsvPars)
                            {
                                parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                            }
                            strmrdr.unflushedPsvParsl = 0;
                            strmrdr.unflushedPsvParsi = 0;
                            strmrdr.unflushedPsvPars = null;
                        }
                    }
                    else
                    {
                        if (bufmxl > bufi)
                        {
                            parsePassiveChar(strmrdr, ref prvc, c, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                        }
                        if (strmrdr.unflushedPsvParsl > 0)
                        {
                            foreach (var unpsvc in strmrdr.unflushedPsvPars)
                            {
                                parsePassiveChar(strmrdr, ref prvc, unpsvc, ref bufrl, ref buffer, ref bufi, ref bufmxl);
                                strmrdr.unflushedPsvParsi++;
                                if (bufi == bufmxl)
                                {
                                    break;
                                }
                            }
                            if (strmrdr.unflushedPsvParsi == strmrdr.unflushedPsvParsl)
                            {
                                strmrdr.unflushedPsvParsi = 0;
                                strmrdr.unflushedPsvParsl = 0;
                                strmrdr.unflushedPsvPars = null;
                            }
                            if (bufi == bufmxl)
                            {
                                return;
                            }
                        }
                    }
                }
            }
            else if (atvcharsi[0] == atvchars[0].Length && atvcharsi[1] < atvchars[1].Length) {
                if (strmrdr.foundCdeTxt)
                {
                    parseActiveChar(strmrdr, ref prvc, c);
                }
                else
                {
                    if (atvchars[1][atvcharsi[1]] == c)
                    {
                        atvcharsi[1]++;
                        if (atvcharsi[1] == atvchars[1].Length)
                        {
                            atvcharsi[0] = 0;
                            atvcharsi[1] = 0;
                            prvc = (char)0;
                            if (strmrdr.hasCode)
                            {
                                strmrdr.hasCode = false;
                            }
                        }
                    }
                    else
                    {
                        if (atvcharsi[1] > 0)
                        {
                            strmrdr.avtbufl = atvcharsi[1];
                            atvcharsi[1] = 0;
                            foreach (var ac in strmrdr.atvchrs.AsSpan(strmrdr.atvbufi, (strmrdr.avtbufl - strmrdr.atvbufi)).ToArray())
                            {
                                parseActiveChar(strmrdr, ref prvc, ac);
                                strmrdr.atvbufi++;
                                if (strmrdr.atvbufi == strmrdr.avtbufl)
                                {
                                    strmrdr.atvbufi = 0;
                                    strmrdr.avtbufl = 0;
                                }
                            }
                        }
                        parseActiveChar(strmrdr, ref prvc, c);
                    }
                }
            }
        }

        private string psvParsedLabel = "";
        
        private char[] unflushedPsvPars = null;
        private int unflushedPsvParsi = 0;
        private int unflushedPsvParsl = 0;
        private char psvParsc = (char)0;

        private bool parsedName = false;
        private static char[][] psvElemPropLabel = new char[][] { new char[] {'{','{'}, new char[] { '}','}' } };

        private int[] psvElmPrpLbli = new int[] { 0, 0 };

        private enum PassiveElemType { 
            None=0,
            Start = 1,
            Single = 2,
            End=3
        }

        private PassiveElemType isElemType = PassiveElemType.None;

        private const string validElemRegExp = @"^([a-z]|[A-Z])+\w*([:]{1}([a-z]|[A-Z])+\w*)*([-]{1}([a-z]|[A-Z])+\w*)?"; //" ^ ([a-z]|[A-Z])+([a-z]|[A-Z])*([:]{1}([a-z]|[A-Z])+)*([-]{1}([a-z]|[A-Z])+)?";

        private List<string> invalidPsvTags = null;
        private List<string> validPsvTags = null;
        private Dictionary<string,Dictionary<string,object>> validPsvTagDefs = null;
        private Dictionary<int, Dictionary<string, object>> validPsvTagSettings = null;

        private static System.Text.RegularExpressions.Regex validPsvLabel = new System.Text.RegularExpressions.Regex(validElemRegExp);
        private static void parsePassiveChar(ActiveReader strmrdr, ref char prvc,char c, ref int bufrl, ref char[] buffer, ref int bufi, ref int bufmxl)
        {
            if (strmrdr.unflushedPsvParsl == 0)
            {
                if (strmrdr.parsedName) {
                    if (strmrdr.psvElmPrpLbli[1] == 0 && strmrdr.psvElmPrpLbli[0] < psvElemPropLabel[0].Length)
                    {
                        if (strmrdr.psvElmPrpLbli[0] > 0 && psvElemPropLabel[0][strmrdr.psvElmPrpLbli[0]] == strmrdr.psvParsc && psvElemPropLabel[0][strmrdr.psvElmPrpLbli[0]] != c)
                        { 
                            strmrdr.psvElmPrpLbli[0] = 0;
                            strmrdr.psvParsc = (char)0;
                            strmrdr.parsedName = false;
                            strmrdr.psvParsedLabel += c;
                            strmrdr.isElemType = PassiveElemType.None;
                            strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                            strmrdr.psvParsedLabel = "";
                            strmrdr.unflushedPsvParsi = 0;
                            return;
                        }
                        if (psvElemPropLabel[0][strmrdr.psvElmPrpLbli[0]] == c)
                        {
                            strmrdr.psvElmPrpLbli[0]++;
                            if (strmrdr.psvElmPrpLbli[0] == psvElemPropLabel[0].Length)
                            {

                            }
                            else
                            {
                                strmrdr.psvParsc = c;
                            }
                        }
                        else {
                            if(strmrdr.psvElmPrpLbli[0]>0 || !(c + "").Trim().Equals("")){
                                strmrdr.parsedName = false;
                                strmrdr.psvElmPrpLbli[0] = 0;
                                strmrdr.psvParsc = (char)0;
                                strmrdr.parsedName = false;
                                strmrdr.psvParsedLabel += c;
                                strmrdr.isElemType = PassiveElemType.None;
                                strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                strmrdr.psvParsedLabel = "";
                                strmrdr.unflushedPsvParsi = 0;
                                return;
                            }
                        }
                    }
                    else if (strmrdr.psvElmPrpLbli[0] == psvElemPropLabel[0].Length) { 
                    }
                } else {
                    if (strmrdr.psvParsedLabel.Length == 0)
                    {
                        if (c == '<')
                        {
                            strmrdr.isElemType = PassiveElemType.Start;
                            strmrdr.psvParsedLabel += c;
                            return;
                        }
                        else {
                            strmrdr.psvParsedLabel += c;
                            strmrdr.isElemType = PassiveElemType.None;
                            strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                            strmrdr.psvParsedLabel = "";
                            strmrdr.unflushedPsvParsi = 0;
                            return;
                        }
                    }
                    else if (strmrdr.psvParsedLabel.Length > 0)
                    {
                        if(!(c + "").Trim().Equals("")) {
                            if (c == '/')
                            {
                                if (strmrdr.isElemType==PassiveElemType.Start)
                                {
                                    if (strmrdr.psvParsedLabel.Equals("<"))
                                    {
                                        strmrdr.isElemType = PassiveElemType.Single;
                                        strmrdr.psvParsedLabel += c;
                                        return;
                                    }
                                    else
                                    {
                                        if (strmrdr.isElemType!=PassiveElemType.End)
                                        {
                                            if (strmrdr.isElemType!=PassiveElemType.Single)
                                            {
                                                strmrdr.isElemType =PassiveElemType.Single;
                                                strmrdr.psvParsedLabel += c;
                                                return;
                                            }
                                            else
                                            {
                                                strmrdr.psvParsedLabel += c;
                                                strmrdr.isElemType=PassiveElemType.None;
                                                strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                                strmrdr.psvParsedLabel = "";
                                                strmrdr.unflushedPsvParsi = 0;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            strmrdr.psvParsedLabel += c;
                                            strmrdr.isElemType=PassiveElemType.None;
                                            strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                            strmrdr.psvParsedLabel = "";
                                            strmrdr.unflushedPsvParsi = 0;
                                            return;
                                        }
                                    }
                                }
                                else {
                                    strmrdr.psvParsedLabel += c;
                                    strmrdr.isElemType=PassiveElemType.None;
                                    strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                    strmrdr.psvParsedLabel = "";
                                    strmrdr.unflushedPsvParsi = 0;
                                    return;
                                }
                            }
                            else if (c != '>')
                            {
                                strmrdr.psvParsedLabel += c;
                                if (strmrdr.isElemType==PassiveElemType.Start || strmrdr.isElemType==PassiveElemType.End)
                                {
                                    var mtchstr = strmrdr.psvParsedLabel.Substring(strmrdr.isElemType==PassiveElemType.Start ? 1 : 2).Trim();
                                    var mtch = validPsvLabel.Match(mtchstr);
                                    if (!mtch.Success && mtch.Index == 0 && mtch.Length == mtchstr.Length)
                                    {
                                        strmrdr.isElemType=PassiveElemType.None;
                                        strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                        strmrdr.psvParsedLabel = "";
                                        strmrdr.unflushedPsvParsi = 0;
                                    }
                                }
                                else {
                                    strmrdr.isElemType=PassiveElemType.None;
                                    strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                    strmrdr.psvParsedLabel = "";
                                    strmrdr.unflushedPsvParsi = 0;
                                }
                                return;
                            }
                            else if (c == '>')
                            {
                                strmrdr.psvParsedLabel += c;
                                if (strmrdr.isElemType!=PassiveElemType.None)
                                {
                                    var mtchstr = strmrdr.isElemType==PassiveElemType.Start ? strmrdr.psvParsedLabel.Substring(1, strmrdr.psvParsedLabel.Length - 2) : strmrdr.isElemType==PassiveElemType.Single ? strmrdr.psvParsedLabel.Substring(1, strmrdr.psvParsedLabel.Length - 3) : strmrdr.isElemType==PassiveElemType.End ? strmrdr.psvParsedLabel.Substring(2, strmrdr.psvParsedLabel.Length - 3) : "";
                                    var mtch = validPsvLabel.Match(mtchstr);
                                    if (mtch.Success && mtch.Index == 0 && mtch.Length == mtchstr.Length && (strmrdr.invalidPsvTags == null || !strmrdr.invalidPsvTags.Contains(mtchstr)))
                                    {
                                        if (strmrdr.IsValidElem(mtchstr, strmrdr.isElemType))
                                        {

                                        }
                                        else
                                        {
                                            if (!(strmrdr.invalidPsvTags == null ? (strmrdr.invalidPsvTags = new List<string>()) : strmrdr.invalidPsvTags).Contains(mtchstr)) strmrdr.invalidPsvTags.Add(mtchstr);
                                            strmrdr.isElemType=PassiveElemType.None;
                                            strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                            strmrdr.psvParsedLabel = "";
                                            strmrdr.unflushedPsvParsi = 0;
                                        }
                                    }
                                    else
                                    {
                                        strmrdr.isElemType=PassiveElemType.None;
                                        strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                        strmrdr.psvParsedLabel = "";
                                        strmrdr.unflushedPsvParsi = 0;
                                    }
                                }
                                else
                                {
                                    strmrdr.isElemType=PassiveElemType.None;
                                    strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                    strmrdr.psvParsedLabel = "";
                                    strmrdr.unflushedPsvParsi = 0;
                                }
                                return;
                            }
                            else
                            {
                                strmrdr.psvParsedLabel += c;
                                strmrdr.isElemType=PassiveElemType.None;
                                strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                strmrdr.psvParsedLabel = "";
                                strmrdr.unflushedPsvParsi = 0;
                                return;
                            }
                        }
                        else
                        {
                            strmrdr.psvParsedLabel += c;
                            if (strmrdr.isElemType==PassiveElemType.Start || strmrdr.isElemType==PassiveElemType.End)
                            {
                                var mtchstr = strmrdr.psvParsedLabel.Substring(strmrdr.isElemType==PassiveElemType.Start ? 1 : 2).Trim();
                                var mtch = validPsvLabel.Match(mtchstr);
                                if(mtch.Success && mtch.Index==0 && mtch.Length == mtchstr.Length){
                                    //TODO
                                    strmrdr.parsedName = false;
                                    strmrdr.isElemType = PassiveElemType.None;
                                    strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                    strmrdr.psvParsedLabel = "";
                                    strmrdr.unflushedPsvParsi = 0;
                                } else {
                                    strmrdr.isElemType = PassiveElemType.None;
                                    strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                    strmrdr.psvParsedLabel = "";
                                    strmrdr.unflushedPsvParsi = 0;
                                }
                            }
                            else
                            {
                                strmrdr.isElemType = PassiveElemType.None;
                                strmrdr.unflushedPsvParsl = (strmrdr.unflushedPsvPars = strmrdr.psvParsedLabel.ToCharArray()).Length;
                                strmrdr.psvParsedLabel = "";
                                strmrdr.unflushedPsvParsi = 0;
                            }
                            return;
                        }
                    }
                }
            }
            if (strmrdr.foundCode)
            {
                (strmrdr.cntntchrs == null ? (strmrdr.cntntchrs = new char[81920]) : strmrdr.cntntchrs)[strmrdr.cntntchrsi++] = c;
                strmrdr.cntntchrsl++;
                if (strmrdr.cntntchrsl == strmrdr.cntntchrs.Length)
                {
                    (strmrdr.cntntBuffer == null ? (strmrdr.cntntBuffer = new List<char[]>()) : strmrdr.cntntBuffer).Add(strmrdr.cntntchrs);
                    strmrdr.cntntchrs = null;
                    strmrdr.cntntchrsi = 0;
                    strmrdr.cntntchrsl = 0;
                }
            } 
            else
            {
                buffer[bufi++] = c;
                bufrl++;
            }
        }

        internal Dictionary<string, object> LookupValidElemDefinition(string elemname,string elemRoot) {
            if (elemname.IndexOf(":") >=0) {
                Dictionary<string, object> elemdefs = null;
                var elemPath = elemname.Replace(":", "/");
                var elemFullPath = elemPath;
                if (this.validPsvTagDefs == null) {
                    elemdefs = new Dictionary<string, object>();
                }
            }
            return null;
        }

        private bool IsValidElem(string elemname, PassiveElemType passiveElemType)
        { 
            Dictionary<string,object> validdef = (this.validPsvTagDefs == null ? this.LookupValidElemDefinition(elemname,"") : this.validPsvTagDefs.ContainsKey(elemname) ? this.validPsvTagDefs[elemname]:null);
            if(validdef!=null&&!(this.validPsvTagDefs==null?(this.validPsvTagDefs=new Dictionary<string, Dictionary<string, object>>()): this.validPsvTagDefs).ContainsKey(elemname))
            {
                this.validPsvTagDefs.Add(elemname, validdef);
            }

            if (validdef == null)
            {
                (this.invalidPsvTags == null ? (this.invalidPsvTags = new List<string>()) : this.invalidPsvTags).Add(elemname);
            }

            return false;
        }

        private Dictionary<long,List<char[]>> CntntBufferMap
        {
            get { return this==this.TopReader ? this.cntntBufferMap == null ? this.cntntBufferMap = new Dictionary<long, List<char[]>>() : this.cntntBufferMap : this.TopReader.CntntBufferMap; }
        }

        private static void appendPassiveChar(StringBuilder psvParsedLabel, ActiveReader strmrdr, ref char prvc, char c, ref int bufrl, ref char[] buffer, ref int bufi, ref int bufmxl)
        {
            if (strmrdr.foundCode)
            {
                (strmrdr.cntntchrs == null ? (strmrdr.cntntchrs = new char[81920]) : strmrdr.cntntchrs)[strmrdr.cntntchrsi++] = c;
                strmrdr.cntntchrsl++;
                if (strmrdr.cntntchrsl == strmrdr.cntntchrs.Length)
                {
                    (strmrdr.cntntBuffer == null ? (strmrdr.cntntBuffer = new List<char[]>()) : strmrdr.cntntBuffer).Add(strmrdr.cntntchrs);
                    strmrdr.cntntchrs = null;
                    strmrdr.cntntchrsi = 0;
                    strmrdr.cntntchrsl = 0;
                }
            }
            else
            {
                buffer[bufi++] = c;
                bufrl++;
            }
        }
        private static void parseActiveChar(ActiveReader strmrdr, ref char prvc, char c)
        {
            if (strmrdr.hasCode)
            {
                flushPassiveContent(strmrdr, ref prvc);
                if (!strmrdr.foundCode)
                {
                    strmrdr.foundCode = true;
                }

                if (strmrdr.foundCdeTxt)
                {
                    if (strmrdr.cdeTxtPar == c)
                    {
                        strmrdr.foundCdeTxt = false;
                        strmrdr.cdeTxtPar = (char)0;
                        (strmrdr.cdechrs == null ? (strmrdr.cdechrs = new char[81920]) : strmrdr.cdechrs)[strmrdr.cdechrsi++] = c;
                        strmrdr.cdechrsl++;
                        if (strmrdr.cdechrsl == strmrdr.cdechrs.Length)
                        {
                            (strmrdr.code == null ? (strmrdr.code = new List<char[]>()) : strmrdr.code).Add(strmrdr.cdechrs);
                            strmrdr.cdechrs = null;
                            strmrdr.cdechrsi = 0;
                            strmrdr.cdechrsl = 0;
                        }
                    }
                    else if (c == '@')
                    {
                        foreach (var txtc in "\\u0040".ToCharArray())
                        {
                            (strmrdr.cdechrs == null ? (strmrdr.cdechrs = new char[81920]) : strmrdr.cdechrs)[strmrdr.cdechrsi++] = txtc;
                            strmrdr.cdechrsl++;
                            if (strmrdr.cdechrsl == strmrdr.cdechrs.Length)
                            {
                                (strmrdr.code == null ? (strmrdr.code = new List<char[]>()) : strmrdr.code).Add(strmrdr.cdechrs);
                                strmrdr.cdechrs = null;
                                strmrdr.cdechrsi = 0;
                                strmrdr.cdechrsl = 0;
                            }
                        }
                    }
                    else
                    {
                        (strmrdr.cdechrs == null ? (strmrdr.cdechrs = new char[81920]) : strmrdr.cdechrs)[strmrdr.cdechrsi++] = c;
                        strmrdr.cdechrsl++;
                        if (strmrdr.cdechrsl == strmrdr.cdechrs.Length)
                        {
                            (strmrdr.code == null ? (strmrdr.code = new List<char[]>()) : strmrdr.code).Add(strmrdr.cdechrs);
                            strmrdr.cdechrs = null;
                            strmrdr.cdechrsi = 0;
                            strmrdr.cdechrsl = 0;
                        }
                    }
                }
                else
                {
                    if(c=='\''||c=='"')
                    {
                        strmrdr.foundCdeTxt = true;
                        strmrdr.cdeTxtPar = c;
                    }
                    (strmrdr.cdechrs == null ? (strmrdr.cdechrs = new char[81920]) : strmrdr.cdechrs)[strmrdr.cdechrsi++] = c;
                    strmrdr.cdechrsl++;
                    if (strmrdr.cdechrsl == strmrdr.cdechrs.Length)
                    {
                        (strmrdr.code == null ? (strmrdr.code = new List<char[]>()) : strmrdr.code).Add(strmrdr.cdechrs);
                        strmrdr.cdechrs = null;
                        strmrdr.cdechrsi = 0;
                        strmrdr.cdechrsl = 0;
                    }
                }
            }
            else
            {
                if (!("" + c).Trim().Equals(""))
                {
                    strmrdr.hasCode = true;
                    parseActiveChar(strmrdr, ref prvc, c);
                }
            }
        }

        private static void flushPassiveContent(ActiveReader strmrdr,ref char prvc)
        {
            if (strmrdr.cntntchrs != null && strmrdr.cntntchrsl > 0)
            {
                var tmpcntntchrs = new char[strmrdr.cntntchrsl];
                System.Array.Copy(strmrdr.cntntchrs, 0, tmpcntntchrs, 0, strmrdr.cntntchrsl);
                strmrdr.cntntchrsi = 0;
                strmrdr.cntntchrsl = 0;
                strmrdr.cntntchrs = null;
                (strmrdr.cntntBuffer == null ? (strmrdr.cntntBuffer = new List<char[]>()) : strmrdr.cntntBuffer).Add(tmpcntntchrs);                
            }
            if (strmrdr.cntntBuffer != null && strmrdr.cntntBuffer.Count > 0)
            {
                //(strmrdr.cntntBufferMap == null ? (strmrdr.cntntBufferMap = new Dictionary<long, List<char[]>>()) : strmrdr.cntntBufferMap).Add(strmrdr.cntntBufferMap.Count, strmrdr.cntntBuffer);
                strmrdr.CntntBufferMap.Add(strmrdr.CntntBufferMap.Count, strmrdr.cntntBuffer);
                strmrdr.cntntBuffer = null;
                foreach (var psvc in ("WritePsvContent(" + (strmrdr.CntntBufferMap.Count - 1).ToString() + ");").AsSpan())
                {
                    parseActiveChar(strmrdr, ref prvc, psvc);
                    prvc = psvc;
                }
            }
        }



        public void Print(params object[] ss)
        {
            if (ss != null && ss.Length > 0)
            {
                foreach (var s in ss)
                {
                    this.TopReader.remainingContentRW.Writer.Write(s);
                }
            }
        }

        public void Println(params object[] ss)
        {
            if (ss != null && ss.Length > 0)
            {
                foreach (var s in ss)
                {
                    this.TopReader.remainingContentRW.Writer.Write(s);
                }
            }
            this.TopReader.remainingContentRW.Writer.Write("\r\n");
        }

        private delegate void WritingPsvContent(long cntntindex);
        public void WritePsvContent(long cntntindex) {
            if (this.cntntBufferMap != null && this.cntntBufferMap.ContainsKey(cntntindex))
            {
                if (this.cntntBufferMap[cntntindex].Count > 0)
                {
                    foreach (var s in this.cntntBufferMap[cntntindex])
                    {
                        this.remainingContentRW.Writer.Write(s);
                    }
                }
            }
        }

        public Stream BaseStream { get => /*this.streamReader.BaseStream*/null; }
        public Encoding CurrentEncoding { get => Encoding.UTF8;/*this.streamReader.CurrentEncoding;*/ }
        public bool EndOfStream { get => this.readingNextChars.Count==0; }
        public override int Peek() {
            return 0;// this.streamReader.Peek();
        }
        public override int Read(Span<char> buffer) {
            int actualR = 0;
            if (buffer != null && buffer.Length > 0 && buffer.IsEmpty) {
                char[] rdchrs = new char[buffer.Length];
                if ((actualR = this.Read(rdchrs, 0, rdchrs.Length)) > 0){
                    var spn = rdchrs.AsSpan(0, actualR);
                    spn.CopyTo(buffer);
                    spn.Clear();
                }
            }
            return actualR;//this.streamReader.Read(buffer);
        }

        private readonly char[] rd = new char[1];
        public override int Read() {
            if (this.Read(this.rd, 0, 1)>0) {
                return this.rd[0];
            }
            return -1;
        }

        public override int Read(char[] buffer, int index, int count) {
            this.internalActiveRead(buffer, index, count, ref count);
            return count;
        }
        public override async Task<int> ReadAsync(char[] buffer, int index, int count) {
            return await Task<int>.Run(() => {
                this.internalActiveRead(buffer, index, count, ref count);
                return count;
            });
        }
        public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default) {
            return await Task<int>.Run(() => {
                var chrs = new char[buffer.Length];
                var chrscount = 0;
                this.internalActiveRead(chrs, 0, chrscount, ref chrscount);
                new Memory<char>(chrs, 0, chrscount).CopyTo(buffer);
                return chrscount;
            }, cancellationToken);
        }
        public override int ReadBlock(char[] buffer, int index, int count) {
            this.internalActiveRead(buffer, index, count, ref count);
            return count;
        }
        public override int ReadBlock(Span<char> buffer) {
            var chrs = new char[buffer.Length];
            var chrscount = 0;
            this.internalActiveRead(chrs, 0, chrscount, ref chrscount);
            new Memory<char>(chrs, 0, chrscount).Span.CopyTo(buffer);
            return chrscount;
        }
        public override async ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            return await Task<int>.Run(() => {
                var chrs = new char[buffer.Length];
                var chrscount = 0;
                this.internalActiveRead(chrs, 0, chrscount, ref chrscount);
                new Memory<char>(chrs, 0, chrscount).CopyTo(buffer);
                return chrscount;
            }, cancellationToken);
        }
        public override async Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            return await Task<int>.Run(() =>
            {
                this.internalActiveRead(buffer, index, count, ref count);
                return count;
            });
        }
        public override string ReadLine()
        {
            return this.ReadLineAsync().Result;
        }
        public async override Task<string> ReadLineAsync()
        {
            var chr = new char[1];
            var s = "";
            var prvchr = (char)0;
            while (await this.ReadAsync(chr, 0, 1) > 0) {
                if (chr[0] == '\n') {
                    break;
                } else
                {
                    if (chr[0] == '\r')
                    {
                        continue;
                    }
                    else {
                        if (prvchr == '\r') {
                            s += prvchr;
                        }
                        s += chr[0];
                    }
                }
                prvchr = chr[0];
            }
            return s;
        }

        public override string ReadToEnd()
        {
            return this.ReadToEndAsync().Result;
        }
        public async override Task<string> ReadToEndAsync()
        {
            var chrstoend = new char[8192];
            var chrstoendl = 0;
            var s = "";
            while ((chrstoendl = await this.ReadAsync(chrstoend, 0, chrstoend.Length)) > 0) {
                s += chrstoend.AsSpan(0, chrstoend.Length).ToArray();
            }
            return s;
        }

        private bool disposed = true;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.disposed)
                {
                    this.disposed = false;
                    if (this.readingNextChars != null)
                    {
                        while (this.readingNextChars.Count > 0)
                        {
                            this.readingNextChars[0](null, 0, 0, true);
                            this.readingNextChars[0] = null;
                            this.readingNextChars.RemoveAt(0);
                        }
                        this.readingNextChars.Clear();
                        this.readingNextChars = null;
                    }
                    if (this.internchars != null)
                    {
                        this.internchars = null;
                    }
                    if (this.atvcharsi != null)
                    {
                        this.atvcharsi = null;
                    }
                    if (this.atvchrs != null)
                    {
                        this.atvchrs = null;
                    }
                    if (this.chrstoprscs != null)
                    {
                        this.chrstoprscs = null;
                    }
                    if (this.cntntBuffer != null)
                    {
                        while (this.cntntBuffer.Count > 0)
                        {
                            this.cntntBuffer[0] = null;
                            this.cntntBuffer.RemoveAt(0);
                        }
                        this.cntntBuffer.Clear();
                        this.cntntBuffer = null;
                    }
                    if (this.cntntBufferMap != null)
                    {
                        if (this.cntntBufferMap.Count > 0)
                        {
                            var mxkey = (long)(this.cntntBufferMap.Count - 1);
                            while (mxkey >= 0)
                            {
                                var cntntv = this.cntntBufferMap[mxkey];
                                while (cntntv.Count > 0)
                                {
                                    cntntv[0] = null;
                                    cntntv.RemoveAt(0);
                                }
                                cntntv.Clear();
                                this.cntntBufferMap.Remove(mxkey);
                                mxkey--;
                            }
                            this.cntntBufferMap.Clear();
                        }
                        this.cntntBufferMap = null;
                    }
                    if (this.cntntchrs != null)
                    {
                        this.cntntchrs = null;
                    }
                    if (this.code != null)
                    {
                        while (this.code.Count > 0)
                        {
                            this.code[0] = null;
                            this.code.RemoveAt(0);
                        }
                        this.code.Clear();
                        this.code = null;
                    }
                    if (this.remainingContentRW != null)
                    {
                        this.remainingContentRW.Dispose();
                        this.remainingContentRW = null;
                    }
                    if (this.invalidPsvTags != null)
                    {
                        this.invalidPsvTags.Clear();
                        this.invalidPsvTags = null;
                    }
                    if (this.validPsvTagDefs != null)
                    {
                        this.validPsvTagDefs.Clear();
                        this.validPsvTagDefs = null;
                    }
                    if (this.validPsvTagDefs != null)
                    {
                        if (this.validPsvTagDefs.Count > 0)
                        {
                            foreach (var valdefk in this.validPsvTagDefs.Keys.ToArray())
                            {
                                this.validPsvTagDefs[valdefk].Clear();
                                this.validPsvTagDefs.Remove(valdefk);
                            }
                        }
                        this.validPsvTagDefs.Clear();
                        this.validPsvTagDefs = null;
                    }
                    if (this.validPsvTagSettings != null)
                    {
                        if (this.validPsvTagSettings.Count > 0)
                        {
                            foreach (var valdefk in this.validPsvTagSettings.Keys.ToArray())
                            {
                                this.validPsvTagSettings[valdefk].Clear();
                                this.validPsvTagSettings.Remove(valdefk);
                            }
                        }
                        this.validPsvTagSettings.Clear();
                    }
                    if (this.loadedModules != null)
                    {
                        if (this.loadedModules.Count > 0)
                        {
                            var keys = new string[this.loadedModules.Count];
                            foreach (var k in keys)
                            {
                                if (this.loadedModulesObjects != null && this.loadedModulesObjects.ContainsKey(k))
                                {
                                    this.loadedModulesObjects[k] = null;
                                    this.loadedModulesObjects.Remove(k);
                                }
                                this.loadedModules[k] = null;
                                this.loadedModules.Remove(k);
                            }
                            this.loadedModules.Clear();
                        }
                        this.loadedModules = null;
                    }
                    if (this.loadedModulesObjects != null)
                    {
                        this.loadedModulesObjects.Clear();
                        this.loadedModulesObjects = null;
                    }
                    if (this.prntAtvRdr != null)
                    {
                        this.prntAtvRdr = null;
                    }
                }
            }
            GC.SuppressFinalize(this);
        }

        public override void Close()
        {
            if (this.disposeReader)
            {
                /*if (this.streamReader != null)
                {
                    this.streamReader.Close();
                }*/
            }
        }

        private ActiveReader TopReader
        {
            get { return this.prntAtvRdr == null ? this : this.prntAtvRdr.TopReader; }
        }

        [Obsolete]
        public bool Require(ref Bcoring.ES6.Core.Context cntxt,string modpath,out JSValue result, params object[]args)
        {
            result = null;
            if ((modpath = modpath == null ? "" : modpath.Trim()).Equals("")) return false;
            Bcoring.ES6.Module modresult = null;
            if (loadedModules != null && loadedModules.ContainsKey(modpath)) 
            {
                modresult = loadedModules[modpath];
            } else
            {
                Type modType = null;
                var modatvrdr = this.FindSource(modpath.LastIndexOf(".")==-1?(modpath+".js"):modpath, out modType);
                using (var modAtvstrm = new ActiveReader(this, true))
                {
                    modAtvstrm.foundCode = true;
                    var modobj = modType == null ? null : this.loadedModulesObjects != null && this.loadedModulesObjects.ContainsKey(modpath) ? this.loadedModulesObjects[modpath] :this.InvokeSourceType(sourceType: modType);
                    if (modatvrdr == null && modType != null)
                    {
                        if (modobj != null)
                        {
                            var cde = "";
                            int prmcount = 0;
                            foreach (var modmethod in modType.GetTypeInfo().DeclaredMethods)
                            {
                                //if (propMethods.Contains(modmethod.Name)) continue;
                                prmcount = modmethod.GetParameters().Length;
                                var methdargs = "";
                                while (prmcount > 0)
                                {
                                    methdargs += "a" + prmcount.ToString();
                                    prmcount--;
                                    if (prmcount > 0)
                                    {
                                        methdargs += ",";
                                    }
                                }
                                cde += "export function " + modmethod.Name + "(" + methdargs + "){ return  _" + modType.Name + "." + modmethod.Name + "(" + args + ");};";
                            }
                            //propMethods.Clear();
                            modatvrdr = new StringReader("<@"+cde+ "@>");
                        }
                    }
                    if (modatvrdr != null)
                    {
                        modAtvstrm.AddReadingSource(modatvrdr);

                        modAtvstrm.ReadToEnd();
                        Script script = Script.Parse(modAtvstrm.Code());

                        Bcoring.ES6.Module mod = new Bcoring.ES6.Module(modpath, script: script, cntxt.GlobalContext);
                        mod.ModuleResolversChain.Add(modAtvstrm.TopReader);
                        if (mod != null)
                        {   
                            if (modobj != null) {
                                (loadedModulesObjects == null ? (loadedModulesObjects = new Dictionary<string, Object>()) : loadedModulesObjects).Add(modpath, modobj);
                            }
                            (loadedModules == null ? (loadedModules = new Dictionary<string, Bcoring.ES6.Module>()) : loadedModules).Add(modpath, mod);
                            modresult = mod;
                        }
                    }
                }                
            }
            if (modresult != null)
            {
                var modcntxt = modresult.Context;

                if (this.activeMap != null)
                {
                    foreach (var kv in this.activeMap)
                    {
                        modcntxt.DefineVariable(kv.Key).Assign(JSValue.Marshal(kv.Value));
                    }
                }

                modcntxt.DefineVariable("WritePsvContent").Assign(JSValue.Marshal((Action<long>)this.WritePsvContent));
                modcntxt.DefineVariable("Print").Assign(JSValue.Marshal((Printing)this.Print));
                modcntxt.DefineVariable("Println").Assign(JSValue.Marshal((Printing)this.Println));
                modcntxt.DefineVariable("Interrupt").Assign(JSValue.Marshal((Interrupting)this.Interrupt));
                modcntxt.DefineVariable("require").Assign(JSValue.Marshal((Requiring)new Requiring((path, args) => {
                    JSValue result;
                    this.TopReader.Require(ref modcntxt, path, out result, args: args);
                    return result;
                })));
                if (loadedModulesObjects!= null && loadedModulesObjects.ContainsKey(modpath))
                {
                    var modobj = loadedModulesObjects[modpath];
                    modcntxt.DefineVariable("_" + modobj.GetType().Name).Assign(JSValue.Marshal(modobj));
                }
                modresult.Script.Evaluate(modcntxt);
                //result = modresult.Script.Root.Variables;
            }
            return result!=null;
        }

        private Dictionary<string, Bcoring.ES6.Module> loadedModules = null;
        private Dictionary<String, Object> loadedModulesObjects = null;
        public bool TryGetModule(ModuleRequest moduleRequest, out Bcoring.ES6.Module result)
        {
            result = null;
            if (loadedModules != null && loadedModules.ContainsKey(moduleRequest.AbsolutePath)) 
            {
               result = loadedModules[moduleRequest.AbsolutePath];
            } else
            {
                Type modType = null;
                var modatvrdr = this.FindSource(moduleRequest.AbsolutePath,out modType);
                using (var modAtvstrm = new ActiveReader(this, true))
                {
                    modAtvstrm.foundCode = true;
                    var modobj = modType == null ? null : this.loadedModulesObjects != null && this.loadedModulesObjects.ContainsKey(moduleRequest.AbsolutePath) ? this.loadedModulesObjects[moduleRequest.AbsolutePath]:this.InvokeSourceType(sourceType: modType);
                    if (modatvrdr == null && modType != null)
                    {
                        if (modobj != null)
                        {
                            var cde = "";
                            int prmcount = 0;
                            foreach (var modmethod in modType.GetTypeInfo().DeclaredMethods)
                            {
                                //if (propMethods.Contains(modmethod.Name)) continue;
                                prmcount = modmethod.GetParameters().Length;
                                var args = "";
                                while(prmcount>0)
                                {
                                    args += "a" + prmcount.ToString();
                                    prmcount--;
                                    if (prmcount > 0)
                                    {
                                        args += ",";
                                    }
                                }
                                cde += "export function " + modmethod.Name + "(" + args + "){ "+ (modmethod.Name.StartsWith("set_")?"":"return") + "  _" + modType.Name + "." + modmethod.Name + "(" + args + ");};";
                            }
                            //propMethods.Clear();
                            modatvrdr = new StringReader("<@"+cde+"@>");
                        }
                    }
                    if (modatvrdr != null)
                    {
                        modAtvstrm.AddReadingSource(modatvrdr);

                        modAtvstrm.ReadToEnd();
                        Script script = Script.Parse(modAtvstrm.Code());

                        Bcoring.ES6.Module mod = new Bcoring.ES6.Module(moduleRequest.AbsolutePath, script: script, moduleRequest.Initiator.Context.GlobalContext);
                        mod.ModuleResolversChain.Add(modAtvstrm.TopReader);
                        if (mod != null)
                        {   
                            if (modobj != null) {
                                (loadedModulesObjects == null ? (loadedModulesObjects = new Dictionary<string, Object>()) : loadedModulesObjects).Add(moduleRequest.AbsolutePath, modobj);
                            }
                            (loadedModules == null ? (loadedModules = new Dictionary<string, Bcoring.ES6.Module>()) : loadedModules).Add(moduleRequest.AbsolutePath, mod);
                            result = mod;
                        }
                    }
                }                
            }
            if (result != null)
            {
                var cntxt = result.Context;

                if (this.activeMap != null)
                {
                    foreach (var kv in this.activeMap)
                    {
                        cntxt.DefineVariable(kv.Key).Assign(JSValue.Marshal(kv.Value));
                    }
                }

                cntxt.DefineVariable("WritePsvContent").Assign(JSValue.Marshal((Action<long>)this.WritePsvContent));
                cntxt.DefineVariable("Print").Assign(JSValue.Marshal((Printing)this.Print));
                cntxt.DefineVariable("Println").Assign(JSValue.Marshal((Printing)this.Println));
                cntxt.DefineVariable("Interrupt").Assign(JSValue.Marshal((Interrupting)this.Interrupt));

                if (loadedModulesObjects!= null && loadedModulesObjects.ContainsKey(moduleRequest.AbsolutePath))
                {
                    var modobj = loadedModulesObjects[moduleRequest.AbsolutePath];
                    cntxt.DefineVariable("_" + modobj.GetType().Name).Assign(JSValue.Marshal(modobj));
                }
            }
            return result!=null;
        }

        private System.IO.TextReader FindSource(string sourcePath,out Type sourceType)
        {
            sourceType = this.SourceFinder==null?null:this.SourceFinder.FindActiveSourceType(sourcePath);
            System.IO.TextReader sourceFound = null;
            
            Task.WaitAll(Task.Run(async () => {
                sourceFound = (sourceFound = this.SourceFinder == null ? null : await this.sourceFinder.FindActiveSourceAsync(sourcePath));
            }));
            
            return sourceFound;
        }

        private Object InvokeSourceType(Type sourceType,params object[] args)
        {
            return this.SourceFinder == null ? null : this.SourceFinder.InvokeActiveSourceType(sourceType,args:args);
        }
    }
}
