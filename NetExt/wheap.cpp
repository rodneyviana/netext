/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "selectparser.h"
#include "Indexer.h"
#ifndef _CLASSDEF_
#include "CLRHelper.h"

#endif
#include <memory>
#include <list>

//----------------------------------------------------------------------------
//
// mt extension command.
//
// This command displays the MethodTable of a .NET object
//
// The argument string means:
//
//   {;          - No name for the first argument.
//   e,          - The argument is an expression.
//   o,          - The argument is optional.
//   ;			 - There is no argument's default expression
//   Object;     - The argument's short description is "Object".
//   Object address - The argument's long description.
//   }           - No further arguments.
//
// This extension has a single, optional argument that
// is an expression for the PEB address.
//
//----------------------------------------------------------------------------

list<CLRDATA_ADDRESS> gcBorders;

struct HeapFlags
{
	bool fshort;
	bool fdetailsonly;
	bool fnothrottle;
	bool fstart;
	bool fend;
	CLRDATA_ADDRESS start;
	CLRDATA_ADDRESS end;
	vector<string> type;
	vector<CLRDATA_ADDRESS> mt;
};

EXT_COMMAND(wheap,
            "Dump heap objects. Use '!whelp wheap' for detailed help",
			"{short;b,o;;Dump object addresses only for .foreach processing}"
			"{detailsonly;b,o;;Show heap detail and areas}"
			"{nothrottle;b,o;;No limit for objects dumped per area. Otherwise will show only first 500 per heap area}"
			"{start;ed,o;;start address}"
			"{end;ed,o;;end address}"
			"{type;s,o;;List of types to include wildcards accepted (eg. -type *HttRequest,system.servicemodel.*)}"
			"{mt;s,o;;List of Method Tables to include (eg. -mt 7012ab8,70ac080)}")

{
		INIT_API();

		string typeStr;
		string mtStr;
		gcBorders.clear();
		HeapFlags flags;
		flags.fshort = HasArg("short");
		flags.fdetailsonly = HasArg("detailsonly");
		flags.fnothrottle = HasArg("nothrottle");
		flags.fstart = HasArg("start");
		flags.fend = HasArg("end");
		if(flags.fstart) flags.start = GetArgU64("start");
		if(flags.fend) flags.end = GetArgU64("end");
		//sqlparser::Split

		if(flags.end < flags.start)
		{
			swap(flags.end, flags.start);
		}
		try
		{
			std::unique_ptr<Heap> heap(new Heap());
			heap->EnumRanges();
			if(!heap->IsValid())
			{
				Out("Error: Unable to get heap info\n");
				return;
			}
			if(HasArg("type"))
			{
				typeStr.assign(GetArgStr("type"));
				Heap::AddTypes(typeStr);
			}
			if(HasArg("mt"))
			{
				mtStr.assign(GetArgStr("mt"));
				Heap::AddMT(mtStr);
			}

			if(!heap->AreStructuresValid())
			{
				Out("Error: Unable to get heap structures\n");
				return;
			}
			if(!flags.fshort)
			{
				Out("Heaps: %u\n", heap->Count());
				Out("Server Mode: %u\n", (UINT)heap->IsServerMode());
			}
			if((typeStr.size()>0 || mtStr.size()>0 || flags.fstart || flags.fend || flags.fshort) && !flags.fnothrottle)
			{
				flags.fnothrottle = true;
				if(!flags.fshort) Out("You have used a parameter that disables throttle. Results below are complete\n");
			}
			//for(int i=0;i<heap->Count(); i++)
			//{
			//	if(!heap->IsServerMode() && i>0)
			//		break;
			//	DacpGcHeapDetails detail = heap->GetHeapDetails(i);
			//	if(!flags.fshort)
			//	{
			//		Out("Heap [%i]:\n",i);
			//		Out("\tAllocated: %p\n", detail.alloc_allocated);
			//		Out("\tCard Table: %p\n", detail.card_table);
			//		Out("\tEphemeral Heap Segment: %p\n", detail.ephemeral_heap_segment);
			//		Out("\tFinalization Fill Pointers: %p\n", detail.finalization_fill_pointers);
			//		Out("\tHeap Address: %p\n", detail.heapAddr);
			//		Out("\tLowest Address: %p\n", detail.lowest_address);
			//		Out("\tHighest Address: %p\n", detail.highest_address);
			//		Out("\tGeneration Addresses:\n");
			//	}
			//	vector<CLRDATA_ADDRESS> segments;
			//	//CLRDATA_ADDRESS cSeg = NULL;
			//	for(int k=0;k<=heap->MaxGeneration()+1;k++)
			//	{
			//		//if(cSeg != detail.generation_table[k].start_segment)
			//		//{
			//		//	cSeg = detail.generation_table[k].start_segment;
			//		//	segments.push_back(cSeg);
			//		//}
			//		if(!flags.fshort)
			//		{
			//			Out("\t\t[%i]:AllocStart(%p),AllocCxtLimit(%p),AllocCtxPtr(%p),StartSeg(%p)\n",
			//				k,
			//				detail.generation_table[k].allocation_start,
			//				detail.generation_table[k].allocContextLimit,
			//				detail.generation_table[k].allocContextPtr,
			//				detail.generation_table[k].start_segment);
			//		}
			//		CLRDATA_ADDRESS currSegment = detail.generation_table[k].start_segment;
			//		int throttle = 0; // avoid problems with bad structure
			//		if(k>0 && (currSegment == detail.generation_table[k-1].start_segment)) currSegment = NULL;
			//		while(currSegment)
			//		{
			//			if(IsInterrupted())
			//			{
			//				Out("\nInterrupted by user\n");
			//				return;
			//			}

			//			DacpGcHeapDetails heapDetail;
			//			DacpHeapSegmentData segment;

			//			if(FAILED(segment.Request(sosData, currSegment, heapDetail)))
			//			{
			//				break;
			//			}

			//			segments.push_back(currSegment);
			//			gcBorders.push_back(segment.mem);

			//			gcBorders.push_back(detail.alloc_allocated);

			//			currSegment = segment.next;
			//			if(throttle++>10)
			//				break;
			//		}
			//		// Add to the ordered list
			//		//gcBorders.push_back(detail.generation_table[k].allocation_start);
			//		if(k<heap->Count()-1)
			//		{
			//			//Heap::DisplayHeapObjects(detail.generation_table[k].allocation_start, detail.generation_table[k+1].allocation_start, false, 50);
			//		}
			//	}
			//	if(!flags.fshort)
			//	{
			//		Out("\nSegments:\n");

			//		for(
			//			vector<CLRDATA_ADDRESS>::const_iterator iSeg = segments.begin();
			//			iSeg != segments.end(); iSeg++)
			//		{
			//			DacpHeapSegmentData sd;
			//			DacpGcHeapDetails ht;
			//			if(sd.Request(sosData, *iSeg, ht)!=S_OK)
			//			{
			//				Out("Bad segment at: %p", sd.segmentAddr);
			//			} else
			//				Out("Segment: %p Start: %p End: %p HiMark: %p Committed: %p Reserved: %p Next: %p\n", sd.segmentAddr, sd.mem, sd.allocated, sd.highAllocMark, sd.committed, sd.reserved, sd.next);
			//		}
			//	}
			//	segments.clear();

			//	//gcBorders.push_back(detail.highest_address);
			//	//vector<CLRDATA_ADDRESS_RANGE> areas;
			//	//gcBorders.sort();
			//	//list<CLRDATA_ADDRESS>::const_iterator it = gcBorders.begin();
			//	//for(int z=0; z<gcBorders.size() / 2; z++)
			//	//{
			//	//	CLRDATA_ADDRESS_RANGE area;
			//	//	area.startAddress = *it++;
			//	//	area.endAddress = *it++;
			//	//	areas.push_back(area);
			//	//	Out("Area %i: %p %p %x (0n%u)\n",z,area.startAddress, area.endAddress, area.endAddress-area.startAddress, area.endAddress-area.startAddress);
			//	//	Heap::DisplayHeapObjects(area.startAddress, area.endAddress, false, 10000000);
			//	//}
			//	//gcBorders.clear();
			//}
			heap->EnumRanges();
			map<CLRDATA_ADDRESS, HeapRange>::const_iterator it = heap->ranges.begin();

			if(!flags.fshort)
			{
				Out("\nHeap Areas:\n");
				UINT64 total = 0;
				while(it!=heap->ranges.end())
				{
					HeapRange range = it->second;
					CLRDATA_ADDRESS begin = get<0>(range);
					CLRDATA_ADDRESS end = get<1>(range);
					int heapN = get<2>(range);
					int gen = get<3>(range);
					bool isLarge = get<4>(range);

					Out("Area [%p]: %p-%p (%12.12S) ", begin,
						begin, end, formatnumber(end-begin).c_str());
					total += end-begin;
					Out("Heap: %i Generation: %i Large: %i\n", heapN, gen, (int)isLarge);
					it++;
				}
				Out("\nTotal Bytes Used for GC objects: %S", formatnumber(total).c_str());
				Out("\n");
			}

			it = heap->ranges.begin();

			EXT_CLASS::SessionThreads.Request();
			if(!flags.fdetailsonly)
			{
				while(it!=heap->ranges.end())
				{
					if(IsInterrupted()) return;
					HeapRange range = it->second;
					CLRDATA_ADDRESS begin = get<0>(range);
					CLRDATA_ADDRESS end = get<1>(range);
					int heapN = get<2>(range);
					int gen = get<3>(range);
					bool isLarge = get<4>(range);
					it++;

					if(flags.fstart)
					{
						if(end < flags.start)
							continue;
						begin = max(begin, flags.start);
					}

					if(flags.fend)
					{
						if(begin >= flags.end)
							continue;
						end = min(end, flags.end);
					}
					UINT throttle = flags.fnothrottle ? MAXDWORD32 : 500;

					heap->DisplayHeapObjects(begin, end, heapN, gen, isLarge, throttle,flags.fshort);
					if(heap->wasInterrupted) return;
				}
				if(!flags.fnothrottle) Out("This output was throttled. Only the first 500 objects of each heap range has been shown.\nUse -nothrottle to list all objects or any limiting parameter (-type for example)\n");
			}
		} catch(std::exception& e)
		{
			Out("wheap: %s\n", e.what());
		}
}

bool Heap::IsInHeap(CLRDATA_ADDRESS Address, int* iHeap, int* iGen)
{

	int heap =-1;
	int gen = -1;

	map<CLRDATA_ADDRESS, HeapRange>::const_iterator it = ranges.begin();
	if(iHeap != NULL && iGen != NULL)
	{
		*iHeap = -1;
		*iGen = -1;
	}
	while(it!=ranges.end())
	{
		if(IsInterrupted()) return false;
		HeapRange range = it->second;
		CLRDATA_ADDRESS begin = get<0>(range);
		CLRDATA_ADDRESS end = get<1>(range);
		heap = get<2>(range);
		gen = get<3>(range);
		if(Address >= begin && Address < end)
		{
			if(iHeap != NULL && iGen != NULL)
			{
				*iHeap = heap;
				*iGen = gen;
				return true;
			}
		}
		it++;
	}

	return false;
}

void Heap::AddRanges(Thread &thread, CLRDATA_ADDRESS Start, CLRDATA_ADDRESS End,
					 long heap, int gen, bool isLarge)
{
	CLRDATA_ADDRESS s=Start;
	while(s < End)
	{
		Thread::const_iterator it = thread.GetThreadInRange(s, End);
		if(it != thread.end())
		{
			ranges[s]=make_tuple(s, it->AllocationStart,
				heap, gen, isLarge);
			s=it->AllocationLimit+g_ExtInstancePtr->m_PtrSize*3; // First object after limit

		} else
		{
			if(s < End)
			{
				ranges[s]=make_tuple(s, End,
					heap, gen, isLarge);
				s=End;
			}

		}

	}
}

void Heap::EnumRanges()
{
	if(ranges.size() > 0)
		return;
	ranges.clear();

	
	CComPtr<IMDSegmentEnum> pSegEnum;

	HRESULT hr=pHeap->EnumerateSegments(&pSegEnum);
	if(hr!=S_OK || pSegEnum == NULL)
	{
		g_ExtInstancePtr->Out("Heap cannot be walked\n");
		return;
	}
	int i=0;
	Thread td;
	td.Request(true); // Order by Allocation start

	while(hr==S_OK && i < 1000) // Also avoid infinite loop
	{
		i++;
		CComPtr<IMDSegment> pSeg;
		hr=pSegEnum->Next(&pSeg);
		if(hr==S_OK && pSeg)
		{
			MD_SegData segData;
			pSeg->GetSegData(&segData);
			CLRDATA_ADDRESS gen2End = 0;
			CLRDATA_ADDRESS gen1End = 0;
			if(segData.Gen2Length) AddRanges(td, segData.Gen2Start,
				segData.Gen2Start+segData.Gen2Length,segData.ProcessorAffinity, 2, false);
			if(segData.Gen1Length) AddRanges(td, segData.Gen1Start,
				segData.Gen1Start+segData.Gen1Length,segData.ProcessorAffinity, 1, false);
			if(segData.Gen0Length) AddRanges(td, segData.Gen0Start,
				segData.Gen0Start+segData.Gen0Length,segData.ProcessorAffinity, 0, false);
			if(segData.IsLarge) AddRanges(td, segData.FirstObject,
				segData.End,segData.ProcessorAffinity, 3, true);

		} else
		{
			hr = E_FAIL;
		}
	}
}

CLRDATA_ADDRESS Heap::DisplayHeapObjects(CLRDATA_ADDRESS Start, CLRDATA_ADDRESS End, int HeapNumber, int Gen, bool IsLarge, UINT Limit, bool IsLean)
{
	CLRDATA_ADDRESS objAddr = Start;
	UINT64 i=0;
	while(objAddr < End && (i++<Limit || Limit==MAXDWORD32))
	{
		if(IsInterrupted())
		{
			//g_ExtInstancePtr->Out("Interrupted\n");
			wasInterrupted = true;
			return objAddr;
		}
		ObjDetail obj(objAddr);
		CLRDATA_ADDRESS mt = ObjDetail::GetPTR(objAddr) & ~(CLRDATA_ADDRESS)3;
		bool printObj = Heap::IncludeType(mt);
		if(!printObj) printObj = Heap::IncludeType(mt, obj.TypeName());

		if(IsLean)
		{
			if(obj.IsValid())
			{
				if(printObj) g_ExtInstancePtr->Out("%p\n", objAddr);
			}
		}
		else
		{
			if(printObj) g_ExtInstancePtr->Dml("<link cmd=\"!wdo %p\">%p</link> %p %8u %3i %i ", objAddr, objAddr, mt, obj.Size(), HeapNumber, Gen  );
		}
		if(obj.IsFree())
		{
			if(!IsLean)
			{
				if(printObj) g_ExtInstancePtr->Out("Free\n");
			}
		} else if(obj.IsValid())
		{
			if(!IsLean)
			{
				if(printObj) g_ExtInstancePtr->Out("%S\n", GetMethodName(mt).c_str());
			}
		} else
		{
			Thread::const_iterator td = EXT_CLASS::SessionThreads.GetThreadByAddress(objAddr);
			if(td != EXT_CLASS::SessionThreads.end())
			{
				objAddr = td->AllocationLimit; //move to the end of thread allocation area
			}
			objAddr+=sizeof(void*)*3; // avoid bad pointer infinite loop

			if(!IsLean)
			{
				if(printObj) g_ExtInstancePtr->Out(" BadPtr\n");
			}
			//return objAddr;
		}
		//g_ExtInstancePtr->Out("\n");

		if(IsLarge)
			objAddr += AlignLarge(obj.Size());
		else
			objAddr += Align(obj.Size());
	}
	//g_ExtInstancePtr->Out("%u Objects\n", i);
	return objAddr;
}