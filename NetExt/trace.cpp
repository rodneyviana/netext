#include "NetExt.h"
#include "EventCallBacks.h"


HRESULT TraceCallBack(IDebugBreakpoint* BP, std::string Tag)
{
#if _DEBUG
	CLRDATA_ADDRESS IP=0;
	BP->GetOffset(&IP);
	g_ExtInstancePtr->Out("Reached BP: [%p]\n",Tag.c_str());
#endif

	//if(IP)
	//	pTarget->OpenSource(IP);
	return DEBUG_STATUS_BREAK;
}



EXT_COMMAND(wp,
	"Step-out managed code (like F10 in Visual Studio)",
	"")
{
	DO_INIT_API;
	CLRDATA_ADDRESS ip = 0;
	m_Registers2->GetInstructionOffset(&ip);
//#ifndef _X86_
//	ip = EvalExprU64("@rip");
//#else
//	ip = EvalExprU64("@eip");	
//#endif
	CComPtr<NetExtShim::IMDSourceMapEnum> srcMapEnum;

	HRESULT hr = pTarget->GetLineRange(ip, &srcMapEnum);

	if(SUCCEEDED(hr))
	{
		std::vector<CLRDATA_ADDRESS> addresses;
		CLRDATA_ADDRESS start, end = 0;
		srcMapEnum->GetRange(&start, &end);
#if _DEBUG
		Out("IP: %p Start: %p End: %p\n", ip, start, end);
		if(end >= ip)
		{
			Out("End >= IP\n");
		}
#endif
		NetExtShim::MD_SourceMap srcMap;
		int i=0;
		EventCallBacks::DeleteInstance();
		while (SUCCEEDED(srcMapEnum->Next(&srcMap)))
		{
			if(i++>300)
				break; // avoid infinite loop
			
			if(srcMap.IsJump && srcMap.PointTo > end)
			{
				EventCallBacks::GetInstance()->AddBreakPoint(srcMap.PointTo, "Jump", TraceCallBack);
			}

			//if(srcMap.IsCall && srcMap.IsManaged)
			//{
			//	EventCallBacks::GetInstance()->AddBreakPoint(srcMap.PointTo, "Call", TraceCallBack);
			//}
			
		}
		EventCallBacks::GetInstance()->AddBreakPoint(end+1, "Edge", TraceCallBack);
		m_Control4->SetExecutionStatus(DEBUG_STATUS_GO);
		hr = m_Control4->WaitForEvent(0, INFINITE);


		if(hr != S_OK)
		{
			Out("Error: 0x%x\n", hr);
			return;
		}
		EventCallBacks::DeleteInstance();

		m_Registers2->GetInstructionOffset(&ip);
//#ifndef _X86_
//		ip = EvalExprU64("@rip");
//#else
//		ip = EvalExprU64("@eip");	
//#endif
		pTarget->OpenSource(ip);
	}


}

EXT_COMMAND(wt,
	"Step-into managed code (like F11 in Visual Studio)",
	"")
{
	DO_INIT_API;
	CLRDATA_ADDRESS ip = 0;
	m_Registers2->GetInstructionOffset(&ip);
//#ifndef _X86_
//	ip = EvalExprU64("@rip");
//#else
//	ip = EvalExprU64("@eip");	
//#endif
	CComPtr<NetExtShim::IMDSourceMapEnum> srcMapEnum;

	HRESULT hr = pTarget->GetLineRange(ip, &srcMapEnum);

	if(SUCCEEDED(hr))
	{
		std::vector<CLRDATA_ADDRESS> addresses;
		CLRDATA_ADDRESS start, end = 0;
		srcMapEnum->GetRange(&start, &end);
		NetExtShim::MD_SourceMap srcMap;
		int i=0;
		while (SUCCEEDED(srcMapEnum->Next(&srcMap)))
		{
			if(i++>300)
				break; // avoid infinite loop
			
			if(srcMap.IsJump && srcMap.PointTo > end)
			{
				EventCallBacks::GetInstance()->AddBreakPoint(srcMap.PointTo, "Jump", TraceCallBack);
			}

			if(srcMap.IsCall && srcMap.IsManaged)
			{
				EventCallBacks::GetInstance()->AddBreakPoint(srcMap.PointTo, "Call", TraceCallBack);
			}
		}
		EventCallBacks::GetInstance()->AddBreakPoint(end+1, "Edge", TraceCallBack);
		m_Control4->SetExecutionStatus(DEBUG_STATUS_GO);
		m_Control4->WaitForEvent(0, INFINITE);
		EventCallBacks::GetInstance()->DeleteInstance();
		m_Registers2->GetInstructionOffset(&ip);
//#ifndef _X86_
//	ip = EvalExprU64("@rip");
//#else
//	ip = EvalExprU64("@eip");	
//#endif
		pTarget->OpenSource(ip);

	}



}