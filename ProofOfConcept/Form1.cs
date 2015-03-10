/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Diagnostics.Runtime;
using NetExt.Shim;
using NetExt.HeapCacheUtil;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace ProofOfConcept
{
    public partial class Form1 : Form
    {
        ClrRuntime m_runtime = null;
        ClrHeap m_heap = null;
        IEnumerator<ulong> heapObjs;
        HeapCache cache = null;
        int count = 0;
        ulong currObj = 0;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(textBox1.Text))
            {
                if (File.Exists(textBox1.Text))
                {
                    openFileDialog1.InitialDirectory = Path.GetDirectoryName(textBox1.Text);
                    openFileDialog1.FileName = Path.GetFileName(textBox1.Text);
                }
            }
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (textBox1.Text != openFileDialog1.FileName)
                    m_runtime = null;
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private static string pFormat = String.Format(":x{0}", Marshal.SizeOf(IntPtr.Zero)*2);
        private static string pointerFormat(string Message)
        {

            return Message.Replace(":%p", pFormat);

        }

        private void Write(string Text, params object[] Params)
        {

            textBox3.AppendText(String.Format(pointerFormat(Text), Params).Replace("\n", Environment.NewLine));
        }

        private void WriteLine(string Text, params object[] Params)
        {
            Write(Text, Params);
            textBox3.AppendText(Environment.NewLine);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            StartRuntime();
            if (m_heap == null) return;
            ClrType currType = null;
            do
            {
                if (!heapObjs.MoveNext())
                {
                    WriteLine("=====================================================-===");
                    WriteLine(" End of Stack. It will be reset to the beggining of heap");
                    WriteLine("=========================================================");
                    heapObjs.Reset();
                    return;
                }
                count++;
                if (count % 1000 == 0)
                {
                    WriteLine("{0} Objects counted", count);
                }
                System.Threading.Thread.Sleep(1);
                
                currObj = heapObjs.Current;
                currType = m_heap.GetObjectType(currObj);
                textBox4.Text = String.Format("{0:x16}", currObj);
            } while (!(String.IsNullOrWhiteSpace(textBox2.Text) || currType.Name.ToLower().Contains(textBox2.Text.ToLower())));
            WriteLine("{0:%p} {2:%p} {1}", currObj, currType.Name, ReadPointer(currObj));

        }

        private void StartRuntime()
        {
            if (m_runtime == null)
            {
                StartRuntime(textBox1.Text);
            }
        }

        public void DumpModules(ulong DomainAddr = 0, bool DebugOnly = false, bool NotMicrosoftOnly = false)
        {
            var domains = AdHoc.GetDomains(m_runtime);
            if (domains == null || domains.Length < 2)
            {
                WriteLine("Unable to get Application Domains. This is not expected.");
                return;
            }

            

        }

        public void DumpDomains()
        {
            var domains = AdHoc.GetDomains(m_runtime);
            if (domains == null || domains.Length < 2)
            {
                WriteLine("Unable to get Application Domains. This is not expected.");
                return;
            }

            if (m_runtime.PointerSize == 8)
                WriteLine("Address          Domain Name                                                 Modules Base Path & Config");
            else
                WriteLine("Address  Domain Name                                                 Modules Base Path & Config");


            for (int i = 0; i < domains.Length; i++)
            {
                if(IsInterrupted())
                    return;
                
                Write("{0:%p} ", domains[i].Address);
                Write("{0, -60} ", i == 0 ? "System" : i == 1 ? "Shared" : domains[i].Name);
                Write("{0,6:#,#} ", domains[i].Modules.Count);
                if(!String.IsNullOrEmpty(domains[i].ApplicationBase)) Write("Base Path: {0} ", domains[i].ApplicationBase);
                if(!String.IsNullOrEmpty(domains[i].ConfigurationFile)) Write("Config: {0} ", domains[i].ConfigurationFile);
                WriteLine("");
            }


        }

        private void StartRuntime(string Target)
        {
            DataTarget dataTarget = DataTarget.LoadCrashDump(Target);
            ClrInfo latest = null;
            foreach(var version in dataTarget.ClrVersions)
            {
                WriteLine("Version: {0}.{1}.{2}.{3} from {4}", version.Version.Major, version.Version.Minor, version.Version.Patch, version.Version.Revision, version.DacInfo.FileName);
                latest  = version;
            }
            m_runtime = dataTarget.CreateRuntime(latest.TryDownloadDac());
            ulong strMT, arrMT, freeMT = 0;
            AdHoc.GetCommonMT(m_runtime, out strMT, out arrMT, out freeMT);

            WriteLine("Free MT: {0:x16}, String MT: {1:x16}, Array MT: {2:x16}", freeMT, strMT, arrMT);

            WriteLine("== App Domains ===");
            int i = 0;
            DumpDomains();
            //foreach (var appDomain in AdHoc.GetDomains(m_runtime))
            //{
            //    i++;
            //    if (i == 1)
            //    {
            //        WriteLine("{0:x16} System", appDomain.Address);
            //        WriteLine("  Modules: {0}", appDomain.Modules.Count);
            //        continue;
            //    }
            //    if (i == 2)
            //    {
            //        WriteLine("{0:x16} Shared", appDomain.Address);
            //        WriteLine("  Modules: {0}", appDomain.Modules.Count);
            //        continue;
            //    }



            //    WriteLine("{0:x16} {1}", appDomain.Address, appDomain.Name);
            //    WriteLine("  {0}{1}", appDomain.ApplicationBase, appDomain.ConfigurationFile);
            //    WriteLine("  Modules: {0}", appDomain.Modules.Count);
                
            //}

            WriteLine("==================");
            WriteLine("Heap(s): {0}  GC Server Mode: {1}", m_runtime.HeapCount, m_runtime.ServerGC);
            m_heap = m_runtime.GetHeap();
            heapObjs = m_heap.EnumerateObjects().GetEnumerator();
            count = 0;

            

        }

        public ulong ReadPointer(ulong Address)
        {
            StartRuntime();
            ulong ptr;
            m_runtime.ReadPointer(Address, out ptr);
            return ptr;
        }
        public ulong GetDomain(ulong Address)
        {
            StartRuntime();
            foreach (var domain in AdHoc.GetDomains(m_runtime))
            {
                foreach (var module in domain.Modules)
                {
                    if (module.ImageBase == Address)
                        return domain.Address;
                }

            }
            
            return 0;
        }

        private string TrimRight(string Str, int size)
        {
            if (Str.Length <= size)
                return Str;
            return Str.Substring(Str.Length - size);
        }

        private void DumpFields(ulong Address, ClrType type = null)
        {
            StartRuntime();
            ClrType obj;
            if (type == null)
                obj = m_heap.GetObjectType(Address);
            else
                obj = type;

            if (cache == null)
                cache = new HeapCache(m_runtime, ShowProgress);
            MDType tp = new MDType(obj);
            MD_TypeData data;
            tp.GetHeader(Address,out data);
            int count = 0;
            tp.GetAllFieldsDataRawCount(out count);
            int temp=0;
            MD_FieldData[] fields = new MD_FieldData[count];

            tp.GetAllFieldsDataRaw(data.isValueType ? 1 : 0, count, fields, out temp);

            for (int i=0;i<count;i++)
            {
                string typeName;
                string Name;
                tp.GetRawFieldTypeAndName(i,out typeName, out Name);
                MD_TypeData fd;
                ClrType ftp = AdHoc.GetTypeFromMT(m_runtime, fields[i].MethodTable);
                MDType ft = new MDType(ftp);
                ulong pointer = 0;

                tp.GetRawFieldAddress(Address, data.isValueType ? 1 : 0, i, out pointer);
                if (fields[i].isValueType)
                    ft.GetHeader(pointer, out fd);
                else
                    ft.GetHeader(ReadPointer(pointer), out fd);
                Write("{0:x16} {1:x4} {5:x16} {6} +{2:x4} {3,30} {4,30} {7} ", fd.module,
                    fd.token, fields[i].offset, TrimRight(typeName, 30), Name,
                    data.MethodTable,fields[i].isThreadStatic ? " thread " : fields[i].isStatic ? " Static " : "        ",
                    fields[i].isEnum  ?
                    AdHoc.GetEnumName(ftp, 
                    ReadPointer(pointer) & (fd.size == 4 ?
                    0x0000FFFF : ulong.MaxValue))
                    : ""/*cache.GetFieldValue(Address, Name, obj)*/);
                 ulong effAddress=pointer;
                 if(fd.isValueType)
                 {
                     if (fields[i].isEnum)
                     {
                         WriteLine("");
                         continue;
                     }
                     
                     try
                     {
                         WriteLine("{0}", ftp.GetValue(pointer));
                     }
                     catch
                     {
                         WriteLine("{0}", ReadPointer(pointer) & (fd.size == 4 ? 0xFFFFFFFF :
                               fd.size == 8 ? 0x00000000FFFFFFFF : ulong.MaxValue));
                     }
                     continue;
                 } else
                 {
                     
                     if (pointer != 0)
                         effAddress = ReadPointer(pointer);

                     Write("({0:x16}) ",effAddress);
                     if (effAddress == 0)
                     {
                         WriteLine("");
                         continue;
                     }
                     if(fd.isString)
                     {
                        string str;

                         tp.GetString(effAddress, out str);
                         Write("{0}",str);
                     }
                     WriteLine("");
                     
                 }
                        
               

            }
            /*
            var fields =
                from f in obj.Fields
                orderby f.Offset
                select f;

            foreach (var field in fields)
            {
                WriteLine("{0:x16} {1:x4} +{2:x4} {3,30} {4,30}", field.Type.Module.AssemblyId, 
                    field.Type.MetadataToken, field.Offset, TrimRight(field.Type.Name, 30), field.Name);

                
            }
            if (obj.StaticFields.Count > 0)
            {
                var statFields =
                    from s in obj.StaticFields
                    orderby s.Offset
                    select s;
                WriteLine("Static Fields:");
                foreach (var field in statFields)
                {
                    WriteLine("{0:x16} {1:x4} +{2:x4} {3,30} {4,30}", field.Type.Module.AssemblyId,
                        field.Type.MetadataToken, field.Offset, TrimRight(field.Type.Name, 30), field.Name);
                }
            }
            if (obj.ThreadStaticFields.Count > 0)
            {
                var threadFields =
                    from t in obj.ThreadStaticFields
                    orderby t.Offset
                    select t;
                WriteLine("Thread Static Fields:");
                foreach (var field in threadFields)
                {
                    WriteLine("{0:x16} {1:x4} +{2:x4} {3,30} {4,30}", field.Type.Module.AssemblyId,
                        field.Type.MetadataToken, field.Offset, TrimRight(field.Type.Name, 30), field.Name);
                }

            }
            */
        }

        private ulong GetEEClass(ulong MethodTable)
        {
            StartRuntime();
            return AdHoc.GetEEFromMT(m_runtime, MethodTable);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            StartRuntime();
            if (currObj == 0)
            {
                WriteLine("No object selected");
                return;
            }
            var type = m_heap.GetObjectType(currObj);
            if (type.IsException)
            {
                DumpException(currObj);
                return;
            }
            WriteLine("===================================================");
            WriteLine("000>!wdo {0:x16}",currObj);
            WriteLine("Address: {0:x16}", currObj);
            WriteLine("EE Class: {0:x16}", GetEEClass(ReadPointer(currObj)));
            WriteLine("Method Table: {0:x16}", ReadPointer(currObj));
            WriteLine("Class Name: {0}", type.Name);
            WriteLine("Size: {0}", type.GetSize(currObj));
            WriteLine("Instance Fields: {0}", type.Fields.Count);
            WriteLine("Static Fields: {0}", type.StaticFields.Count);
            WriteLine("Total Fields: {0}", type.Fields.Count + type.StaticFields.Count);
            var seg = m_heap.GetSegmentByAddress(currObj);
            
            WriteLine("Heap/Generation: {0}/{1}", seg.ProcessorAffinity, m_heap.GetGeneration(currObj));

            WriteLine("Module: {0:x16}", type.Module.ImageBase);
            WriteLine("Assembly: {0:x16}", type.Module.AssemblyId);

            WriteLine("File Name: {0}", type.Module.FileName);


            WriteLine("Domain: {0:x16}",AdHoc.GetDomainFromMT(m_runtime, ReadPointer(currObj)));

            /*
            foreach(var domainAddr in Hacks.GetDomainFromMT(runtime, ReadPointer(currObj))
            { 
                Write("{0:x16} ", domainAddr);
            }
            WriteLine("");
            */

            WriteLine("===================================================");
            DumpFields(currObj);
        }

        public static string DumpStack(IList<ClrStackFrame> Stack, int WordSize, bool SkipAddress=false)
        {
            if (Stack == null || Stack.Count == 0)
            {
                return "(no managed stack found)\n";
            }
            StringBuilder sb = new StringBuilder();
            if (WordSize == 8)
                if(!SkipAddress)
                    sb.Append("SP               IP               Function\n");
                else
                    sb.Append("IP               Function\n");

            else
                if(SkipAddress)
                    sb.Append("IP       Function\n");
                else
                    sb.Append("SP       IP       Function\n");
            foreach (var frame in Stack)
            {
                if(!SkipAddress)
                    sb.AppendFormat(pointerFormat("{0:%p} "), frame.StackPointer);
                sb.AppendFormat(pointerFormat("{0:%p} "), frame.InstructionPointer);
                sb.Append(frame.DisplayString);
                sb.Append("\n");
            }
            return sb.ToString();

        }

        public void DumpException(ulong ObjRef)
        {
            var exception = m_heap.GetExceptionObject(ObjRef);
            if (exception == null)
            {
                WriteLine("No expeception found at {0:%p}", ObjRef);
                WriteLine("");
                return;
            }
            WriteLine("Address: {0:%p}", ObjRef);
            WriteLine("Exception Type: {0}", exception.Type.Name);
            WriteLine("Message: {0}", exception.Message);
            Write("Inner Exception: ");
            if (exception.Inner == null)
                WriteLine("(none)");
            else
                WriteLine("<link cmd=\"!wpe {0:%p}\">{0:%p}</link> {1} {2}</link>", exception.Inner.Address,
                    exception.Inner.Type.Name.Replace("<","&lt;").Replace(">","&gt;"),exception.Inner.Message);
            WriteLine("Stack:");
            WriteLine("{0}",DumpStack(exception.StackTrace, m_runtime.PointerSize));
            WriteLine("HResult: {0:x4}", exception.HResult);
            WriteLine("");
        }

        private bool IsInterrupted()
        {
            System.Threading.Thread.Sleep(0);
            return false;
        }

        public void DumpAllExceptions(IMDObjectEnum Exceptions)
        {
            Dictionary<string, List<ulong>> allExceptions = new Dictionary<string, List<ulong>>(); 
            foreach (var obj in ((MDObjectEnum)Exceptions).List)
            {
                ClrException ex = m_heap.GetExceptionObject(obj);
                if (ex != null)
                {
                    if (IsInterrupted())
                        return;
                    string key = String.Format("{0}\0{1}\0{2}", ex.Type.Name, ex.Message, DumpStack(ex.StackTrace, m_heap.GetRuntime().PointerSize,true));
                    if (!allExceptions.ContainsKey(key))
                    {
                        allExceptions[key] = new List<ulong>();
                    }
                    allExceptions[key].Add(obj);
                }
            }

            int exCount = 0;
            int typeCount = 0;
            foreach (var key in allExceptions.Keys)
            {
                typeCount++;
                exCount += allExceptions[key].Count;
                Write("{0,8:#,#} of Type: {1}",allExceptions[key].Count, key.Split('\0')[0]);
                for(int i=0;i<Math.Min(3,allExceptions[key].Count);i++)
                {
                    Write(" <link cmd=\"!wpe {0:%p}\">{0:%p}</link>", (allExceptions[key])[i]);
                }
                ClrException ex = m_heap.GetExceptionObject((allExceptions[key])[0]);
                WriteLine("");
                WriteLine("Message: {0}", key.Split('\0')[1]);
                WriteLine("Inner Exception: {0}", ex.Inner == null ? "(none)" : ex.Inner.Type.Name);
                WriteLine("Stack:");
                WriteLine("{0}", key.Split('\0')[2]);
                WriteLine("");

            }
            WriteLine("{0:#,#} Exceptions in {1:#,#} unique type/stack combinations (duplicate types in similar stacks may be rethrows)",exCount,typeCount);
            WriteLine("");
        }

        public void PrintFieldVisibility(ClrField field, bool isStatic = false)
        {
            Write("{0}", field.IsInternal ? "internal " : field.IsProtected ? "protected " : field.IsPrivate ? "private " : field.IsPublic ? "public " : "undefinedvisibility ");
            if (isStatic)
                Write("static ");
            Write("{0} ", field.Type == null ? "object" : field.Type.Name);
            WriteLine("{0};", field.Name);
        }

        public void DumpClass(ulong MethodTable)
        {


            ClrType type = AdHoc.GetTypeFromMT(m_runtime, MethodTable);
            if (type == null)
            {
                WriteLine("No type with Method Table {0:%p}", MethodTable);
                WriteLine("");
                return;
            }
            string fileName = type.Module == null || !type.Module.IsFile ? "(dynamic)" : type.Module.FileName;
            WriteLine("// Method Table: {0}", MethodTable);
            WriteLine("// Module Address: {0:%p}", type.Module == null ? 0 : type.Module.ImageBase);
            WriteLine("// Debugging Mode: {0}", type.Module == null ? "(NA in Dynamic Module)" : type.Module.DebuggingMode.ToString());
            WriteLine("// Filename: {0}", fileName);
            WriteLine("namespace {0} {1}",type.Name.Substring(0,type.Name.LastIndexOf(".")), "{");

            WriteLine("");
            Write(" ");
            Write("{0}", type.IsInternal ? "internal " : type.IsProtected ? "protected " : type.IsPrivate ? "private " : type.IsPublic ? "public " : "undefinedvisibility ");
            Write("{0}", type.IsSealed ? "sealed " : "");
            Write("{0}", type.IsAbstract ? "abstract " : "");
            Write("{0}", type.IsInterface ? "interface " : "");
            Write("{0}", type.IsValueClass ? "struct " : "");
            Write("{0}", !type.IsValueClass && !type.IsInterface ? "class " : "");
            Write("{0}", type.Name.Split('.')[type.Name.Split('.').Length - 1]);
            if ((type.BaseType != null && type.BaseType.Name != "System.Object") || type.Interfaces.Count > 0)
            {
                Write(": ");
                if (type.BaseType != null && type.BaseType.Name != "System.Object")
                {
                    Write("<link cmd=\"!wclass {0:%p}\">{1}</link>", HeapStatItem.GetMTOfType(type.BaseType), type.BaseType.Name);
                    if (type.Interfaces.Count > 0)
                        Write(", ");
                }
                for (int i = 0; i < type.Interfaces.Count; i++)
                {
                    Write("{0}", type.Interfaces[i].Name);
                    if (i < type.Interfaces.Count - 1)
                        Write(", ");
                }
            }
            WriteLine("");
            WriteLine("{0}", " {");
            WriteLine("\t//");
            WriteLine("\t// Fields");
            WriteLine("\t//");
            WriteLine("");
            foreach (var field in type.Fields)
            {
                Write("\t");
                PrintFieldVisibility(field);
            }
            WriteLine("");
            WriteLine("\t//");
            WriteLine("\t// Static Fields");
            WriteLine("\t//");
            WriteLine("");
            foreach (var field in type.StaticFields)
            {
                Write("\t");
                PrintFieldVisibility(field, true);
            }

            foreach (var field in type.ThreadStaticFields)
            {
                Write("\t");
                PrintFieldVisibility(field, true);
            }

            var properties =
                from m in type.Methods
                where m.Name.StartsWith("get_") ||
                    m.Name.StartsWith("set_")
                orderby m.Name.Substring(4).Split('(')[0], -(int)m.Name[0]
                select m;

            WriteLine("");
            WriteLine("\t//");
            WriteLine("\t// Properties");
            WriteLine("\t//");
            WriteLine("");

            List<string> propstr = new List<string>();
            int propCount = 0;
            foreach (ClrMethod met in properties)
            {
                string prop = met.Name.Substring(4);
                bool isFirst = propstr.IndexOf(prop) == -1;
                if (isFirst)
                {
                    if (propCount > 0)
                    {
                        WriteLine("\t{0}", "}"); // close previous
                    }
                    Write("\t");
                    if(met.Name.StartsWith("set_"))
                    {
                        Write("{0} ", met.GetFullSignature().Split('(')[1].Split(')')[0]);
                    } else
                    {
                        Write("/* property * / ");
                    }
                    WriteLine("{0}", prop.Split('(')[0]);
                    WriteLine("\t{0}", "{");

                    propstr.Add(prop);
                }
                WriteLine("");
                WriteLine("\t\t// JIT MODE: {0} - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND", met.CompilationType);
                if (met.NativeCode != ulong.MaxValue)
                {
                    WriteLine("\t\t// Click for breakpoint: <cmd link=\"bp {0:%p}\">{0:%p}</link>", met.NativeCode);
                }
                else
                {
                    WriteLine("\t\t// Not JITTED");
                }
                Write("\t\t{0}", met.IsInternal ? "internal " : met.IsProtected ? "protected " : met.IsPrivate ? "private " : met.IsPublic ? "public " : "");

                WriteLine("{0} {1}", met.Name.Substring(0,3), " { } ");
                propCount++;

            }
            if(propCount > 0)
                WriteLine("\t{0}", "}"); // close previous
            WriteLine("");
            WriteLine("\t//");
            WriteLine("\t// Methods");
            WriteLine("\t//");
            WriteLine("");

            foreach (var method in type.Methods)
            {

                if (!(method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
                {
                    WriteLine("");
                    WriteLine("\t// JIT MODE: {0} - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND", method.CompilationType);
                    if (method.NativeCode != ulong.MaxValue)
                    {
                        WriteLine("\t// Click for breakpoint: <cmd link=\"bp {0:%p}\">{0:%p}</link>", method.NativeCode);
                    }
                    else
                    {
                        WriteLine("\t// Not JITTED");
                    }
                    Write("\t");

                    Write("{0}", method.IsInternal ? "internal " : method.IsProtected ? "protected " : method.IsPrivate ? "private " : method.IsPublic ? "public " : "");
                    Write("{0}", method.IsVirtual ? "virtual " : "");
                    Write("{0}", method.IsStatic ? "static " : "");
                    //Write("{0} ", method.Type == null ? "object" : method.Type.Name);
                    WriteLine("{0}({1};", method.Name, method.GetFullSignature().Split('(')[method.GetFullSignature().Split('(').Length - 1]);
                }
            }

            WriteLine("{0}", " }");
            WriteLine("{0}", "}");



                       
            
             
        }

        private void button4_Click(object sender, EventArgs e)
        {
            currObj = Convert.ToUInt64(textBox4.Text, 16);
            button3_Click(sender, e);
        }

        public bool ShowProgress(uint Total)
        {
           WriteLine("{0}", Total);
           return true;
        }

        public void CreateCache()
        {
            StartRuntime();
            if (cache == null)
                cache = new HeapCache(m_runtime, ShowProgress);
            DateTime n = DateTime.Now;
            cache.EnsureCache();
            WriteLine("==========");
            WriteLine("It took {0}", (DateTime.Now - n).ToString());
            WriteLine("==========");

        }

        private void button5_Click(object sender, EventArgs e)
        {
            CreateCache();
            string enumType;
            if (String.IsNullOrWhiteSpace(textBox2.Text))
                enumType = "*.HttpContext";
            else
                enumType = textBox2.Text.Contains('*') ? textBox2.Text : "*" + textBox2.Text + "*";
            
            foreach (var obj in cache.EnumerateObjectsOfType(enumType))
            {
                WriteLine("{0:x16} {1}", obj, m_heap.GetObjectType(obj).Name);
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            StartRuntime();
            ulong mt = Convert.ToUInt64(textBox5.Text, 16);
            ClrType myType = AdHoc.GetTypeFromMT(m_runtime, mt);
            WriteLine("MT: {1:x16} Type: {0}", myType == null ? "*Invalid*" : myType.Name,mt);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            StartRuntime();
            ulong eeClass = Convert.ToUInt64(textBox6.Text, 16);
            ClrType myType = AdHoc.GetTypeFromEE(m_runtime, eeClass);
            WriteLine("EEClass: {1:x16} Type: {0}", myType == null ? "*Invalid*" : myType.Name, eeClass);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            StartRuntime();
            
            foreach (var memSeg in m_heap.Segments)
            {

                Write("Start:{0:x16} End: {1:x16} Size: {2:#,#}", memSeg.Start,
                    memSeg.End, memSeg.Length);
                if (memSeg.IsLarge)
                {
                    WriteLine(" - (Large Segment)");
                }
                else
                {
                    WriteLine("");
                    if(memSeg.Gen0Length > 0)
                        WriteLine("Gen 0 - Start: {0:x16} End: {1:x16} Size: {2:#,#}",
                        memSeg.Gen0Start, memSeg.Gen0Start + memSeg.Gen0Length,
                        memSeg.Gen0Length);
                    if(memSeg.Gen1Length > 0)
                        WriteLine("Gen 1 - Start: {0:x16} End: {1:x16} Size: {2:#,#}",
                        memSeg.Gen1Start, memSeg.Gen1Start + memSeg.Gen1Length,
                        memSeg.Gen1Length);
                    if (memSeg.Gen2Length > 0)
                        WriteLine("Gen 2 - Start: {0:x16} End: {1:x16} Size: {2:#,#}",
                        memSeg.Gen2Start, memSeg.Gen2Start + memSeg.Gen2Length,
                        memSeg.Gen2Length);

                }

            }
        }


        private void button9_Click(object sender, EventArgs e)
        {
            string uri = "http://netext.codeplex.com/";
            WebClient client = new WebClient();
            string text = client.DownloadString(uri);
            //string adj = text.Replace("<br>", Environment.NewLine).Replace("{", "(").Replace("}", ")").Replace("UPDATE", "LATEST VERSION: 2.0.0.3");
            Regex reg = new Regex(@"VERSION:\s+(\d+)\.(\d+)\.(\d+)\.(\d+)");
            Match match = reg.Match(text);

            Version now = new Version(2, 0, 0, 2);
            Version codeplex = new Version(0,0,0,0);;
            if (match.Groups.Count == 5)
            {
                try
                {
                codeplex = new Version(Int32.Parse(match.Groups[1].Value),
                    Int32.Parse(match.Groups[2].Value),
                    Int32.Parse(match.Groups[3].Value),
                    Int32.Parse(match.Groups[4].Value));
                } catch
                {
                    codeplex = new Version(0,0,0,0);
                }
            }

            if(codeplex > now)
                WriteLine("There is a new version ({0}) at http://netext.codeplex.com",
                    codeplex.ToString());

            Process.Start("http://netext.codeplex.com");

        }

        private void DumpHandles(int GroupOnly = 0,
            string filterByType = null,
            string filterByObjType = null)
        {
            Dictionary<string, int> categories = new Dictionary<string,int>();
            if (m_runtime.PointerSize == 8)
            {
                WriteLine("Handle           Object           Refs Type            Object Type");
            }
            else
            {
                WriteLine("Handle   Object   Refs Type            Object Type");
            }

            int i = 0;
            int c = 0;
            foreach (var handle in m_runtime.EnumerateHandles())
            {

                if (!categories.ContainsKey(handle.HandleType.ToString()))
                {
                    categories[handle.HandleType.ToString()] = 0;
                }
                categories[handle.HandleType.ToString()]++;
                ClrType obj = m_runtime.GetHeap().GetObjectType(handle.Object);

                if (
                    (String.IsNullOrEmpty(filterByType) || handle.HandleType.ToString().ToLowerInvariant().Contains(filterByType.ToLowerInvariant()))
                    &&
                    (String.IsNullOrEmpty(filterByObjType) || obj.Name.ToLowerInvariant().Contains(filterByObjType.ToLowerInvariant()))
                    )
                {
                    if (GroupOnly == 0)
                    {
                        Write("{0:%p} {1:%p}", handle.Address, handle.Object);
                        Write(" {0,4} {1,-15}", handle.RefCount, handle.HandleType);
                        Write(" {0}", obj.Name);
                        WriteLine("");
                    }
                    c++;
                }
                i++;
            }
            WriteLine("");
            WriteLine("{0,8:#,#} Objects Listed or met the criteria", c);
            if(c != i)
                WriteLine("{0,8:#,#} Objects Skipped by the filter(s)", i-c);
            WriteLine("");
            WriteLine("{0,8:#,#} Handle(s) found in {1} categories", i, categories.Keys.Count);
            foreach (var cat in categories.Keys)
            {
                Write("{0,8:#,#} ",categories[cat]);
                Write("<link cmd=\"!wcghandle -handletype {0}\">{0}</link>", cat);
                WriteLine(" found");
            }
            WriteLine("");
        }

        private void button10_Click(object sender, EventArgs e)
        {
            StartRuntime();
            DumpHandles(checkBox1.Checked ? 1 : 0, textBox7.Text, textBox8.Text);
        }

        public static void AddIfTrue(ref StringBuilder Sb, bool IsTrue, string StrToAdd)
        {
            if (!IsTrue) return;
            if(Sb.Length > 0) Sb.Append('|');
            Sb.Append(StrToAdd);
                
        }

        public void DumpThreads()
        {

            if(m_runtime.PointerSize == 8)
                WriteLine("   Id OSId Address          Domain           Allocation Start:End              COM  GC Type  Locks Type / Status             Last Exception");
            else
                WriteLine("   Id OSId Address  Domain   Alloc Start:End   COM  GC Type  Locks Type / Status             Last Exception");              

            foreach (var thread in m_runtime.Threads)
            {
                StringBuilder sb = new StringBuilder();
                ulong AllocStart;
                ulong AllocEnd;
                AdHoc.GetThreadAllocationLimits(m_runtime, thread.Address, out AllocStart, out AllocEnd);
                Write("{0,5}", thread.ManagedThreadId);
                if (thread.OSThreadId != 0)
                    Write(" {0:x4}", thread.OSThreadId);
                else
                    Write(" ----");
                Write(" {0:%p} {1:%p} {2:%p}:{3:%p}", thread.Address, thread.AppDomain,
                    AllocStart, AllocEnd);
                Write(" {0}", thread.IsSTA ? "STA " : thread.IsMTA ? "MTA " : "NONE");
                Write(" {0,-11}", thread.GcMode.ToString());
                Write(" {0,2}", thread.LockCount > 9 ? "9+" : thread.LockCount.ToString());

                AddIfTrue(ref sb, thread.IsAbortRequested, "Aborting");
                AddIfTrue(ref sb, thread.IsBackground, "Background");
                AddIfTrue(ref sb, thread.IsDebuggerHelper, "Debugger");
                AddIfTrue(ref sb, thread.IsFinalizer, "Finalizer");
                AddIfTrue(ref sb, thread.IsGC, "GC");
                AddIfTrue(ref sb, thread.IsShutdownHelper, "Shutting down Runtime");
                AddIfTrue(ref sb, thread.IsSuspendingEE, "EESuspend");
                AddIfTrue(ref sb, thread.IsThreadpoolCompletionPort, "IOCPort");
                AddIfTrue(ref sb, thread.IsThreadpoolGate, "Gate");
                AddIfTrue(ref sb, thread.IsThreadpoolTimer, "Timer");
                AddIfTrue(ref sb, thread.IsThreadpoolWait, "Wait");
                AddIfTrue(ref sb, thread.IsThreadpoolWorker, "Worker");
                AddIfTrue(ref sb, thread.IsUnstarted, "NotStarted");
                AddIfTrue(ref sb, thread.IsUserSuspended, "Thread.Suspend()");
                AddIfTrue(ref sb, thread.IsDebugSuspended, "DbgSuspended");
                AddIfTrue(ref sb, !thread.IsAlive, "Terminated");
                AddIfTrue(ref sb, thread.IsGCSuspendPending, "PendingGC");
                Write(" {0,-25}", sb.ToString());
                if (thread.CurrentException != null)
                {
                    Write(" {0}", thread.CurrentException.Type.Name);
                }


                if(thread.IsAbortRequested) sb.Append("Aborting");
                if (thread.IsBackground)
                {
                    if (sb.Length > 0) sb.Append("|");
                    sb.Append("Background");
                }
                WriteLine("");

            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            StartRuntime();
            DumpThreads();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            CreateCache();
            IMDObjectEnum objEnum = new MDObjectEnum();
            foreach (var exc in cache.GetExceptions())
            {
                foreach(var excAddr in exc.Addresses)
                {
                    objEnum.AddAddress(excAddr);
                }
            }
            DumpAllExceptions(objEnum);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if(currObj > 0)
                DumpClass(ReadPointer(currObj));
        }
    }
}
