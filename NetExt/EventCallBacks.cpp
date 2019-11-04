#include "EventCallBacks.h"


#pragma region BreakPointClass

BreakpointClass::BreakpointClass(std::string BPExpression, std::string Tag, BPCallBack Method, CLRDATA_ADDRESS BPOffset)
{
	this->tag = Tag;
	this->method = Method;
	hr = g_ExtInstancePtr->m_Control4->AddBreakpoint(DEBUG_BREAKPOINT_CODE, DEBUG_ANY_ID, &breakPoint);
	if(IsValid())
	{
		ULONG flags = 0;
		breakPoint->GetFlags(&flags);
		if((flags & DEBUG_BREAKPOINT_DEFERRED) == 0)
		{
			breakPoint->AddFlags(DEBUG_BREAKPOINT_ADDER_ONLY | DEBUG_BREAKPOINT_ENABLED);
			if(BPOffset)
			{
				hr = breakPoint->SetOffset(BPOffset);
				isOffset = true;
			} else
			{
				isOffset = false;
				hr = breakPoint->SetOffsetExpression(BPExpression.c_str());
			}
		}
		else
		{
			RemoveBreakPoint();
			hr = E_NOT_SET;
		}
	} else
	{
		hr = E_NOT_SET;
	}
}


void BreakpointClass::RemoveBreakPoint()
{
	if(!IsValid())
		return;
	if(g_ExtInstancePtr->m_Control4.IsSet())
	{
		if(breakPoint)
		{
			hr = g_ExtInstancePtr->m_Control4->RemoveBreakpoint(breakPoint);
			
			hr = E_NOT_SET;
		}
	}
}

HRESULT BreakpointClass::InvokeCallBack()
{
	if(NULL == method)
	{
		//this->RemoveBreakPoint();
		return DEBUG_STATUS_IGNORE_EVENT;
	}
	return method(this->breakPoint, this->tag);
}

#pragma endregion

#pragma region EventCallBackClass

CComPtr<EventCallBacks> EventCallBacks::singleTon;

EventCallBacks::EventCallBacks(void)
{
	g_ExtInstancePtr->m_Client4->SetEventCallbacks(this);
}


EventCallBacks::~EventCallBacks(void)
{

}

STDMETHODIMP_(ULONG)
EventCallBacks::AddRef(
    THIS
    )
{
    // This class is designed to be static so
    // there's no true refcount.
    return 1;
}

STDMETHODIMP_(ULONG)
EventCallBacks::Release(
    THIS
    )
{
    // This class is designed to be static so
    // there's no true refcount.
    return 0;
}

STDMETHODIMP
EventCallBacks::GetInterestMask(
    THIS_
    _Out_ PULONG Mask
    )
{
    *Mask =
        DEBUG_EVENT_BREAKPOINT;
		/*|
        DEBUG_EVENT_EXCEPTION |
        DEBUG_EVENT_CREATE_PROCESS |
        DEBUG_EVENT_LOAD_MODULE |
        DEBUG_EVENT_SESSION_STATUS; */
    return S_OK;
}

STDMETHODIMP
EventCallBacks::Breakpoint(
    THIS_
    _In_ PDEBUG_BREAKPOINT Bp
    )
{
	CLRDATA_ADDRESS offset = 0;
	//
	// Let's first try the Offset
	//
	if(SUCCEEDED(Bp->GetOffset(&offset)))
	{
		std::string Expression(formathex(offset));
#if _DEBUG
		g_ExtInstancePtr->Out("Offset: %s\n", Expression.c_str());
#endif
		if(bps.find(Expression) != bps.end())
		{
			return bps[Expression].InvokeCallBack();
		}
	}
	std::string Expression(MAX_MTNAME, ' ');

	ULONG size = 0;

	

	HRESULT hr = Bp->GetOffsetExpression(const_cast<char*>(Expression.c_str()),MAX_MTNAME, &size);

#if _DEBUG
	g_ExtInstancePtr->Out("Expression: %s\n", Expression.c_str());
#endif
	if(hr == S_OK && size > 0)
	{
		Expression.resize(size - 1);
		if(bps.find(Expression) != bps.end())
		{
			return bps[Expression].InvokeCallBack();
		}
	}
#if _DEBUG
	g_ExtInstancePtr->Out("BreakPoint could not be found\n");
#endif
	return DEBUG_STATUS_NO_CHANGE;
}

STDMETHODIMP
EventCallBacks::Exception(
    THIS_
    _In_ PEXCEPTION_RECORD64 Exception,
    _In_ ULONG FirstChance
    )
{
	/*
    // We want to handle these exceptions on the first
    // chance to make it look like no exception ever
    // happened.  Handling them on the second chance would
    // allow an exception handler somewhere in the app
    // to be hit on the first chance.
    if (!FirstChance)
    {
        return DEBUG_STATUS_NO_CHANGE;
    }

    //
    // Check and see if the instruction causing the exception
    // is a cli or sti.  These are not allowed in user-mode
    // programs on NT so if they're present just nop them.
    //

    // sti/cli will generate privileged instruction faults.
    if (Exception->ExceptionCode != STATUS_PRIVILEGED_INSTRUCTION)
    {
        return DEBUG_STATUS_NO_CHANGE;
    }

    UCHAR Instr;
    ULONG Done;
    
    // It's a privileged instruction, so check the code for sti/cli.
    if (g_Data->ReadVirtual(Exception->ExceptionAddress, &Instr,
                            sizeof(Instr), &Done) != S_OK ||
        Done != sizeof(Instr) ||
        (Instr != 0xfb && Instr != 0xfa))
    {
        return DEBUG_STATUS_NO_CHANGE;
    }

    // It's a sti/cli, so nop it out and continue.
    Instr = 0x90;
    if (g_Data->WriteVirtual(Exception->ExceptionAddress, &Instr,
                             sizeof(Instr), &Done) != S_OK ||
        Done != sizeof(Instr))
    {
        return DEBUG_STATUS_NO_CHANGE;
    }

    // Fixed.
    if (g_Verbose)
    {
        Print("Removed sti/cli at %I64x\n", Exception->ExceptionAddress);
    }
    */
    return DEBUG_STATUS_GO_HANDLED;
	
}

STDMETHODIMP
EventCallBacks::CreateProcess(
    THIS_
    _In_ ULONG64 ImageFileHandle,
    _In_ ULONG64 Handle,
    _In_ ULONG64 BaseOffset,
    _In_ ULONG ModuleSize,
    _In_ PCSTR ModuleName,
    _In_ PCSTR ImageName,
    _In_ ULONG CheckSum,
    _In_ ULONG TimeDateStamp,
    _In_ ULONG64 InitialThreadHandle,
    _In_ ULONG64 ThreadDataOffset,
    _In_ ULONG64 StartOffset
    )
{
	    
    UNREFERENCED_PARAMETER(ImageFileHandle);
    UNREFERENCED_PARAMETER(Handle);
    UNREFERENCED_PARAMETER(ModuleSize);
    UNREFERENCED_PARAMETER(ModuleName);
    UNREFERENCED_PARAMETER(CheckSum);
    UNREFERENCED_PARAMETER(TimeDateStamp);
    UNREFERENCED_PARAMETER(InitialThreadHandle);
    UNREFERENCED_PARAMETER(ThreadDataOffset);
    UNREFERENCED_PARAMETER(StartOffset);
	/*
    // The process is now available for manipulation.
    // Perform any initial code patches on the executable.
    ApplyExePatches(ImageName, BaseOffset);

    // If the user requested that version calls be fixed up
    // register breakpoints to do so.
    if (g_NeedVersionBps)
    {
        AddVersionBps();
    }
    */
    return DEBUG_STATUS_GO;
	
}

STDMETHODIMP
EventCallBacks::LoadModule(
    THIS_
    _In_ ULONG64 ImageFileHandle,
    _In_ ULONG64 BaseOffset,
    _In_ ULONG ModuleSize,
    _In_ PCSTR ModuleName,
    _In_ PCSTR ImageName,
    _In_ ULONG CheckSum,
    _In_ ULONG TimeDateStamp
    )
{
	
    UNREFERENCED_PARAMETER(ImageFileHandle);
    UNREFERENCED_PARAMETER(ModuleSize);
    UNREFERENCED_PARAMETER(ModuleName);
    UNREFERENCED_PARAMETER(CheckSum);
    UNREFERENCED_PARAMETER(TimeDateStamp);
    /*
    ApplyDllPatches(ImageName, BaseOffset);
	*/
    return DEBUG_STATUS_GO;
}

STDMETHODIMP
EventCallBacks::SessionStatus(
    THIS_
    _In_ ULONG SessionStatus
    )
{
	/*
    // A session isn't fully active until WaitForEvent
    // has been called and has processed the initial
    // debug events.  We need to wait for activation
    // before we query information about the session
    // as not all information is available until the
    // session is fully active.  We could put these
    // queries into CreateProcess as that happens
    // early and when the session is fully active, but
    // for example purposes we'll wait for an
    // active SessionStatus callback.
    // In non-callback applications this work can just
    // be done after the first successful WaitForEvent.
    if (SessionStatus != DEBUG_SESSION_ACTIVE)
    {
        return S_OK;
    }
    
    HRESULT Status;
    
    //
    // Find the register index for eax as we'll need
    // to access eax.
    //

    if ((Status = g_Registers->GetIndexByName("eax", &g_EaxIndex)) != S_OK)
    {
        Exit(1, "GetIndexByName failed, 0x%X\n", Status);
    }
	*/
    return S_OK;
	
}

#pragma endregion