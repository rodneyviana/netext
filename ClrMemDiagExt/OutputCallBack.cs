using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetExt.Shim
{
    public class OutputCallBack : IDebugOutputCallbacks
    {

        StringBuilder text;
        StringBuilder error;

        public OutputCallBack()
        {
            text = new StringBuilder();
            error = new StringBuilder();
        }

        public string Execute(IDebugClient Client, IDebugControl Control, string Command)
        {
            try
            {
                Clear();
                Client.SetOutputMask(SupportedMasks);
                Client.SetOutputCallbacks(this);
                //Application.DoEvents();
                //DEBUG_STATUS status;
                //Control.GetExecutionStatus(out status);
                //DebugApi.WriteLine("Execution status {0}", status.ToString());
                Control.Execute(DEBUG_OUTCTL.THIS_CLIENT |
                    DEBUG_OUTCTL.NOT_LOGGED, Command, DEBUG_EXECUTE.NOT_LOGGED |
                    DEBUG_EXECUTE.NO_REPEAT);
                //int res = 0;
                //do
                //{
                //    res = Control.WaitForEvent(DEBUG_WAIT.DEFAULT, 10000);
                //    if(res == 1) WinApi.SetStatusBarMessage("10 seconds elapsed");
                //    //DebugApi.DebugWriteLine("10 seconds elapsed");
                //} while (res == 1  /* ResultCom.S_FALSE */);
            }
            finally
            {
                Client.SetOutputCallbacks(null);
            }
            return Text;
        }
        public static DEBUG_OUTPUT SupportedMasks
        {
            get
            {
                return DEBUG_OUTPUT.NORMAL | DEBUG_OUTPUT.ERROR | DEBUG_OUTPUT.DEBUGGEE | DEBUG_OUTPUT.VERBOSE | DEBUG_OUTPUT.EXTENSION_WARNING;
            }
        }
        public void Clear()
        {
            text.Clear();
            error.Clear();
        }

        public string Text
        {
            get
            {
                return text.ToString();
            }
        }

        public string Error
        {
            get
            {
                return error.ToString();
            }
        }
        public int Output(DEBUG_OUTPUT Mask, string Text)
        {
            if (Mask.HasFlag(DEBUG_OUTPUT.NORMAL))
            {
                text.Append(Text);
            }

            if (Mask.HasFlag(DEBUG_OUTPUT.ERROR))
            {
                error.Append(Text);
            }

            return 0; // S_OK
        }

    }

    public class OutputMaskBits
    {
        // Output mask bits.
        // Normal output.
        public const uint DEBUG_OUTPUT_NORMAL = 0x00000001;
        // Error output.
        public const uint DEBUG_OUTPUT_ERROR = 0x00000002;
        // Warnings.
        public const uint DEBUG_OUTPUT_WARNING = 0x00000004;
        // Additional output.
        public const uint DEBUG_OUTPUT_VERBOSE = 0x00000008;
        // Prompt output.
        public const uint DEBUG_OUTPUT_PROMPT = 0x00000010;
        // Register dump before prompt.
        public const uint DEBUG_OUTPUT_PROMPT_REGISTERS = 0x00000020;
        // Warnings specific to extension operation.
        public const uint DEBUG_OUTPUT_EXTENSION_WARNING = 0x00000040;
        // Debuggee debug output, such as from OutputDebugString.
        public const uint DEBUG_OUTPUT_DEBUGGEE = 0x00000080;
        // Debuggee-generated prompt, such as from DbgPrompt.
        public const uint DEBUG_OUTPUT_DEBUGGEE_PROMPT = 0x00000100;
        // Symbol messages, such as for !sym noisy.
        public const uint DEBUG_OUTPUT_SYMBOLS = 0x00000200;
        // Output which modifies the status bar
        public const uint DEBUG_OUTPUT_STATUS = 0x00000400;

    }

}
