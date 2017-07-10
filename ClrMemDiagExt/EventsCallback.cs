using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetExt.Shim
{
    public class EventsCallback : IDebugEventCallbacks
    {
        public int Breakpoint(IDebugBreakpoint Bp)
        {

            StringBuilder str = new StringBuilder(100);
            uint size = 0;
            bool run = false;
            Bp.GetOffsetExpression(str, 100, out size);

            foreach (var keyvalue in targets)
            {
                if (keyvalue.Key.ToLower() == str.ToString().ToLower())
                {
                    run = keyvalue.Value.Invoke(Bp, str.ToString());
                }
            }
            if (run)
            {
                return (int)DEBUG_STATUS.GO; // (int)DEBUG_STATUS.REVERSE_GO (backwards)

            }
            return (int)DEBUG_STATUS.BREAK;

        }

        public int ChangeDebuggeeState(DEBUG_CDS Flags, ulong Argument)
        {
            DebugApi.DebugWriteLine("Change Debugee Status: {0} - {1:x}", Flags, Argument);
            return 0; // BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int ChangeEngineState(DEBUG_CES Flags, ulong Argument)
        {
            DebugApi.DebugWriteLine("Change Enginee State: {0} - {1:x}", Flags, Argument);
            return 0; //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int ChangeSymbolState(DEBUG_CSS Flags, ulong Argument)
        {
            DebugApi.DebugWriteLine("Change Symbol State: {0} - {1:x}", Flags, Argument);
            return 0; //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int CreateProcess(ulong ImageFileHandle, ulong Handle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp, ulong InitialThreadHandle, ulong ThreadDataOffset, ulong StartOffset)
        {
            DebugApi.DebugWriteLine("Create Process");
            return BitConverter.ToInt32(BitConverter.GetBytes((uint)DEBUG_STATUS.IGNORE_EVENT), 0); //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int CreateThread(ulong Handle, ulong DataOffset, ulong StartOffset)
        {
            DebugApi.DebugWriteLine("Create Thread {0:x}", Handle);
            return BitConverter.ToInt32(BitConverter.GetBytes((uint)DEBUG_STATUS.IGNORE_EVENT), 0); // BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int Exception(ref EXCEPTION_RECORD64 Exception, uint FirstChance)
        {
            DebugApi.DebugWriteLine("Exception : 0x{0:x8} - First: {1}", Exception.ExceptionCode, FirstChance != 0);
            return BitConverter.ToInt32(BitConverter.GetBytes((uint)DEBUG_STATUS.IGNORE_EVENT), 0);
        }

        public int ExitProcess(uint ExitCode)
        {
            DebugApi.DebugWriteLine("Exit Process: 0x{0:x8}", ExitCode);
            return BitConverter.ToInt32(BitConverter.GetBytes((uint)DEBUG_STATUS.IGNORE_EVENT), 0); ; //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int ExitThread(uint ExitCode)
        {
            DebugApi.DebugWriteLine("Exit Thread: 0x{0:x8}", ExitCode);
            return BitConverter.ToInt32(BitConverter.GetBytes((uint)DEBUG_STATUS.IGNORE_EVENT), 0);  //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int GetInterestMask(out DEBUG_EVENT Mask)
        {
            Mask = DEBUG_EVENT.BREAKPOINT;
#if DEBUG
            Mask = (DEBUG_EVENT)uint.MaxValue;
#endif

            return (int)HRESULT.S_OK;
        }

        public int LoadModule(ulong ImageFileHandle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp)
        {
            DebugApi.DebugWriteLine("Load Module: {0}", ModuleName);
            return BitConverter.ToInt32(BitConverter.GetBytes((uint)DEBUG_STATUS.IGNORE_EVENT), 0);  //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int SessionStatus(DEBUG_SESSION Status)
        {
            DebugApi.DebugWriteLine("Session Status: {0}", Status);
            return 0; //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int SystemError(uint Error, uint Level)
        {
            DebugApi.DebugWriteLine("System Error: 0x{0:x8}", Error);
            return BitConverter.ToInt32(BitConverter.GetBytes((uint)DEBUG_STATUS.IGNORE_EVENT), 0);  //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }

        public int UnloadModule(string ImageBaseName, ulong BaseOffset)
        {
            DebugApi.DebugWriteLine("Unload Module");
            return BitConverter.ToInt32(BitConverter.GetBytes((uint)DEBUG_STATUS.IGNORE_EVENT), 0); //  BitConverter.ToInt32(BitConverter.GetBytes((uint)HRESULT.E_NOTIMPL),0);
        }



        Dictionary<string, BreakPointCallBack> targets = new Dictionary<string, BreakPointCallBack>();

        public void AddEvent(string Expression, BreakPointCallBack Callback)
        {
            targets[Expression] = Callback;
        }

    }

}
