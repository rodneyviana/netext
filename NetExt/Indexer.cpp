/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "NetExt.h"
#include "Indexer.h"
#include <fstream>
#include <stdio.h>
#include <locale>


Indexer *indc=NULL;

Indexer::Indexer(void)
{
	isValid = false;
	commonMTs.Request();
}

Indexer::~Indexer(void)
{
	if(treeFilename.size() == 0) return;
	if( remove( treeFilename.c_str() ) != 0 )
			g_ExtInstancePtr->Out("Error deleting temporary tree file %s...\n"
			"You may have to delete it manually\n", treeFilename.c_str());
	else
		g_ExtInstancePtr->Out("Temporary tree file %s was deleted successfully\n", treeFilename.c_str());
}

std::string Indexer::CalculateSignature()
{
	std::string strSig = EXT_CLASS::Execute("~*e? @esp;? @eip;? @eax;? @cs");
	//char rang[128];
	//map<CLRDATA_ADDRESS, HeapRange>::const_iterator it = heap->ranges.begin();
	//string strSig;
	//while(it!=heap->ranges.end())
	//{
	//	HeapRange range = it->second;
	//	sprintf_s(rang,128,"SEG%pS%pE%pH%iG%i\n",it->first, get<0>(range),
	//					get<1>(range), get<2>(range), get<3>(range));
	//	it++;
	//	strSig.append(rang);
	//}
	return strSig;
}

bool Indexer::DoIndex(bool ShowProgress)
{

	string temp = CalculateSignature();
	//g_ExtInstancePtr->Out("%s\n",temp.c_str());
	unique_ptr<Heap> heap(new Heap());
	if(signature == temp)
	{
		if(ShowProgress)
			g_ExtInstancePtr->Out("Getting from heap index\n");
	} else
	{
		heap->EnumRanges();
		map<CLRDATA_ADDRESS, HeapRange>::const_iterator it = heap->ranges.begin();
		map<CLRDATA_ADDRESS, HeapRange>::const_reverse_iterator itend = heap->ranges.rbegin();

		//Thread threads;
		//threads.Request();

		
		CLRDATA_ADDRESS mn = get<0>(it->second);//heap->GetHeapDetails(0).lowest_address;
		CLRDATA_ADDRESS mx = UINT64_MAX; // <1>(itend->second); //heap->GetHeapDetails(heap->Count()-1).highest_address;
		unique_ptr<DumpHeapCache> cache(new DumpHeapCache());
		count=0;
		size=0;
		while(it!=heap->ranges.end())
		{
			if(IsInterrupted()) return false;
			HeapRange range = it->second;
			CLRDATA_ADDRESS begin = get<0>(range);
			CLRDATA_ADDRESS end = get<1>(range);
			int heapN = get<2>(range);
			int gen = get<3>(range);
			bool isLarge = get<4>(range);
			//g_ExtInstancePtr->Out("Start: %p End: %p\n", begin, end);

			if(!WalkHeapAndCache(this, begin, end, isLarge, mn, mx, cache.get(), !ShowProgress))
			{
				isValid=false;
				mtT.clear();
				typeT.clear();
				g_ExtInstancePtr->Out("Index was not created\n");
				return false;
			}

			it++;
			//heap->DisplayHeapObjects(begin, end, heapN, gen, isLarge, throttle,flags.fshort);
		}

		signature = temp;
		isValid = true;
	}
	return true;
}

void Indexer::DumpTypes(string* Type)
{
	TypeTable::const_iterator tt=typeT.begin();

	ULONG total=0;
	ULONG count = 0;
	while(tt!=typeT.end())
	{
		if(Type == NULL || g_ExtInstancePtr->MatchPattern(CW2A(mtT[tt->second].typeName.c_str()), Type->c_str()))
		{
			g_ExtInstancePtr->Dml("<link cmd=\"!windex -mt %p\">%p</link>", tt->second, tt->second);
			g_ExtInstancePtr->Out(" %S (%i)\n", mtT[tt->second].typeName.c_str(), mtT[tt->second].Addresses.size());
			total+=static_cast<ULONG>(mtT[tt->second].Addresses.size());
			count++;
		}
		tt++;
	}
	g_ExtInstancePtr->Out("Heap contains %u Objects in %u types selected\n", total,count);
	if(Type != NULL)
		g_ExtInstancePtr->Out("%u Types skipped by the filter\n", typeT.size() - count); 
}

inline std::string toCPPFile(const std::string& FileName)
{
	std::string cppFileName;
	std::string::const_iterator ch = FileName.begin();
	while(ch!=FileName.end())
	{
		if((*ch=='\\') || (*ch=='\"'))
		{
			cppFileName.append("\\");
		}
		cppFileName += *ch;

		ch++;
	}
	return cppFileName;
}

void Indexer::DumpTypesTree(EXT_CLASS *Ext)
{
	char fileName[MAX_PATH];
	char pathName[MAX_PATH];
	char tempCmd[MAX_MTNAME*3+1];
	ofstream treeStream;
	DWORD hr;

	if(treeFilename.size() > 0)
	{
		//g_ExtInstancePtr->Dml("<link cmd=\".cmdtree %s\n\">Run </link>", treeFilename.c_str());
		g_ExtInstancePtr->Out("Copy, paste and run command below:\n.cmdtree %s\n", treeFilename.c_str());
		return;
	}

	hr = GetTempPathA(MAX_PATH, pathName);
	if((hr==0) || (hr>MAX_PATH))
	{
		g_ExtInstancePtr->Out("Please make sure you set TEMP environment variable and try again\n");
		return;
	}
	hr = GetTempFileNameA(pathName, "HEAPTREE", 0, fileName);

	if(!hr)
	{
		g_ExtInstancePtr->Out("Unable to generate temp file name\n");
		return;
	}
	treeStream.open(fileName);
	treeStream << "windbg ANSI Command Tree 1.0\n";
	treeStream << "title {\"Types in Heap\"}\nbody\n";
	treeStream << "{\"Types\"}\n";
	TypeTable::const_iterator tt=typeT.begin();

	while(tt!=typeT.end())
	{
		sprintf_s(tempCmd, MAX_MTNAME*3, " {\"%p %S (%i)\"} {\"!windex -mt %p\"}\n", tt->second, mtT[tt->second].typeName.c_str(),
			mtT[tt->second].Addresses.size(), tt->second);
		treeStream << tempCmd;
		EEClass cl;
		cl.Request((CLRDATA_ADDRESS)tt->second);
		cl.EnsureFields();
		std::vector<FieldStore> fields=cl.Fields();
		for(int i=0;i<fields.size();i++)
		{
			sprintf_s(tempCmd, MAX_MTNAME* 3, "  {\"%S (%S) +0n%u Token: %x\"} {\".foreach({$addr} {!windex -short -mt %p}){.echo Address: {$addr}; !wselect %S from {$addr}}\"}\n", fields[i].FieldName.c_str(), fields[i].mtName.c_str(),
				fields[i].FieldDesc.offset, fields[i].FieldDesc.token, tt->second, fields[i].FieldName.c_str());
			treeStream << tempCmd;
		}


		tt++;
	}
	treeStream.close();
	sprintf_s(tempCmd, MAX_MTNAME, ".cmdtree %s\n", fileName);
	treeFilename.assign(fileName);
	//g_ExtInstancePtr->Dml("<link cmd=\"%s\">Run </link>", tempCmd);
	g_ExtInstancePtr->Out("Copy, paste and run command below:\n%s\n",tempCmd);
	//Ext->Execute(tempCmd);
}

void Indexer::GetByType(std::wstring PartialTypeNames, MatchingAddresses& Addresses)
{
	std::string partialNames = localCW2A(PartialTypeNames.c_str());
	vector<std::string> types;
	VectorSplit(partialNames, types);

	Addresses.clear();
	TypeTable::const_iterator it=typeT.begin();
	while(it!=typeT.end())
	{
		for(int i=0;i<types.size();i++)
		{
			if(g_ExtInstancePtr->MatchPattern(CW2A(it->first.c_str()), types[i].c_str()))
			{
				Addresses.push_back(&mtT[it->second].Addresses);
			}
		}
		it++;
	}
}

void Indexer::GetByMethodName(std::string MethodNameStringList, MatchingAddresses& Addresses)
{
		std::vector<std::string> tempMT;
		std::vector<CLRDATA_ADDRESS> mtList;
		VectorSplit(MethodNameStringList, tempMT);
		std::vector<std::string>::const_iterator it = tempMT.begin();

		while(it!=tempMT.end())
		{
			CLRDATA_ADDRESS addr;
			addr = g_ExtInstancePtr->EvalExprU64(it->c_str());
			mtList.push_back(addr);
			it++;
		}
		Addresses.clear();
		MTTable::const_iterator itm=mtT.begin();
		while(itm!=mtT.end())
		{
			for(int i=0;i<mtList.size();i++)
			{
				if(itm->first==mtList[i])
				{
					Addresses.push_back(&mtT[itm->first].Addresses);
				}
			}
			itm++;
		}
}

void Indexer::GetByFieldName(std::string FieldNameStringList, MatchingAddresses& Addresses)
{
	vector<std::string> fnames;
	VectorSplit(FieldNameStringList, fnames);

	Addresses.clear();
	TypeTable::const_iterator it=typeT.begin();
	while(it!=typeT.end())
	{
		EEClass cl;
		cl.Request((CLRDATA_ADDRESS)it->second);

		for(int i=0;i<fnames.size();i++)
		{
			if(cl.IsValidClass() && cl.FieldsByName(fnames[i]).size()>0)
			{
				Addresses.push_back(&mtT[it->second].Addresses);
			}
		}
		it++;
	}
}
void Indexer::GetByFieldType(std::string FieldTypeStringList, MatchingAddresses& Addresses)
{
	vector<std::string> ftype;
	VectorSplit(FieldTypeStringList, ftype);

	Addresses.clear();
	TypeTable::const_iterator it=typeT.begin();
	while(it!=typeT.end())
	{
		EEClass cl;
		cl.Request((CLRDATA_ADDRESS)it->second);

		for(int i=0;i<ftype.size();i++)
		{
			if(cl.IsValidClass() && cl.FieldsByType(ftype[i]).size()>0)
			{
				Addresses.push_back(&mtT[it->second].Addresses);
			}
		}
		it++;
	}
}
void Indexer::GetByDerive(std::string DeriveStringList, MatchingAddresses& Addresses)
{
	vector<std::string> fchain;
	VectorSplit(DeriveStringList, fchain);

	Addresses.clear();
	TypeTable::const_iterator it=typeT.begin();
	while(it!=typeT.end())
	{
			EEClass cl;
			cl.Request((CLRDATA_ADDRESS)it->second);
			for(int i=0;i<fchain.size();i++)
			{
				std::wstring str(CA2W(fchain[i].c_str()));

				if(cl.IsValidClass() && cl.Implement(str))
				{
					Addresses.push_back(&mtT[it->second].Addresses);
				}
			}

		//DacpMethodTableData mt;
		//mt.Request(sosData, it->second);
		//EEClass cl;
		//cl.Request(mt.Class);

		//for(int i=0;i<fchain.size();i++)
		//{
		//	std::wstring str(CA2W(fchain[i].c_str()));
		//	if(cl.Implement(str))
		//	{
		//		Addresses.push_back(&mtT[it->second].Addresses);
		//	}
		//}
		it++;
	}
}

void Indexer::GetWithPointers(MatchingAddresses& Addresses)
{
	Addresses.clear();
	TypeTable::const_iterator it=typeT.begin();
	while(it!=typeT.end())
	{

			EEClass cl;
			cl.Request((CLRDATA_ADDRESS)it->second);
			if(cl.IsValidClass() && cl.PointerFields().size()>0)
			{
				Addresses.push_back(&mtT[it->second].Addresses);
			}
		it++;
	}
}
void Indexer::AddAddress(DWORD_PTR Address, DWORD_PTR MethodTable, MD_TypeData* obj)
{
	if(MethodTable == 0)
		return;
	typeIndex *ti=&mtT[MethodTable];
	ti->Addresses.push_back(Address);

	// if it is the first time, get type info
	if(ti->Addresses.size() == 1)
	{
		ObjDetail tmpObj(Address);
		//ti->EEClass=obj.EEClass;
		CLRDATA_ADDRESS elemSize=0;
		CLRDATA_ADDRESS baseSize=0;
		
		if(tmpObj.IsValid() && tmpObj.InnerComponentSize() > 0)
		{
			obj->elementSize = tmpObj.InnerComponentSize();
			obj->BaseSize = tmpObj.BaseSize();
			obj->size = tmpObj.Size();
		} else
		{
			if(tmpObj.IsArray())
			{
				EEClass::GetArraySizeByMT(MethodTable, &baseSize,
					&elemSize);
				obj->elementSize = static_cast<long>(elemSize);
				obj->BaseSize = baseSize;
			} else
			{
				obj->BaseSize = tmpObj.BaseSize();
				obj->elementSize = tmpObj.IsString() ? 2 : 0;
			}
		}
		if(obj->BaseSize == 0)
		{
			obj->MethodTable = 0;
			obj->arraySize = 0;
			obj->size = 0;
			obj->elementSize = 0;
			mtT.erase(MethodTable);
			return;
		}
		ti->mtData = *obj;
		if(obj->MethodTable != commonMTs.FreeMethodTable())
		{
			if(tmpObj.IsValid())
				ti->typeName = tmpObj.TypeName();
			else
				ti->typeName = L"<UNKNOWN>";
			/*
			ti->typeName = EEClass::GetNameForMT(obj->MethodTable);
			if(ti->typeName.size() < 1)
			{
				ti->typeName = L"<UNKNOWN>";
			}
			*/
			typeT.insert(pair<std::wstring, DWORD_PTR>(ti->typeName,MethodTable));
		} else
		{
			ti->typeName = L"Free";
			typeT.insert(pair<std::wstring, DWORD_PTR>(L"Free", MethodTable));
			ti->mtData.elementSize = 1;
			obj->elementSize = 1; // Free is somewhat an array
		}
		//g_ExtInstancePtr->Out("%p: %S (%p) %u\n", Address, ti->typeName.c_str(), MethodTable,  ti->mtData.BaseSize + obj->dwNumComponents * ti->mtData.ComponentSize);
	}
	obj->size = ti->mtData.BaseSize + obj->arraySize * ti->mtData.elementSize;
	ti->size+=obj->size;
	count++;
	size += obj->size;
}

bool Indexer::IsUpToDate(void)
{

	string temp=CalculateSignature();
	if(signature == temp)
		return true;
	return false;
}

#define writestr(f,s) \
	if(!WriteString(f, s)) goto clean;

#define writebuf(f,b,s) \
	if(!WriteBuffer(f, b, s)) goto clean;

long SimpleHash(const std::string& Str)
{
	/*
	locale loc("C");
	const collate<char>& collUS = use_facet<collate<char>>(loc);
	return collUS.hash(Str.data(),Str.data()+Str.length());
	*/
	UINT64 sum1=0;
	UINT64 sum2=0;
	for(int i=0;i<Str.size();i++)
	{
		sum1+=i*(int)Str[i];
		sum2+=sum1*i+(int)Str[i];
	}
	return(long)((sum1 ^ sum2) & 0xffffffff);
}

inline bool WriteString(FILE *file, const std::string& Str, bool ZeroTerminated=false)
{
	DWORD size=static_cast<DWORD>(Str.size());
	if(ZeroTerminated) size++;
	if(fwrite(&size,1,sizeof(size),file)!=sizeof(size))
		return false;
	return (fwrite(Str.c_str(), 1, size, file) == size);
}

inline bool WriteBuffer(FILE *file, void* buff, size_t size)

{
	return (fwrite(buff, 1, size, file)==size);
}

#define readstr(f,str) \
	if(!ReadString(file, str)) goto clean;

#define readbuf(f,b,s) \
	if(!ReadBuffer(f, b, s)) goto clean;

inline bool ReadString(FILE *file, std::string& Str, bool ZeroTerminated=false)
{
	DWORD size;
	if(fread((void*)&size,1,sizeof(size),file)!=sizeof(size))
		return false;
	if(ZeroTerminated) size++;
	if(size>=MAX_MTNAME) return false;
	char str[MAX_MTNAME];
	ZeroMemory(&str,MAX_MTNAME);
	if(fread((void*)&str, 1, size, file) == size)
	{
		Str.assign(str);
		return true;
	} else
	{
		return false;
	}
}

inline bool ReadBuffer(FILE *file, void* buff, size_t size)

{
	return (fread(buff, 1, size, file)==size);
}

bool Indexer::SaveIndex(std::string FileName)
{


	FILE *file = fopen(FileName.c_str(), "wb");
	if(!file)
	{
		g_ExtInstancePtr->Out("Failed to create the file %s\nVerify if the name is valid and you have write rights to the folder\n", FileName.c_str());
		return false;
	}

	try
	{
		// signature
		writestr(file,"RHV");
		// Pointer size
		BYTE w=(BYTE)g_ExtInstancePtr->m_PtrSize;
		writebuf(file, (void*)&w,sizeof(w));
		// Hash

		std::string temp=CalculateSignature();

		long h=SimpleHash(temp);
		g_ExtInstancePtr->Out("%s\nHash: %x\n",temp.c_str(), h);
		writebuf(file,(void*)&h,sizeof(h));
		// start the loop
		TypeTable::const_iterator ti = typeT.begin();
		while(ti!=typeT.end())
		{
			// Method Table
			DWORD_PTR mt=ti->second;
			writebuf(file,(void*)&mt,sizeof(mt));
			std::string typestr = localCW2A(mtT[ti->second].typeName.c_str());
			// type size (DWORD) / type name (char*) without 0
			writestr(file, typestr);
			// size of objects
			UINT64 size=mtT[ti->second].size;
			writebuf(file,(void*)&size,sizeof(size));
			// number of objects
			DWORD items = static_cast<DWORD>(mtT[ti->second].Addresses.size());
			writebuf(file,(void*)&items,sizeof(items));
			// pointers
			DWORD_PTR* addrs=&mtT[ti->second].Addresses[0];
			writebuf(file,(void*)addrs, sizeof(addrs)*items);
			ti++;
		}

		fclose(file);
		return true;
	} catch(std::exception& e)
	{
		fclose(file);
		g_ExtInstancePtr->Err("Failed to save file %s\n%s\n", FileName.c_str(), e.what());
	}

clean:
	fclose(file);
	return false;
}
bool Indexer::LoadIndex(std::string FileName, bool IgnoreSignature)
{


	FILE *file = fopen(FileName.c_str(), "rb");
	if(!file)
	{
		g_ExtInstancePtr->Out("Failed to open the file %s\nVerify if the name is valid and you have write rights to the folder\n", FileName.c_str());
		return false;
	}
	try
	{
		// signature
		std::string sig;
		readstr(file,sig);
		if(sig!="RHV")
		{
			g_ExtInstancePtr->Out("File %s is not a valid index file\n", FileName.c_str());
			return false;
		}
		// Pointer size
		BYTE w=0;
		readbuf(file, &w,sizeof(w));
		if(w!=(BYTE)g_ExtInstancePtr->m_PtrSize)
		{
			g_ExtInstancePtr->Out("Index file %s contain invalid pointer size (%i)\n", FileName.c_str(), (int)w);
			return false;
		}

		// Hash
		long h=0;
		readbuf(file,&h,sizeof(h));

		std::string temp=CalculateSignature();
		long h1=SimpleHash(temp);
		//g_ExtInstancePtr->Out("%s\nHash: %x\n",temp.c_str(), h1);

		if(!IgnoreSignature && (h!=h1))
		{
			g_ExtInstancePtr->Out("The index file %s does not match the current heap state\nFound %x <> Expected %x\nUse -ignorestate if you believe this is an error\n", FileName.c_str(), h, h1);
			return false;
		}

		// start the loop
		isValid=false;
		signature="";
		mtT.clear();
		typeT.clear();
		while(true)
		{
			// Method Table
			DWORD_PTR mt=NULL;
			if(!ReadBuffer(file,&mt,sizeof(mt)))
				break;

			std::string typestr;
			// type size (DWORD) / type name (char*) without 0
			readstr(file, typestr);
			std::wstring wtypestr = localCA2W((LPSTR)typestr.c_str());
			// size of objects
			UINT64 size=0;
			readbuf(file,&size,sizeof(size));
			typeT.insert(pair<std::wstring,DWORD_PTR>(wtypestr,mt));
			// number of objects
			DWORD items = 0;
			readbuf(file,&items,sizeof(items));
			mtT[mt].size = size;
			mtT[mt].typeName = wtypestr;
			AddressList addrs(items,NULL);
			// pointers
			DWORD_PTR* paddr=&addrs[0];
			readbuf(file,(void*)paddr, sizeof(paddr)*items);
			mtT[mt].Addresses = addrs;
		}

		fclose(file);
		signature=temp;
		isValid=true;
		return true;
	} catch(std::exception& e)
	{
		fclose(file);
		g_ExtInstancePtr->Err("Failed to load file %s\n%s\n", FileName.c_str(), e.what());
	}

clean:
	fclose(file);
	return false;
}

bool Indexer::WalkHeapAndCache(Indexer *idx, CLRDATA_ADDRESS Start, CLRDATA_ADDRESS End, bool IsLarge, CLRDATA_ADDRESS Minimum, CLRDATA_ADDRESS Maximum, DumpHeapCache* Cache, bool quiet)
{
	CLRDATA_ADDRESS objAddr = Start;

	while(objAddr < End)
	{
		if(count % 1000000 == 0)
		{
			if((!quiet) && (count>0)) g_ExtInstancePtr->Out("%I64u objects...\n", count );

			if(IsInterrupted())
			{
				return false;
			}
		}
		//if(i>=19000000) return false;

		MD_TypeData obj;
		DWORD size;
		//obj = cache.
		obj = Cache->GetObj(objAddr);
		idx->AddAddress(objAddr, obj.MethodTable, &obj);
		size=static_cast<DWORD>(obj.size);
		if(obj.MethodTable == 0)
		{
			//if(!quiet) g_ExtInstancePtr->Out("Bad object: %p\n", objAddr);
			//Thread::const_iterator td = threads.GetThreadByAddress(objAddr);
			//if(td != threads.end())
			//{
			//	objAddr = td->AllocationLimit; //move to the end of thread allocation area
			//}
			size = 0;
			//size=sizeof(void*)*3; // avoid infinite loop
		//} else
		//{
		//	if(quiet)
		//	{
		//		g_ExtInstancePtr->Out("%p\n",objAddr);
		//	}
		//	count++;
		}
		//if(size==0) size=sizeof(void*)*3; // double check (should not happen)
		if(IsLarge)
			objAddr += AlignLarge(size == 0 ? g_ExtInstancePtr->m_PtrSize*3 : size );
		else
			objAddr += Align(size == 0 ? g_ExtInstancePtr->m_PtrSize*3 : size );
	}
	//g_ExtInstancePtr->Out("%I64u objects %f%% complete...\n", i, 100.0*(double)(objAddr-Minimum) / (double)(Maximum - Minimum));

	//if(!quiet) g_ExtInstancePtr->Out("%u Objects on this Area\n", i);
	return true;
}

bool DisplayHeapEnum(MatchingAddresses& Addresses, bool Short, UINT Top)
{
	if(Addresses.size()==0)
		return true;
	AddressEnum enumAdd;
	enumAdd.Start(Addresses);
	CLRDATA_ADDRESS objAddr;
	UINT32 i=0;
	if(!Short)
	{
		if(g_ExtInstancePtr->m_PtrSize == 4)
			g_ExtInstancePtr->Out("Address   MT         Size Heap Gen Type Name\n");
		else
			g_ExtInstancePtr->Out("Address          MT                  Size Heap Gen Type Name\n");
	}
	while(objAddr = enumAdd.GetNext())
	{
		if(IsInterrupted())
		{
			return false;
		}

		int h,g;


		ObjDetail obj(objAddr);
		h=obj.Heap();
		g=obj.Gen();
		i++;
		if(Short)
		{
			g_ExtInstancePtr->Out("%p\n", objAddr);
		} else
		{
			g_ExtInstancePtr->Dml("<link cmd=\"!wdo %p\">%p</link>", objAddr, objAddr);
			g_ExtInstancePtr->Out(" %p %8u %3i %3i %S\n", obj.MethodTable(), obj.Size(), h, g, obj.TypeName().c_str());
		}
		// this will ignore Top = 0 which means infinite
		if(i == Top)
		{
			if(!Short) g_ExtInstancePtr->Out("\n*** Top %u Objects listed. No more objects will be shown ***\n", i);
			break;
		}
	}
	if(!Short && Top == 0)
		g_ExtInstancePtr->Out("\n%u Objects listed\n", i);
	return true;
}

#include "CLRUtilities.h"

void EXT_CLASS::Uninitialize()
{
	if(indc)
	{
		delete indc;
		indc=NULL;
	}

	pAct = NULL;
	pRuntime = NULL;
	pHeap = NULL;
	pRuntimeInfo = NULL;
	pTarget = NULL;
	BOOL released = true;
	/*
	if(hDll)
	{
		released = FreeLibrary(hDll);
		if(!released)
			g_ExtInstancePtr->Out("Unable to release NetExtShim.dll\n");
		else
			hDll = NULL;
	}
	*/
	CLRUtilities reflection(L"NetExtShim");

	HRESULT hr = reflection.UnloadDomainByName();

	::CoUninitialize();
}
