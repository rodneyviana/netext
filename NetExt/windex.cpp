/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "selectparser.h"
#include "Indexer.h"
#ifndef _CLASSDEF_
#include "netext.h"

#endif
#include <memory>
#include <list>

struct IndexFlags
{
	bool ftree;
	bool fquiet;
	bool fenumtypes;
	bool fflush;
	bool fshort;
	bool fsave;
	bool fload;
	bool fignorestate;
	UINT top;
	std::string saveFileName;
	std::string loadFileName;
	vector<string> type;
	vector<CLRDATA_ADDRESS> mt;
};

EXT_COMMAND(windex,
			"Index and dump heap. Use '!whelp windex' for detailed help",
			"{quiet;b,o;;quiet;Do not display index progress}"
			"{enumtypes;b,o;;enumtypes;Display types with link to objects}"
			"{tree;b,o;;tree;Show types tree on a different window (WinDBG only)}"
			"{flush;b,o;;flush;Fush index}"
			"{top;ed,o;;Limit to the number of objects displayed}"
			"{short;b,o;;short;Display addresses only}"
			"{ignorestate;b,o;;ignorestate;Ignore state when loading index}"
			"{type;s,o;;type;List of types to include wildcards accepted (eg. -type *HttRequest,system.servicemodel.*)}"
			"{fieldname;s,o;;fieldname;List of field names that the type must contain (eg. -fieldname *request,_wr)}"
			"{fieldtype;s,o;;fieldtype;List of field types that the type must contain (eg. -fieldtype *.String,*.object)}"
			"{implement;s,o;;implement;List of parent types that the type must implement (eg. -implement *.Exception,*.Array)}"
			"{withpointer;b,o;;withpointer;List all types which include pointer fields}"
			"{save;s,o;;save;Save index to file}"
			"{load;s,o;;load;Load previously saved index file}"
			"{mt;s,o;;mt;List of Method Tables to include (eg. -mt 7012ab8,70ac080)}")

{
	IndexFlags flags;
	string typeStr;
	string mtStr;
	if(HasArg("top"))
	{
		flags.top = (UINT)(GetArgU64("top") & UINT32_MAX);
	} else
	{
		flags.top = 0;
	}

	flags.fenumtypes = HasArg("enumtypes");
	flags.ftree = HasArg("tree");
	flags.fquiet = HasArg("quiet");
	flags.fflush = HasArg("flush");
	flags.fshort = HasArg("short");
	if(flags.fshort) flags.fquiet = true;
	flags.fignorestate = HasArg("ignorestate");
	flags.fsave = HasArg("save");
	flags.fload = HasArg("load");
	INIT_API();
	std::unique_ptr<Heap> heap(new Heap());

	if(flags.fsave && flags.fload)
	{
		Out("You must not use load and save together\n");
		return;
	}

	if(!heap->IsValid())
	{
		Out("Heap is invalid\n");
		return;
	}
	heap->EnumRanges();
	if(flags.fload)
	{
		flags.loadFileName.assign(GetArgStr("load"));
		delete indc;
		indc=new Indexer();
		if(!indc->LoadIndex(flags.loadFileName, flags.fignorestate))
		{
			Out("Unable to load index file\n");
			return;
		} else
		{
			Out("Index file %s was loaded\n",flags.loadFileName.c_str());
			return;
		}
	}

	if(flags.fflush)
	{
		if(indc)
		{
			delete indc;
			indc=NULL;
		}
		Out("Index has been flushed\n");
		pRuntime->Flush();
		return;
	}

	UINT64 startTime, endTime;

	if(indc==NULL)
	{
		indc=new Indexer();
	}
	if(flags.fload || indc->IsUpToDate())
	{
		if(!flags.fquiet) Out("Index is up to date\n If you believe it is not, use !windex -flush to force reindex\n");
	} else
	{
		startTime = GetTickCount64();

		if(!flags.fquiet)
		{
			NameBuffer[0]=L'\0';
			GetTimeFormatEx(LOCALE_NAME_USER_DEFAULT, TIME_FORCE24HOURFORMAT, NULL, NULL, NameBuffer, MAX_MTNAME);

			Out("Starting indexing at %S\n", NameBuffer);
		}
		indc->DoIndex(!flags.fquiet);
		endTime = GetTickCount64();
		if(!flags.fquiet)
		{
			NameBuffer[0]=L'\0';
			GetTimeFormatEx(LOCALE_NAME_USER_DEFAULT, TIME_FORCE24HOURFORMAT, NULL, NULL, NameBuffer, MAX_MTNAME);

			Out("Indexing finished at %S\n", NameBuffer);
			Out("%S Bytes in %S Objects\n", formatnumber(indc->size).c_str(), formatnumber(indc->count).c_str());

			Out("Index took %s\n", tickstotimespan((endTime-startTime)*10000).c_str());
		}
	}
	if(flags.fenumtypes)
	{
		if(HasArg("type")) 
		{
			typeStr.assign(GetArgStr("type"));
			indc->DumpTypes(&typeStr);
		} else
			indc->DumpTypes();

		return;
	}
	if(flags.ftree)
	{
		indc->DumpTypesTree(this);
	}
	MatchingAddresses Addresses;

	if(HasArg("type"))
	{
		typeStr.assign(GetArgStr("type"));
		std::wstring wtypeStr((wchar_t*)CA2W(typeStr.c_str()));
		indc->GetByType(wtypeStr, Addresses);
		if(!DisplayHeapEnum(Addresses, flags.fshort, flags.top))
		{
			return;
		}
	}
	if(HasArg("implement"))
	{
		typeStr.assign(GetArgStr("implement"));
		indc->GetByDerive(typeStr, Addresses);
		if(!DisplayHeapEnum(Addresses, flags.fshort, flags.top))
		{
			return;
		}
	}
	if(HasArg("fieldname"))
	{
		typeStr.assign(GetArgStr("fieldname"));
		indc->GetByFieldName(typeStr, Addresses);
		if(!DisplayHeapEnum(Addresses, flags.fshort, flags.top))
		{
			return;
		}
	}
	if(HasArg("fieldtype"))
	{
		typeStr.assign(GetArgStr("fieldtype"));
		indc->GetByFieldType(typeStr, Addresses);
		if(!DisplayHeapEnum(Addresses, flags.fshort, flags.top))
		{
			return;
		}
	}
	if(HasArg("withpointer"))
	{
		indc->GetWithPointers(Addresses);
		if(!DisplayHeapEnum(Addresses, flags.fshort, flags.top))
		{
			return;
		}
	}

	if(HasArg("mt"))
	{
		mtStr.assign(GetArgStr("mt"));
		indc->GetByMethodName(mtStr, Addresses);
		if(!DisplayHeapEnum(Addresses, flags.fshort, flags.top))
		{
			return;
		}
	}
	if(flags.fsave)
	{
		flags.saveFileName.assign(GetArgStr("save"));
		if(indc->SaveIndex(flags.saveFileName))
		{
			Out("Index saved succesfully on %s\n", flags.saveFileName.c_str());
		} else
			Err("Failed to save index on %s\n", flags.saveFileName.c_str());
	}
}