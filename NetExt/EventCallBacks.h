#pragma once

#include <memory>
#include "CLrHelper.h"


typedef HRESULT (*BPCallBack)(IDebugBreakpoint* BP, std::string Tag);

struct BreakpointClass
{
public:

	BreakpointClass() {};
	BreakpointClass(std::string BPExpression, std::string Tag, BPCallBack Method, CLRDATA_ADDRESS BPOffset=0);


	HRESULT InvokeCallBack();
	void RemoveBreakPoint();
	bool IsValid()
	{
		return hr == S_OK;
	}

	bool IsOffset()
	{
		return isOffset;
	}

	CComPtr<IDebugBreakpoint> breakPoint;
protected:
	bool isOffset;
	std::string tag;
	BPCallBack method;
	HRESULT hr;
};


class EventCallBacks :
	public DebugBaseEventCallbacks
{
public:
	EventCallBacks(void);
	~EventCallBacks(void);
	static EventCallBacks* GetInstance()
	{
		if(NULL != EventCallBacks::singleTon.p)
		{
#if _DEBUG
		g_ExtInstancePtr->Out("[CREATE] EventCallBack is Not NULL\n");
#endif
			return singleTon;
		}
#if _DEBUG
		g_ExtInstancePtr->Out("[CREATE] EventCallBack is NULL\n");
#endif

		singleTon = CComPtr<EventCallBacks>(new EventCallBacks());
		return singleTon;
	}

	static void DeleteInstance()
	{
		if(NULL == EventCallBacks::singleTon.p)
			return;
#if _DEBUG
		g_ExtInstancePtr->Out("[DELETE] EventCallBack is Not NULL\n");
#endif

		if(g_ExtInstancePtr->m_Control4.IsSet())
		{
			for(auto i=GetInstance()->bps.begin();i!=GetInstance()->bps.end();i++)
			{
#if _DEBUG
		CLRDATA_ADDRESS IP = 0;
		i->second.breakPoint->GetOffset(&IP);
		g_ExtInstancePtr->Out("Deleting BP: %p IsValid: %s\n",IP, i->second.IsValid() ? "true" : "false");
#endif
				
				i->second.RemoveBreakPoint();
			}
		}
		GetInstance()->bps.clear();
		PDEBUG_EVENT_CALLBACKS callBack = NULL;
		if(g_ExtInstancePtr->m_Client4.IsSet())
		{
			g_ExtInstancePtr->m_Client4->GetEventCallbacks(&callBack);
			if(NULL != callBack)
			{
				g_ExtInstancePtr->m_Client4->SetEventCallbacks(NULL);

			}
		}
		singleTon.Release();
		
	}

	int AddBreakPoint(CLRDATA_ADDRESS BPExpr, std::string Tag, BPCallBack Method)
	{
#if _DEBUG
		g_ExtInstancePtr->Out("Add BP: %s ",Tag.c_str());
		g_ExtInstancePtr->Out("[%p]\n",BPExpr);
#endif
		std::string BPStr = formathex(BPExpr);
		if(bps.find(BPStr) == bps.end())
		{
			bps.emplace(BPStr, BreakpointClass("", Tag, Method, BPExpr));
			if(bps.find(BPStr)->second.IsValid())
				return static_cast<int>(bps.size());
			bps.erase(BPStr);
		}
#if _DEBUG
		g_ExtInstancePtr->Out("*** Adding BP %p failed\n", BPExpr);
#endif

		
		return -1;
	}



    // IUnknown.
    STDMETHOD_(ULONG, AddRef)(
        THIS
        );
    STDMETHOD_(ULONG, Release)(
        THIS
        );

    // IDebugEventCallbacks.
    STDMETHOD(GetInterestMask)(
        THIS_
        _Out_ PULONG Mask
        );
    
    STDMETHOD(Breakpoint)(
        THIS_
        _In_ PDEBUG_BREAKPOINT Bp
        );
    STDMETHOD(Exception)(
        THIS_
        _In_ PEXCEPTION_RECORD64 Exception,
        _In_ ULONG FirstChance
        );
    STDMETHOD(CreateProcess)(
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
        );
    STDMETHOD(LoadModule)(
        THIS_
        _In_ ULONG64 ImageFileHandle,
        _In_ ULONG64 BaseOffset,
        _In_ ULONG ModuleSize,
        _In_ PCSTR ModuleName,
        _In_ PCSTR ImageName,
        _In_ ULONG CheckSum,
        _In_ ULONG TimeDateStamp
        );
    STDMETHOD(SessionStatus)(
        THIS_
        _In_ ULONG Status
        );
protected:
	std::map<std::string, BreakpointClass> bps;
	//std::map<std::string, BreakpointClass>::iterator it;
	static CComPtr<EventCallBacks> singleTon;

};



