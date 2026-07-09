/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#pragma once

#include "CLRHelper.h"

WDBGEXTS_CLR_DATA_INTERFACE Query;


MTFieldsMap typesMap;
MTList mtList;
std::vector<std::string> typeList;

size_t Align (size_t nbytes)
{
	return (nbytes + ALIGNCONST) & ~ALIGNCONST;
}

size_t AlignLarge(size_t nbytes)
{
	return (nbytes + ALIGNCONSTLARGE) & ~ALIGNCONSTLARGE;
}

std::wstring GetMethodName(CLRDATA_ADDRESS MethodTable)
{
	if(pRuntime && (MethodTable != NULL))
	{
		CComPtr<IMDType> type;

		if(pRuntime->GetTypeByMT(MethodTable, &type) == S_OK)
		{
			CComBSTR typeName;
			if(type->GetName(&typeName) == S_OK)
			{
				std::wstring tmpString = ToWString(typeName);
				return tmpString;
			}
		}
	}
	return L"";
};
std::wstring GetMethodDesc(CLRDATA_ADDRESS MethodDesc)
{
	if(pRuntime && (MethodDesc != NULL))
	{
		CComBSTR methodName;;
		if(pRuntime->GetMethodNameByMD(MethodDesc, &methodName) == S_OK)
		{
			std::wstring tmpString = ToWString(methodName);
			return tmpString;
		}
	}
	return L"";
}

bool GetModuleInterfaceFromMT(IMetaDataImport **MetaInterface, CLRDATA_ADDRESS MethodTable)
{
	CComPtr<IMDType> type;
	if(pRuntime->GetTypeByMT(MethodTable, &type) == S_OK)
	{
		if(type->GetIMetadata((IUnknown**)MetaInterface) == S_OK)
		{
			return true;
		}
	}
	return false;

}

bool GetModuleInterfaceFromModule(IMetaDataImport **MetaInterface, CLRDATA_ADDRESS ModuleAddress)
{
	return false;
	//IXCLRDataModule *module;
	//
	//DacpModuleData mod;
	//if(FAILED(sosData->GetModule(ModuleAddress, &module))) return false;
	//
	//HRESULT hr = module->QueryInterface(IID_IMetaDataImport, (LPVOID *) MetaInterface);
	//if(module) module->Release();
	//return SUCCEEDED(hr);
}

CLRDATA_ADDRESS MTFromEEClass(CLRDATA_ADDRESS cl)
{
	return NULL;

	//CLRDATA_ADDRESS addr;
	//if(SUCCEEDED(sosData->GetMethodTableForEEClass(cl, &addr)))
	//	return addr;
	//else
	//	return NULL;

}

bool IsMinidump()
{
	ULONG cls, qual, flags = 0;
	g_ExtInstancePtr->m_Control4->GetDebuggeeType(&cls, &qual);
	if(cls == DEBUG_USER_WINDOWS_SMALL_DUMP || qual == DEBUG_KERNEL_IDNA || qual == DEBUG_USER_WINDOWS_IDNA)
	{
		g_ExtInstancePtr->m_Control4->GetDumpFormatFlags(&flags);
		return (flags & DEBUG_FORMAT_USER_SMALL_FULL_MEMORY) == 0;
	}
	return false;
	
}

bool IsiDNA()
{
	ULONG cls, qlf;
	g_ExtInstancePtr->m_Control4->GetDebuggeeType(&cls, &qlf);
	return (qlf == DEBUG_KERNEL_IDNA || qlf == DEBUG_USER_WINDOWS_IDNA);
}

bool IsValidMemory(CLRDATA_ADDRESS Address, MEMORY_BASIC_INFORMATION64& MemInfo, INT64 Size)
{
	static bool isMini = false;

	if(isMini == false && IsMinidump())
	{
		g_ExtInstancePtr->Out("*WARNING*: This is a minidump. It will not test whether memory is valid or not. This message is only shown once.\n");
		isMini = true;
	}

	//
	// For mini-dump only consider if the memory is above 64K
	//
	if(isMini && Address > 64*1024)
	{
		return true;
	}

	if (!(Address >= MemInfo.BaseAddress && Address < (MemInfo.BaseAddress + MemInfo.RegionSize)))
		return false;
	if (0 == Size)
	{
		return true;
	}
	ZeroMemory(&MemInfo, sizeof(MemInfo));
	g_ExtInstancePtr->m_Data4->QueryVirtual(Address + Size, &MemInfo);
#ifdef _X86_
	MemInfo.BaseAddress = MemInfo.BaseAddress & UINT32_MAX;
	MemInfo.AllocationBase = MemInfo.AllocationBase  & UINT32_MAX;
#endif
	return IsValidMemory(Address + Size, MemInfo);
}

bool IsValidMemory(CLRDATA_ADDRESS Address, INT64 Size)
{
	MEMORY_BASIC_INFORMATION64 mi;
	ZeroMemory(&mi, sizeof(mi));
	g_ExtInstancePtr->m_Data4->QueryVirtual(Address, &mi);
#ifdef _X86_
	mi.BaseAddress = mi.BaseAddress & UINT32_MAX;
	mi.AllocationBase = mi.AllocationBase  & UINT32_MAX;
#endif
	return IsValidMemory(Address, mi, Size);
}

SimpleCache<CLRDATA_ADDRESS, vector<FieldStore>> fieldsCache;

#pragma region FieldStore

void FieldStore::Create(const NetExtShim::MD_FieldData &FieldData, mdTypeDef Token, const std::wstring& MethodName, const std::wstring& ResolvedFieldName)
{
	FieldDesc = FieldData;
	TokenClass = Token;
	mtName = MethodName;
	FieldName = ResolvedFieldName;
}

#pragma endregion

#pragma region SimpleCache

template<class I, class T>
SimpleCache<I,T>::SimpleCache()
{
	maxSize = 100000;
}

template<class I, class T>
void SimpleCache<I,T>::SetMaxSize(const UINT Size)
{
	if(Size > 100000)
		maxSize = 100000;
	else
		maxSize = Size;
}

template<class I, class T>
UINT SimpleCache<I,T>::GetMaxSize()
{
	return maxSize;
}

template<class I, class T>
bool SimpleCache<I,T>::Exists(const I &Index)
{
	return(cacheMap.find(Index) != cacheMap.end());
}

template<class I, class T>
const T* SimpleCache<I,T>::Get(const I &Index)
{
	typename std::map<I,T>::const_iterator it;
	it=cacheMap.find(Index);
	if(it==cacheMap.end())
		return NULL;
	return &it->second;
}

template<class I, class T>
UINT SimpleCache<I,T>::Size()
{
	return cacheMap.size();
}

template<class I, class T>
bool SimpleCache<I,T>::Set(const I& Index, const T& Content)
{
	if(Exists(Index))
		return false;
	bool isCacheFull = false;
	if(cacheMap.size() >= maxSize)
	{
		cacheMap.erase((*timeList.begin()).second);
		timeList.erase(timeList.begin());
		isCacheFull = true;
	}
	cacheMap[Index]=Content;
	timeList.insert(pair<time_t, I>(time(NULL),  Index));
	return isCacheFull;
}

#pragma endregion



#pragma region EEClass

EEClass::EEClass()
{
	isValidClass = false;
	IsCacheHit = false;
}

void EEClass::GetArraySizeByMT(CLRDATA_ADDRESS MethodTable, CLRDATA_ADDRESS *BaseSize, CLRDATA_ADDRESS *ComponentSize)
{
	pRuntime->GetArraySizeByMT(MethodTable, BaseSize, ComponentSize);
	return;
}
std::wstring EEClass::GetNameForMT(CLRDATA_ADDRESS MethodTable)
{
	CComBSTR str;
	std::wstring strTemp;
	pRuntime->GetNameForMT(MethodTable, &str);
	if(str != NULL)
		strTemp = str;
	return strTemp;
}


std::wstring EEClass::GetEnumString(CLRDATA_ADDRESS& Address)
{
	CComBSTR enumName;
	wstring enumStr;
	if(type->GetEnumName(Address, &enumName) == S_OK)
	{
		enumStr = enumName;
	}
	return enumStr;
}

void EEClass::Request(CLRDATA_ADDRESS MTOfType)
{
	ZeroMemory(&eeClassData, sizeof(MD_TypeData));
	fields.clear();
	chain.clear();
	isValidClass = false;
	type = NULL;
	addr=0;
	if(!pRuntime || pRuntime->GetTypeByMT(MTOfType, &type) != S_OK)
	{
		isValidClass = false;
		return;
	}
	MT=MTOfType;
	if(type->GetHeader(0,&eeClassData) != S_OK || eeClassData.MethodTable == 0)
	{
		isValidClass = false;
		return;
	}

	isValidClass = true;
}

void EEClass::Request(ObjDetail &Obj)	
{
	ZeroMemory(&eeClassData, sizeof(MD_TypeData));
	type = NULL;
	fields.clear();
	chain.clear();
	isValidClass = false;

	addr=Obj.Address();
	eeClassData = Obj.TypeData();
	if(!pRuntime || pRuntime->GetTypeByMT(Obj.MethodTable(), &type) != S_OK || eeClassData.MethodTable == 0)
	{
		isValidClass = false;
		return;
	}
	isValidClass = true;
}


void EEClass::EnsureFields()
{
	if(fields.empty())
	{
		IsCacheHit = fieldsCache.Exists(eeClassData.EEClass);
		if(IsCacheHit)
		{
			fields = *fieldsCache.Get(eeClassData.EEClass);
			return;
		}

		if(!type)
			return;

		long count;
		if(type->GetAllFieldsDataRawCount(&count) != S_OK || count < 0 || count > 1000)
		{
			return;
		}
		MD_FieldData *rawFields = NULL;
		HRESULT hr=S_OK;
		try
		{
			rawFields = new MD_FieldData[count];
			if(rawFields)
			{
				ZeroMemory(rawFields, count*sizeof(MD_FieldData));
				long i;
				hr=type->GetAllFieldsDataRaw(0, count, rawFields, &i);
			} else
			{
				hr=E_FAIL;
			}
		}
		catch(...)
		{
			if(rawFields)
			{
				delete[] rawFields;
				return;
			}
		}
		for(int i=0;i<count;i++)
		{
			if(IsInterrupted() || hr != S_OK)
			{
				if(rawFields) delete[] rawFields;
				isValidClass = false;
				return;
			}
			FieldStore store;
			CComBSTR typeName;
			CComBSTR fieldName;
			hr=type->GetRawFieldTypeAndName(i, &typeName, &fieldName);
			if(hr == S_OK)
			{
				std::wstring fieldStr = ToWString(fieldName);
				std::wstring typeStr = ToWString(typeName);
				store.Create(rawFields[i],rawFields[i].token, typeStr, fieldStr); 

				fields.push_back(store);
			}
		}
		if(rawFields) delete[] rawFields;
		// if it got here it is not cached
		fieldsCache.Set(eeClassData.EEClass, fields);
	}
}

bool EEClass::Implement(std::wstring TypeName, bool IncludeInterface)
{
	if(eeClassData.IsFree != 0 || !IsValidClass())
	{
		return false;
	}
	std::map<std::wstring,CLRDATA_ADDRESS> chain = Chain(IncludeInterface);
	std::map<std::wstring,CLRDATA_ADDRESS>::const_iterator it=chain.begin();
	while(it!=chain.end())
	{
		if(IsInterrupted())
			return false;
		if(g_ExtInstancePtr->MatchPattern(localCW2A(it->first.c_str()).c_str(), localCW2A(TypeName.c_str()).c_str()))
		{
			return true;
		}
		it++;
	}
	return false;
}

std::map<std::wstring,CLRDATA_ADDRESS> EEClass::Chain(bool IncludeInterface)
{
	if(chain.size() > 0) return chain;

	if(IncludeInterface)
	{
		CComPtr<IMDInterfaceEnum> enumInt;
		if(type->EnumerateInterfaces(&enumInt) == S_OK)
		{

			bool next = false;
			int i=0;
			do
			{
				if(IsInterrupted())
					return chain;

				CComPtr<IMDInterface> intF;
				next = enumInt->Next(&intF) != 0;

				if(next)
				{

					CComBSTR name;
					intF->GetName(&name);
					std::wstring tmpStr = ToWString(name);
					chain.emplace(tmpStr, 0);
					if(mtStr.size() > 0)
						mtStr.append(L" ");
					swprintf_s(NameBuffer, MAX_MTNAME, L"%p", 0);
					mtStr.append(NameBuffer);
					if(typeStr.size() > 0)
						typeStr.append(L" ");
					typeStr.append(tmpStr);

					intF = NULL;

				}
				if(i++ > 30) next = false; // avoid infinite loop
			} while(next);
		}
	}
	std::wstring tempStr;
	CComBSTR typeName;
	type->GetName(&typeName);
	tempStr = typeName;
	chain.emplace(tempStr, MT);

	CComPtr<IMDType> parent;
	HRESULT hr = type->GetBaseType(&parent);
	int i=0;
	while(parent && hr==S_OK && i<64)
	{
		i++; // Avoid infinite loop
		CComBSTR typeName;
		if(IsInterrupted())
			return chain;
		hr = parent->GetName(&typeName);

		MD_TypeData td;
		if(hr == S_OK && typeName)
		{
			tempStr = typeName;

			hr = parent->GetHeader(0, &td);
		} else
		{
			return chain;
		}
		chain.emplace(tempStr, td.MethodTable);
		if(mtStr.size() > 0)
			mtStr.append(L" ");
		swprintf_s(NameBuffer, MAX_MTNAME, L"%p", td.MethodTable);
		mtStr.append(NameBuffer);
		if(typeStr.size() > 0)
			typeStr.append(L" ");
		typeStr.append(tempStr);

		if(tempStr == L"System.Object")
		{
			return chain;
		}

		if(hr == S_OK)
		{
			parent = NULL;
			if(td.parentMT == 0)
			{
				hr=S_FALSE;
			} else
			{
				hr = pRuntime->GetTypeByMT(td.parentMT, &parent);
			}
		}
	}
	return chain;
}

std::wstring EEClass::ChainTypeStr(bool IncludeInterface)
{
	if(chain.size() == 0) Chain(IncludeInterface);
	return typeStr;
}

std::wstring EEClass::ChainMTtr(bool IncludeInterface)
{
	if(chain.size() == 0) Chain(IncludeInterface);
	return mtStr;
}

std::vector<FieldStore> EEClass::Fields()
{
	EnsureFields();
	return fields;
}

std::vector<FieldStore> EEClass::FieldsByName(std::string Name)
{
	EnsureFields();
	std::vector<FieldStore> retFields;
	for(int i=0; i<fields.size(); i++)
	{
		if(g_ExtInstancePtr->MatchPattern(CW2A(fields[i].FieldName.c_str()), Name.c_str()))
		{
			retFields.push_back(fields[i]);
		}
	}
	return retFields;
}

std::vector<FieldStore> EEClass::FieldsByType(std::wstring TypeName)
{
	std::string tname(CW2A(TypeName.c_str()));
	return FieldsByType(tname);
}

std::vector<FieldStore> EEClass::FieldsByType(std::string TypeName)
{
	EnsureFields();
	std::vector<FieldStore> retFields;
	for(int i=0; i<fields.size(); i++)
	{
		if(g_ExtInstancePtr->MatchPattern(CW2A(fields[i].mtName.c_str()), TypeName.c_str()))
		{
			retFields.push_back(fields[i]);
		}
	}
	return retFields;
}

std::vector<FieldStore> EEClass::PointerFields()
{
	EnsureFields();
	std::vector<FieldStore> retFields;
	for(int i=0; i<fields.size(); i++)
	{
		if(fields[i].FieldDesc.corElementType != ELEMENT_TYPE_VALUETYPE  && fields[i].FieldDesc.corElementType >= ELEMENT_TYPE_STRING  )
		{
			retFields.push_back(fields[i]);
		}
	}
	return retFields;
}

#pragma endregion

//
//std::wstring GetMethodNameFromMT(CLRDATA_ADDRESS MT)
//{
//	DacpMethodTableData mtData;
//	mtData.Request(sosData, MT);
//	DacpEEClassData clData;
//	clData.Request(sosData,mtData.Class);
//	DacpMethodTableData::GetMethodTableName(clrData, clData.MethodTable,
//	mtData.Class;
//
//	IMDInternalImport *pImport = NULL;
//	if(FAILED(GetMDInternalFromImport(mi, &pImport)))
//		return L"<<INVALID>>";
//
//    ULONG nameLen;
//    LPCSTR *name; //= new char[MAX_MTNAME];
//	//pImport->GetNameOfTypeDef(Token,
//    LPCSTR *namesp; // = new char[MAX_MTNAME];
//    //ZeroMemory((void*)name, MAX_MTNAME);
//    //ZeroMemory((void*)namesp, MAX_MTNAME);
//
//
//    pImport->GetNameOfTypeDef(Token, name, namesp);
//	std::wstring fullname;
//	fullname.assign(CA2W(*namesp));
//	//fullname.assign(CA2W(pImport->GetNameOfMethodDef(Token)));
//	//fullname.append(L".");
//	//fullname.append(CA2W(*name));
//	/*
//	if(name)
//		delete[] name;
//	if(namesp)
//		delete[] namesp;
//		*/
//	if(pImport)
//		pImport->Release();
//
//
//	return fullname;
//
//};

int xyz=0;
/*
BOOL ReceiveTraverse(UINT clauseIndex,UINT totalClauses,DACEHInfo *pEHInfo,LPVOID token)
{
DACEHInfo *info = pEHInfo;

g_ExtInstancePtr->Out("%u//%u Objects %p: \n",clauseIndex, totalClauses, (LPVOID)token);
if(xyz++ > 5)
{
xyz=0;
return false;
}
return true;
}
*/

// Returns 0 (no-overlapping) 1 (region 2 is in region 1) -1 (region 1 is in region 2)
// -2 (part of region 1 and region 2 are common)
int IsOverlap(CLRDATA_ADDRESS Begin1, CLRDATA_ADDRESS End1, CLRDATA_ADDRESS Begin2, CLRDATA_ADDRESS End2)
{
	if(Begin2 >= Begin1 && End2 <= End1)
	{
		return 1;
	}
	if(Begin1 >= Begin2 && End1 <= End2)
	{
		return -1;
	}
	if((Begin2 >= Begin1 && Begin2 <= End1) ||
		(Begin1 >= Begin2 && Begin1 <= End2))
	{
		return -2;
	}
	return 0;
}

#pragma region Thread

ULONG Thread::GetOSThreadIDByAddress(CLRDATA_ADDRESS ThreadAddress)
{
	ULONG osThread = 0;
	pRuntime->GetOSThreadIDByAddress(ThreadAddress, &osThread);
	return osThread;
}

bool sortByAllocation(const MD_ThreadData &lhs, const MD_ThreadData &rhs) { return lhs.AllocationStart < rhs.AllocationStart; }

bool Thread::Request(bool IsOrdered)
{
	if(threads.size() > 0)
		return true;
	if(pRuntime)
	{
		CComPtr<IMDThreadEnum> threadsEnum;
		if(pRuntime->EnumerateThreads(&threadsEnum) != S_OK)
		{
			return false;
		}
		HRESULT hr=S_OK;
		int i=0;
		while(hr== S_OK && !IsInterrupted() && i < 100000)
		{
			i++;
			CComPtr<IMDThread> thread;
			hr=threadsEnum->Next(&thread);
			if(hr==S_OK && thread)
			{
				MD_ThreadData threadData;
				if(thread->GetThreadData(&threadData) == S_OK)
				{
					threads.push_back(threadData);
				}
			} else
			{
				hr=E_FAIL;
			}
		}


	}
	if(IsOrdered) std::sort(threads.begin(), threads.end(), sortByAllocation);
	return true;
}

Thread::const_iterator Thread::GetThreadByAddress(CLRDATA_ADDRESS Address)
{
	for(const_iterator it = threads.begin();it!=threads.end(); it++)
	{
		if(Address == it->Address)
		{
			return it;
		}
	}

	return end();
}

Thread::const_iterator Thread::GetThreadInRange(CLRDATA_ADDRESS Begin, CLRDATA_ADDRESS End)
{
	for(const_iterator it = threads.begin();it!=threads.end(); it++)
	{
		if(IsOverlap(Begin, End, it->AllocationStart, it->AllocationLimit))
		{
			return it;
		}
	}

	return end();
}

const UINT32 Thread::Size()
{
	return static_cast<UINT32>(threads.size());
}
const NetExtShim::MD_ThreadData* Thread::operator[](UINT32 i)
{
	if(i>=threads.size())
	{
		return NULL;
	}
	return &threads.at(i);
}



Thread::const_iterator Thread::begin()
{
	return threads.begin();
}

Thread::const_iterator Thread::end()
{
	return threads.end();
}


#pragma endregion


bool wasInterrupted;

BOOL IsInterrupted()
{
	if(wasInterrupted)
		return true;
	if((g_ExtInstancePtr->m_Control->GetInterrupt() == S_OK) || CheckControlC() == TRUE)
	{
		wasInterrupted = true;
		g_ExtInstancePtr->Out("\nInterrupted by user\n");
		return true;
	}
	return false;
}

UINT64 countH;

const char rootKind[][20]={"Static Var", "Thread Static Var", "Local Var", "Strong", "Weak", "Pinning", "Finalizer", "Async Pinning"};
BOOL TraverseHandle(CLRDATA_ADDRESS HandleAddr,CLRDATA_ADDRESS HandleValue,int HandleType, CLRDATA_ADDRESS appDomainPtr,LPVOID token)
{
	ObjDetail obj(HandleValue);
	if(!obj.IsValid())
	{
		g_ExtInstancePtr->Out("Bad Object: %p\n", HandleValue);
		return true;
	}
	/*
	g_ExtInstancePtr->Out("%u: HAddr: %p - HV: %p, HT: %i, AD: %p, Token %u Type: %S\ MT: %p\n", countH++, HandleAddr, HandleValue,
	HandleType, appDomainPtr, (mdToken*)token, obj.TypeName().c_str(), obj.MethodTable()
	);
	*/
#ifndef _X86_
	g_ExtInstancePtr->Out("%5u: %p poi(%p) %8u", countH++, (DWORD_PTR)HandleValue, (DWORD_PTR)HandleAddr, obj.Size());
#else
	g_ExtInstancePtr->Out("%5u: %p poi(%p) %8u", countH++, (DWORD_PTR)HandleValue, (DWORD_PTR)HandleAddr, obj.Size());
#endif
	g_ExtInstancePtr->Out(" %S - %s", obj.TypeName().c_str(), (HandleType >=0 && HandleType <= 7) ? rootKind[HandleType] : "Invalid");
	g_ExtInstancePtr->Out("\n");

	if(IsInterrupted())
	{
		g_ExtInstancePtr->Out("Interrupted by user\n");
		return false;
	}
	/*
	if(xyz++ > 200)
	{
	xyz=0;
	return false;
	}
	*/
	return true;
}

BOOL TraverseHandle4(CLRDATA_ADDRESS HandleAddr, CLRDATA_ADDRESS HandleValue, int HandleType,
					 ULONG ulRefCount, CLRDATA_ADDRESS appDomainPtr, CLRDATA_ADDRESS Target)
{
	ObjDetail obj(HandleValue);
	if(!obj.IsValid())
	{
		g_ExtInstancePtr->Out("Bad Object: %p\n", HandleValue);
		return true;
	}

	/*
	g_ExtInstancePtr->Out("%u: HAddr: %p - HV: %p, HT: %i, AD: %p, Token %u Type: %S\ MT: %p\n", countH++, HandleAddr, HandleValue,
	HandleType, appDomainPtr, (mdToken*)token, obj.TypeName().c_str(), obj.MethodTable()
	);
	*/
	g_ExtInstancePtr->Out("%5u: %p poi(%p) ", (UINT32)countH++, obj.Address(), (DWORD_PTR)HandleAddr);

	//g_ExtInstancePtr->Out("->%p ", Target);
	g_ExtInstancePtr->Out("%8u", obj.Size());

	g_ExtInstancePtr->Out(" %S (refs:%u) - %s", obj.TypeName().c_str(), ulRefCount, (HandleType >=0 && HandleType <= 7) ? rootKind[HandleType] : "Invalid");
	g_ExtInstancePtr->Out("\n");

	if(IsInterrupted())
	{
		g_ExtInstancePtr->Out("Interrupted by user\n");
		return false;
	}
	/*
	if(xyz++ > 200)
	{
	xyz=0;
	return false;
	}
	*/
	return true;
}

std::string TypeString(CorElementType CorType)
{
	switch(CorType) {
	case ELEMENT_TYPE_VOID          :
		return "void";
	case ELEMENT_TYPE_BOOLEAN       :
		return "bool";
	case ELEMENT_TYPE_CHAR          :
		return "char";
	case ELEMENT_TYPE_I1            :
		return "int8";
	case ELEMENT_TYPE_U1            :
		return "uint8";
	case ELEMENT_TYPE_I2            :
		return "int16";
	case ELEMENT_TYPE_U2            :
		return "uint16";
	case ELEMENT_TYPE_I4            :
		return "int32";
	case ELEMENT_TYPE_U4            :
		return "uint32";
	case ELEMENT_TYPE_I8            :
		return "int64";
	case ELEMENT_TYPE_U8            :
		return "uint64";
	case ELEMENT_TYPE_R4            :
		return "float32";
	case ELEMENT_TYPE_R8            :
		return "float64";
	case ELEMENT_TYPE_U             :
		return "native uint";
	case ELEMENT_TYPE_I             :
		return "native int";
		//	case ELEMENT_TYPE_R             :
		//		str = "native float"; goto APPEND;
	case ELEMENT_TYPE_OBJECT        :
		return "object";
	case ELEMENT_TYPE_STRING        :
		return "string";
	case ELEMENT_TYPE_TYPEDBYREF    :
		return "typedref";
	case ELEMENT_TYPE_VALUETYPE    :
		return "valuetype ";
	case ELEMENT_TYPE_CLASS         :
		return "class";
	case ELEMENT_TYPE_SZARRAY    :
		return "szarray[]";
	case ELEMENT_TYPE_ARRAY       :
		return "array[]";
	case ELEMENT_TYPE_VAR        :
		return "var";

	case ELEMENT_TYPE_MVAR        :
		return "mvar";
	case ELEMENT_TYPE_FNPTR :
		return "method";

	case ELEMENT_TYPE_GENERICINST :
		return "generic";
	case ELEMENT_TYPE_PINNED	:
		return " pinned";
	case ELEMENT_TYPE_PTR           :
		return "(ptr*)";
	case ELEMENT_TYPE_BYREF         :
		return "byref&";
	default:
	case ELEMENT_TYPE_SENTINEL      :
	case ELEMENT_TYPE_END           :
		return "Unknown";
	} // end switch
}


namespace qi = boost::spirit::qi;
namespace ascii = boost::spirit::ascii;

enum FieldEnum
{
	WOBJ_SIZE,
	WOBJ_TYPE,
	WOBJ_ADDR,
	WOBJ_MT,
	WOBJ_MTOFPARENT,
	WOBJ_CHAIN,
	WOBJ_CHAINMT,
	WOBJ_RANK,
	WOBJ_ITEMS,
	WOBJ_ISSTRING,
	WOBJ_ISFREE,
	WOBJ_INSTFIELDS,
	WOBJ_CONSTFIELDS,
	WOBJ_ISVALID,
	WOBJ_ISINSTANCE,
	WOBJ_ISVALUE,
	WOBJ_ISGENERIC,
	WOBJ_ISINNERCLASS,
	WOBJ_ISINNERVALUE,
	WOBJ_ISINNERGENERIC,
	WOBJ_HEAP,
	WOBJ_GEN,
	WOBJ_DOMAIN,
	WOBJ_MODULE,
	WOBJ_MODULENAME,
	WFLD_TYPE = 100,
	WFLD_TOKEN,
	WFLD_OFFSET,
	WFLD_ISVALUE,
	WFLD_ISINSTANCE,
	WFLD_NAME,
	WFLD_BASESIZE,
	WFLD_SIZE,
	WFLD_ISSTATIC
};

qi::symbols<const char, const FieldEnum> fieldSymbols;

void FieldsFill()
{
	if(fieldSymbols.at("$objsize") == WOBJ_SIZE) return;
	fieldSymbols.add
		("$objsize", WOBJ_SIZE)
		("$objtype", WOBJ_TYPE)
		("$objaddr", WOBJ_ADDR)
		("$objmt", WOBJ_MT)
		("$mtofparent", WOBJ_MTOFPARENT)
		("$objchain", WOBJ_CHAIN)
		("$objchainmt", WOBJ_CHAINMT)
		("$objrank", WOBJ_RANK)
		("$objitems", WOBJ_ITEMS)
		("$isstring", WOBJ_ISSTRING)
		("$objisfree", WOBJ_ISFREE)
		("$objinstfields", WOBJ_INSTFIELDS)
		("$objconstfields", WOBJ_CONSTFIELDS)
		("$objisvalid", WOBJ_ISVALID)
		("$objisinstance", WOBJ_ISINSTANCE)
		("$objvalue", WOBJ_ISVALUE)
		("$objisgeneric", WOBJ_ISGENERIC)
		("$objinnerclass", WOBJ_ISINNERCLASS)
		("$objinnervalue", WOBJ_ISINNERVALUE)
		("$objisinnergeneric", WOBJ_ISINNERGENERIC)
		("$objheap", WOBJ_HEAP)
		("$objgen", WOBJ_GEN)
		("$objdomain", WOBJ_DOMAIN)
		("$objmodule", WOBJ_MODULE)
		("$objmodulename", WOBJ_MODULENAME)
		("$fldtype", WFLD_TYPE)
		("$fldtoken", WFLD_TOKEN)
		("$fldoffset", WFLD_OFFSET)
		("$fldvalue", WFLD_ISVALUE)
		("$fldisinstance", WFLD_ISINSTANCE)
		("$fldname", WFLD_NAME)
		("$fldbasesize", WFLD_BASESIZE)
		("$fldsize", WFLD_SIZE)
		("$fldisstatic", WFLD_ISSTATIC);
}
void VectorSplit(std::string Str, std::vector<string> &Vec)
{
	bool p=boost::spirit::qi::parse(Str.begin(), Str.end(),
		+(boost::spirit::qi::alnum | '_' | ':' | '.' | '[' | ']' | '*' | '?') % ',',
		Vec);
}



std::wstring ObjDetail::GetFieldValueByName(std::string FieldName, ObjDetail& Obj, MD_FieldData& FieldDesc)
{
	vector<string> fieldsDepth;
	bool p=boost::spirit::qi::parse(FieldName.begin(), FieldName.end(),
		+(boost::spirit::qi::alnum | '_' | ':' | '$' | '[' | ']' | '*' | '?') % '.' ,
		fieldsDepth);
	if(!p)
		return L"<<Invalid>>";

	//sqlparser::Split(FieldName, fieldsDepth);
	vector<string>::const_iterator it = fieldsDepth.begin();

	for(int i=0;i<fieldsDepth.size();i++)
	{
		std::vector<FieldStore> fields = Obj.GetFieldsByName(*it);
		if(fields.size() == 0)
		{
			g_ExtInstancePtr->Err("Inner field %s not found\n", *it);
			return L"<<INVALID>>\n";
		}
		if(fields.size() > 1)
		{
			g_ExtInstancePtr->Err("Level fields cannot contain wildcards: %s\n", *it);
			return L"<<INVALID>>\n";
		}
		MD_FieldData *field = &fields[0].FieldDesc;
		CLRDATA_ADDRESS addr;
		if(field->corElementType == ELEMENT_TYPE_PTR ||
			field->corElementType == ELEMENT_TYPE_BYREF ||
			field->corElementType == ELEMENT_TYPE_TYPEDBYREF ||
			field->corElementType == ELEMENT_TYPE_OBJECT ||
			field->corElementType == ELEMENT_TYPE_VOID ||
			field->corElementType == ELEMENT_TYPE_GENERICINST ||
			field->corElementType == ELEMENT_TYPE_CLASS)
		{
			addr = ObjDetail::GetPTR(ObjDetail::GetFieldAddress(Obj.Address(), *field, false, &Obj));
			if(addr == NULL)
			{
				FieldDesc = *field;
				return L"NULL";
			}
			Obj.Request(addr);
		}else
			if(field->corElementType == ELEMENT_TYPE_VALUETYPE)
			{
				addr = ObjDetail::GetFieldAddress(Obj.Address(), *field, true, &Obj);
				Obj.Request(addr, field->MethodTable);
			} else
			{
				if(i!=fieldsDepth.size() -1)
				{
					g_ExtInstancePtr->Err("Inner field %s should be OBJECT or CLASS\n", *it);
					return L"<<INVALID>>\n";
				}
			}
			if(i==fieldsDepth.size()-1)
			{
				FieldDesc = *field;
				return ObjDetail::ValueString(FieldDesc, Obj.Address(), Obj.IsValueType());
			}
			//this->classObj.Fields();
			it++;
	}
	return L"<<ERROR>>";
}

const WCHAR* PrintValue(CLRDATA_ADDRESS offset, CorElementType CorType, CLRDATA_ADDRESS MethodTable, const char* ObjectString, const char* ValueTypeString, bool Print)
{
	try
	{
		switch(CorType) {
		case ELEMENT_TYPE_PTR           :
			//return "(ptr*)";
		case ELEMENT_TYPE_GENERICINST :
			//return "generic";
		case ELEMENT_TYPE_CLASS         :
			//return "class";
		case ELEMENT_TYPE_BYREF         :
			//return "byref&";
		case ELEMENT_TYPE_TYPEDBYREF    :
			//return "typedref";
		case ELEMENT_TYPE_OBJECT        :
			//return "object";
		case ELEMENT_TYPE_VOID          :
			//return "void";
		case ELEMENT_TYPE_SZARRAY       :
		case ELEMENT_TYPE_ARRAY       :
			//return "array[]";
		case ELEMENT_TYPE_VAR			:
			//return "var";
		case ELEMENT_TYPE_MVAR			:
			//return "mvar";
		case ELEMENT_TYPE_FNPTR			:
			//return "method";
		case ELEMENT_TYPE_PINNED		:

			{
				ExtRemoteData ptr(offset, sizeof(void*));
				if(!ptr.m_ValidData || !ptr.m_ValidOffset)
				{
					swprintf(NameBuffer, MAX_MTNAME, L"NotInit");
					if(Print)
					{
						g_ExtInstancePtr->Out("%S", NameBuffer);
					}
					return NameBuffer;
				}
				CLRDATA_ADDRESS addr = ptr.GetPtr();
				swprintf(NameBuffer, MAX_MTNAME, L"%p", addr);
				if(Print)
				{
					g_ExtInstancePtr->Dml(ObjectString, addr, addr, addr);
				}
				return NameBuffer;
			}
			break;
		case ELEMENT_TYPE_BOOLEAN       :
			//return "bool";
			{
				ExtRemoteData ptr(offset, sizeof(BOOLEAN));
				USHORT u=ptr.GetBoolean();
				if(u>0)
				{
					swprintf(NameBuffer, MAX_MTNAME, L"%x (True)", u, u > 0);
				} else
				{
					swprintf(NameBuffer, MAX_MTNAME, L"%x (False)", u, u > 0);
				}
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
		case ELEMENT_TYPE_CHAR          :
			{
				ExtRemoteData ptr(offset, sizeof(WCHAR));
				WCHAR ch;
				ptr.ReadBuffer(&ch,sizeof(WCHAR));
				swprintf(NameBuffer, MAX_MTNAME, L"%C", ch);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
		case ELEMENT_TYPE_I1            :
			//return "int8";
			{
				ExtRemoteData ptr(offset, sizeof(BYTE));
				INT u=(INT)(BYTE)ptr.GetBoolean();

				swprintf(NameBuffer, MAX_MTNAME, L"0x%x (0n%i)", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
		case ELEMENT_TYPE_U1            :
			{
				ExtRemoteData ptr(offset, sizeof(BYTE));
				UINT u=(UINT)ptr.GetBoolean();
				swprintf(NameBuffer, MAX_MTNAME, L"0x%x (0n%u)", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
			//return "uint8";
		case ELEMENT_TYPE_I2            :
			{
				ExtRemoteData ptr(offset, sizeof(SHORT));
				INT u=(INT)ptr.GetShort();
				swprintf(NameBuffer, MAX_MTNAME, L"0x%x (0n%i)", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
			//return "int16";
		case ELEMENT_TYPE_U2            :
			{
				ExtRemoteData ptr(offset, sizeof(SHORT));
				UINT u=(UINT)ptr.GetShort();
				swprintf(NameBuffer, MAX_MTNAME, L"0x%x (0n%u)", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
			//return "uint16";
		case ELEMENT_TYPE_I4            :
#ifndef _WIN64
		case ELEMENT_TYPE_I             :
#endif
			{
				ExtRemoteData ptr(offset, sizeof(INT32));
				INT32 u=ptr.GetLong();
				swprintf(NameBuffer, MAX_MTNAME, L"%x (0n%i)", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
			//return "int16";
			//return "int32";
		case ELEMENT_TYPE_U4	:
#ifndef _WIN64
		case ELEMENT_TYPE_U     :
#endif
			{
				ExtRemoteData ptr(offset, sizeof(INT32));
				UINT32 u=ptr.GetUlong();
				swprintf(NameBuffer, MAX_MTNAME, L"%x (0n%u)", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
			//return "uint32";
		case ELEMENT_TYPE_I8            :
#ifdef _WIN64
		case ELEMENT_TYPE_I             :
#endif

			{
				ExtRemoteData ptr(offset, sizeof(INT64));
				INT64 u=ptr.GetLong64();
				swprintf(NameBuffer, MAX_MTNAME, L"%I64x (0n%I64i)", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
			//return "int64";
		case ELEMENT_TYPE_U8            :
#ifdef _WIN64
		case ELEMENT_TYPE_U     :
#endif

			{
				ExtRemoteData ptr(offset, sizeof(INT64));
				UINT64 u=ptr.GetUlong64();
				swprintf(NameBuffer, MAX_MTNAME, L"%I64x (0n%I64u)", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				return NameBuffer;
			}
			break;
		case ELEMENT_TYPE_STRING        :
			{
				ExtRemoteData ptr(offset, sizeof(void*));
				CLRDATA_ADDRESS p = ptr.GetPtr();

				if(Print)
				{
					g_ExtInstancePtr->Dml(ObjectString, p, p);
					g_ExtInstancePtr->Out(" %S", ObjDetail::String(p));
				}
				if(!p)
					swprintf(NameBuffer, MAX_MTNAME, L"NULL");
				else
					swprintf(NameBuffer, MAX_MTNAME, L"%S", ObjDetail::String(p));

				return NameBuffer;
			}
			break;
		case ELEMENT_TYPE_R4            :
			{
				ExtRemoteData ptr(offset, sizeof(float));
				float u=ptr.GetFloat();
				swprintf(NameBuffer, MAX_MTNAME, L"%f", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				return NameBuffer;
			}
			break;
		case ELEMENT_TYPE_R8            :
			{
				ExtRemoteData ptr(offset, sizeof(double));
				double u=ptr.GetDouble();
				swprintf(NameBuffer, MAX_MTNAME, L"%f", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				return NameBuffer;
			}
			break;
		case ELEMENT_TYPE_VALUETYPE    :
			{
				ExtRemoteData ptr(offset, sizeof(double));
				double u=ptr.GetDouble();
				swprintf(NameBuffer, MAX_MTNAME, L"-mt %p %p", MethodTable, offset); // return pointer to value type and the method table
				if(Print)
				{
					g_ExtInstancePtr->Dml(ValueTypeString, MethodTable, offset, offset);
				}
				return NameBuffer;
			}
			break;

			//return "string";

			//return "uint64";
			/*
			case ELEMENT_TYPE_U             :
			return "native uint";
			case ELEMENT_TYPE_I             :
			return "native int";
			//	case ELEMENT_TYPE_R             :
			//		str = "native float"; goto APPEND;
			case ELEMENT_TYPE_SZARRAY    :
			return "szarray[]";
			case ELEMENT_TYPE_ARRAY       :
			return "array[]";
			case ELEMENT_TYPE_VAR        :
			return "var";
			case ELEMENT_TYPE_MVAR        :
			return "mvar";
			case ELEMENT_TYPE_FNPTR :
			return "method";
			case ELEMENT_TYPE_PINNED	:
			return " pinned";
			*/
		default:
		case ELEMENT_TYPE_SENTINEL      :
		case ELEMENT_TYPE_END           :
			if(Print)
			{
				g_ExtInstancePtr->Out("Unknown");
			}
			return L"Unknown";
			break;
		} // end switch
	} catch(...)
	{
		swprintf(NameBuffer, MAX_MTNAME, L"<Unable to read at %p>", offset);
	}
	return NameBuffer;
}


MD_TypeData DumpHeapCache::GetObj(CLRDATA_ADDRESS Address)
{

	MD_TypeData obj;
	ZeroMemory(&obj, sizeof(obj));
	obj.MethodTable = GetPtr(Address) & ~(CLRDATA_ADDRESS)3;

	obj.arrayElementMT = GetPtr(Address+g_ExtInstancePtr->m_PtrSize * 2) & ~(CLRDATA_ADDRESS)3;

	obj.arraySize = GetPtr(Address+g_ExtInstancePtr->m_PtrSize) & ~(DWORD)0;
	// Fix a counting bug for string in .NET 4.0 and beyond
	if(obj.MethodTable == commonMTs.StringMethodTable() && (!NET2))
	{
			obj.arraySize++;

	}
	return obj;
};

void* DumpHeapCache::Read(CLRDATA_ADDRESS Address, UINT32 Size)
{
	ULONG read;
	if(Size > maxSize) return NULL;
	if(Address+Size >= address+maxSize || Address < address)
	{
		address = Address;
		ReadMemory(Address, memBuffer, maxSize, &read);
		size = (UINT32)read;
		//g_ExtInstancePtr->Out("Cache at: %p from %p to %p - Read: %u\n", memBuffer, Address, Address+maxSize, read);
	}
	// calculate offset
	void* offset = &memBuffer[Address-address];
	return offset;
};

DWORD_PTR DumpHeapCache::GetPtr(CLRDATA_ADDRESS Address)
{
	DWORD_PTR* ret = (DWORD_PTR*)Read(Address, sizeof(DWORD_PTR));
	if(ret == NULL) return NULL;
	return *ret;
};

