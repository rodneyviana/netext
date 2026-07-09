/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "CLRHelper.h"

#include <memory>
#include "SpecialCases.h"

typedef std::map<std::string, CLRDATA_ADDRESS> RegsMap;

const char *reg64[]= {"@rax", "@rbx", "@rcx", "@rdx", "@rdx", "@rsi",
					  "@rdi", "@r8", "@r9", "@r10", "@r11", "@r12",
					  "@r13", "@r14", "@r15" };
#define regs64c 15
#define regs32c 6

const char *reg32[]= {"@eax", "@ebx", "@ecx", "@edx", "@esi", "@edi" };

CLRDATA_ADDRESS GetRegAddr(const char* Reg)
{
	return g_ExtInstancePtr->EvalExprU64(Reg);
}

void GetRegs(RegsMap& Regs, int WordSize = sizeof(void*))
{
	Regs.clear();
	int i;
	if(WordSize == 8)
	{
		for(i=0;i<regs64c;i++)
		{
			Regs[reg64[i]]=GetRegAddr(reg64[i]);
		}
	} else
	{
		for(i=0;i<regs32c;i++)
		{
			Regs[reg32[i]]=GetRegAddr(reg32[i]);
		}
	}
}



EXT_COMMAND(wstack,
            "Dump stack objects. Use '!whelp wstack' for detailed help",
			"{{custom}}")
{
		DO_INIT_API;

		try
		{
			std::unique_ptr<StackObj> stacks(new StackObj());
			//stacks->Initialize();
			//Out("Stack Start: %p\n", stacks->stackStart);
			//Out("Stack End: %p\n", stacks->stackEnd);
			stacks->DumpStackObject();
		} catch(std::exception& e)
		{
			Out("wstack: %1\n", e.what());
		}
}

vector<ULONG> StackObj::IsInStack(CLRDATA_ADDRESS addr)
{
	// Needs to update?
	ULONG ThreadId;
	g_ExtInstancePtr->m_System3->GetCurrentThreadId(&ThreadId);
	if(StackCache.size() == 0)
	{
		Thread tp;
		tp.Request();

		Thread::const_iterator curThread = tp.begin();
		while(curThread != tp.end())
		{
			if(IsInterrupted())
			{
				g_ExtInstancePtr->m_System->SetCurrentThreadId(ThreadId);
				return vector<ULONG>();
			}
			ULONG tid;
			HRESULT status = g_ExtInstancePtr->m_System2->GetThreadIdBySystemId(curThread->OSThreadID, &tid);
			if(SUCCEEDED(status)) g_ExtInstancePtr->m_System->SetCurrentThreadId(tid);
			if(SUCCEEDED(status)) DumpStackObject(true);
			curThread++;
		}
		g_ExtInstancePtr->m_System3->SetCurrentThreadId(ThreadId);

	}
	if(StackCache.count(addr))
		return StackCache[addr];
	return vector<ULONG>();

}

void StackObj::DumpStackObject(bool CacheOnly)
{
	NT_TIB teb;
	ZeroMemory(&teb, sizeof(teb));
	ULONG64 tebAddr=0;

	HRESULT status = g_ExtInstancePtr->m_System->GetCurrentThreadTeb(&tebAddr);

	if(SUCCEEDED(status))
	{
		ExtRemoteData rteb(tebAddr, sizeof(teb));
		rteb.ReadBuffer(&teb, sizeof(teb));
		stackStart = (CLRDATA_ADDRESS)teb.StackBase;
		stackEnd = (CLRDATA_ADDRESS)teb.StackLimit;
	} else
	{
		if(!CacheOnly) g_ExtInstancePtr->Err("Unable to get stack limits\n");
		return;
	}

	ULONG tid;
	ULONG sysid;
	g_ExtInstancePtr->m_System->GetCurrentThreadId(&tid);
	g_ExtInstancePtr->m_System->GetCurrentThreadSystemId(&sysid);

	if(!CacheOnly) g_ExtInstancePtr->Out("\nListing objects from: %p to %p from thread: %u [%x]\n\n", stackEnd, stackStart, tid, sysid);
	Heap heaps;
	heaps.EnumRanges();
	std::map<CLRDATA_ADDRESS, CLRDATA_ADDRESS> uniques;
	RegsMap regs;
	GetRegs(regs);
	if(sizeof(void*)==8)
		if(!CacheOnly) g_ExtInstancePtr->Out("\n\nAddress          Method Table    Heap Gen      Size Type\n");
	else
		if(!CacheOnly) g_ExtInstancePtr->Out("\n\nAddress     Method Table Heap Gen      Size Type\n");

	RegsMap::const_iterator it=regs.begin();
	int h,g,count=0;
	UINT64 size=0;

	while(it!=regs.end())
	{
		CLRDATA_ADDRESS addr = it->second;
		if(heaps.IsInHeap(addr,&h,&g))
		{
			ObjDetail obj(addr);
			if(obj.IsValid() && h>=0 && g>=0)
			{
				string pp;
				if(!CacheOnly) g_ExtInstancePtr->Dml("<link cmd=\"!wdo %p\">%s=%p</link> %p %3i %2i %10u %S", addr, it->first.c_str(), addr, obj.MethodTable(), h, g, obj.Size(), obj.TypeName().c_str() );
				if(!CacheOnly)
				{
					pp = SpecialCases::PrettyPrint(obj.Address());
				}
				if(pp.size() > 0 && !CacheOnly) g_ExtInstancePtr->Out(" %s", pp.c_str());
				if(!CacheOnly) g_ExtInstancePtr->Out("\n");

				if(uniques.find(addr)==uniques.end())
				{
					if(CacheOnly) StackCache[addr].push_back(tid);
					uniques[addr]=NULL;
					count++;
					size+=obj.Size();
				}
			}
		}
		it++;
	}
#if _DEBUG
	//g_ExtInstancePtr->Out("Start: %p, End: %p, Size: %i\n", stackEnd, stackStart, stackStart - stackEnd);
#endif
	if(!IsValidMemory(stackEnd, stackStart - stackEnd))
	{
		g_ExtInstancePtr->Err("Stack is in an unstable situation\n");
		return;
	}

//	if(IsiDNA() && (stackStart - stackEnd) > 100*1024)
//	{
//		// cache stack to make things faster
//		ULONG size = (ULONG)(min((stackStart - stackEnd), 64*1014*1024));
//		PVOID Addr = NULL;
//		try
//		{
//			ExtRemoteData rd(stackEnd, size);
//			rd.Read();
//			int i = 0;
//			for(i=0;i<(size / sizeof(void*));i+=sizeof(void*))
//			{
//				PVOID p=(PVOID)(rd.m_Data + i);
//				if(p != NULL)
//					break;
//			}
//			stackEnd += i*sizeof(void*); 
//		} catch(...)
//		{
//#if _DEBUG
//			g_ExtInstancePtr->Out("Exception\n");
//#endif
//
//		}
	//	
	//}
	while(stackEnd <= stackStart)
	{
		if(IsInterrupted())
			return;
		CLRDATA_ADDRESS addr = ObjDetail::GetPTR(stackEnd);
		if(addr != 0 && heaps.IsInHeap(addr,&h,&g))
		{
			if(uniques.find(addr)==uniques.end())
			{
				string pp;
				if(CacheOnly) StackCache[addr].push_back(tid);

				uniques[addr]=NULL;

				ObjDetail obj(addr);
				if(obj.IsValid()  && h>=0 && g>=0)
				{
					if(!CacheOnly) g_ExtInstancePtr->Dml("<link cmd=\"!wdo %p\">%p</link> %p %3i %2i %10u %S", addr, addr, obj.MethodTable(), h, g, obj.Size(), obj.TypeName().c_str() );
				if(!CacheOnly)
				{
					pp = SpecialCases::PrettyPrint(obj.Address());
				}
				if(pp.size() > 0 && !CacheOnly) g_ExtInstancePtr->Out(" %s", pp.c_str());
					if(!CacheOnly) g_ExtInstancePtr->Out("\n");
					count++;
					size+=obj.Size();
				}
			}
		}
		stackEnd += sizeof(void*);
	}
	if(!CacheOnly) g_ExtInstancePtr->Out("\n%i unique object(s) found in %S bytes\n", count, formatnumber(size).c_str());
}
/*
//
// NOT IMPLEMENTED YET. DO NOT USE
//
std::vector<ClrStackFrame> StackObj::WalkStack(ULONG ThreadId)
{

		NT_TIB teb;
		ULONG64 tebAddr=0;

		ULONG currThread;
		g_ExtInstancePtr->m_System3->GetCurrentThreadId(&currThread);
		HRESULT status = g_ExtInstancePtr->m_System3->SetCurrentThreadId(ThreadId);
		std::vector<ClrStackFrame> tempStack;
		
		if(SUCCEEDED(status))
		{
			ExtRemoteData rteb(tebAddr, sizeof(teb));
			rteb.ReadBuffer(&teb, sizeof(teb));
			stackStart = (CLRDATA_ADDRESS)teb.StackBase;
			stackEnd = (CLRDATA_ADDRESS)teb.StackLimit;
		} else
			return std::vector<ClrStackFrame>();
		if(SUCCEEDED(clrData->GetTaskByOSThreadID(threadId, &task)))
		{
			isManaged = SUCCEEDED(task->CreateStackWalk(CLRDATA_SIMPFRAME_UNRECOGNIZED |
                                   CLRDATA_SIMPFRAME_MANAGED_METHOD |
                                   CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE |
                                   CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                                   &stackWalk));
			isManaged = true;
			task->Release();
			task=NULL;
			do
			{
				ClrStackFrame frame;
				if(IsInterrupted() )
				{
					frame.StackStr = L"*** Interrupted by user ***";
					tempStack.push_back(frame);
					break;
				}

				CLRDATA_ADDRESS ip = 0;
				CLRDATA_ADDRESS sp = 0;
				CONTEXT_CLR context;
				if ((status=stackWalk->GetContext(CONTEXT_ALL, sizeof(CONTEXT_CLR),
                                           NULL, (BYTE *)&context))!=S_OK)
				{
					frame.StackStr = L"*** Get Frame Context failed ***";
					tempStack.push_back(frame);
					break;
				}
#ifdef _WIN64
				ip = context.Rip;
				sp = context.Rsp;
#else
				ip = context.Eip;
				sp = context.Esp;
#endif
				DacpFrameData FrameData;
				if(SUCCEEDED(FrameData.Request(stackWalk)) && FrameData.frameAddr)
				{
					sp = FrameData.frameAddr;
					CLRDATA_ADDRESS addr = ObjDetail::GetPTR(sp);
					if(SUCCEEDED(DacpFrameData::GetFrameName(clrData, addr, MAX_MTNAME, NameBuffer)))
					{
						//                ExtOut("[%S: %p] ", g_mdName, (ULONG64)FrameData.frameAddr);
					} else
					{
						// ExtOut("[Frame: %p] ", (ULONG64)FrameData.frameAddr);
					}

				};
			} while (stackWalk->Next() == S_OK);
		}
		
	return std::vector<ClrStackFrame>();
}
*/
