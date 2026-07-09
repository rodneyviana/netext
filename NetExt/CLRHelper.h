/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#pragma once
#ifndef _CLR_HELPER_
#define _CLR_HELPER_

/*
#include <boost/none.hpp>
#include <boost/config/warning_disable.hpp>
#include <boost/spirit/include/qi.hpp>
#include <boost/spirit/include/phoenix_core.hpp>
#include <boost/spirit/include/phoenix_operator.hpp>
#include <boost/spirit/include/phoenix_object.hpp>
#include <boost/fusion/include/adapt_struct.hpp>
#include <boost/fusion/include/io.hpp>
#include <boost/spirit/include/classic.hpp>
// #include <boost/spirit/core.hpp>
// #include <boost/spirit/symbols/symbols.hpp>
#include <iostream>
#include <boost/algorithm/string.hpp>
#include <boost/algorithm/string/replace.hpp>
#include <boost/algorithm/string/case_conv.hpp>
#include <boost/lexical_cast.hpp>

#include <iostream>
*/
#include "NetExt.h"
#include "boost.h"

// Add this typedef at the top of the file, after includes, to resolve ambiguity for 'byte'
#ifndef _BYTE_DEFINED
#define _BYTE_DEFINED
#undef byte
typedef unsigned char byte;
#endif

const WCHAR* PrintValue(CLRDATA_ADDRESS offset, CorElementType CorType, CLRDATA_ADDRESS MethodTable=0,const char* ObjectString = NULL, const char* ValueTypeString=NULL, bool Print = false );

int IsOverlap(CLRDATA_ADDRESS Begin1, CLRDATA_ADDRESS End1, CLRDATA_ADDRESS Begin2, CLRDATA_ADDRESS End2);
using namespace std;
using namespace NetExtShim;

CLRDATA_ADDRESS MTFromEEClass(CLRDATA_ADDRESS cl);



size_t Align (size_t nbytes);
size_t AlignLarge(size_t nbytes);
void VectorSplit(std::string Str, std::vector<string> &Vec);
std::wstring GetMethodName(CLRDATA_ADDRESS MethodTable);
std::wstring GetMethodDesc(CLRDATA_ADDRESS MethodDesc);
bool GetModuleInterfaceFromMT(IMetaDataImport **MetaInterface, CLRDATA_ADDRESS MethodTable);
bool GetModuleInterfaceFromModule(IMetaDataImport **MetaInterface, CLRDATA_ADDRESS ModuleAddress);

BOOL IsInterrupted();

#pragma region NativeModule

struct LANGUACODEPAGE
{
	unsigned int wLanguage;
	unsigned int wCodePage;

	void SetValue(uint8_t* Bytes, int Index = 0)
	{
		wLanguage = Bytes[Index] + ((unsigned int)(Bytes[Index + 1]) << 8);
		wCodePage = Bytes[Index + 2] + ((unsigned int)(Bytes[Index + 3]) << 8);
	}

	string VarQueryValue(string SubBlock)
	{
		char buffer[100] = { 0 };
		sprintf_s(buffer, 100, "\\StringFileInfo\\%04x%04x\\%s", wLanguage, wCodePage, SubBlock.c_str());
		string query(buffer);
		return query;
	}

};

struct ModuleVersion
{
	int Major;
	int Minor;
	int Build;
	int Revision;
};

class NativeModule
{

private:
	bool isValid;
	ULONG Index;
public:
	
	bool IsValid()
	{
		return isValid;
	}

	NativeModule(string ModuleName)
	{
		isValid = false;
		ULONG64 addr = 0;

		HRESULT hr = g_ExtInstancePtr->m_Symbols3->GetModuleByModuleName2(ModuleName.c_str(), 0, DEBUG_GETMOD_NO_UNLOADED_MODULES, &Index, &addr);
		if (SUCCEEDED(hr))
		{
			isValid = true;

		}

	}

	ModuleVersion GetVersionInfo()
	{
		ModuleVersion fi = { 0 };
		ULONG size = 1000;
		uint8_t buffer[1000] = { 0 };

		HRESULT hr = g_ExtInstancePtr->m_Symbols3->GetModuleVersionInformation(Index, 0, "\\VarFileInfo\\Translation", buffer, size, &size);

		if (SUCCEEDED(hr))
		{
			LANGUACODEPAGE cp = { 0 };
			cp.SetValue(buffer);

			hr = g_ExtInstancePtr->m_Symbols3->GetModuleVersionInformation(Index, 0, cp.VarQueryValue("ProductVersion").c_str(), buffer, 1000, &size);
			if (SUCCEEDED(hr))
			{
				string verStr((char*)&buffer);

				regex reg("(\\d+).(\\d+).(\\d+).(\\d+)");
				smatch matches;
				if (regex_search(verStr, matches, reg))
				{
					fi.Major = atoi(matches[1].str().c_str());
					fi.Minor = atoi(matches[2].str().c_str());
					fi.Revision = atoi(matches[3].str().c_str());
					fi.Build = atoi(matches[4].str().c_str());

				}
			}

			
		}
		return fi;
		
	}


};

#pragma endregion



class FieldStore
{
public:
	NetExtShim::MD_FieldData FieldDesc;
	mdTypeDef TokenClass;
	std::wstring mtName;
	std::wstring FieldName;

	void Create(const NetExtShim::MD_FieldData &FieldData, mdTypeDef Token, const std::wstring& MethodName, const std::wstring& ResolvedFieldName);
};




struct Compare
{
	bool operator() (const time_t& left, const time_t& right) const
	{
		return left < right;
	}
};

template<class I, class T>
class SimpleCache
{
private:
	std::map<I,T> cacheMap;
	std::multimap<time_t, I, Compare> timeList;
	UINT maxSize;
public:

	SimpleCache();
	void SetMaxSize(const UINT Size);
	UINT GetMaxSize();
	bool Exists(const I &Index);
	const T* Get(const I &Index);
	UINT Size();
	bool Set(const I& Index, const T& Content);
};



extern class ObjDetail;

class EEClass
{
private:
	CLRDATA_ADDRESS addr;
	bool isValidClass;

	vector<FieldStore> fields;
	int instFields;
	CComPtr<IMDType> type;

	CLRDATA_ADDRESS MT;
	std::map<std::wstring,CLRDATA_ADDRESS> chain;
	std::wstring typeStr;
	std::wstring mtStr;
public:
	MD_TypeData eeClassData;
	bool IsCacheHit;
	EEClass();
	void Request(CLRDATA_ADDRESS MTOfType);
	void Request(ObjDetail &Obj);
	void EnsureFields();
	bool Implement(std::wstring TypeName, bool IncludeInterface=false);
	std::map<std::wstring,CLRDATA_ADDRESS> Chain(bool IncludeInterface=false);
	std::wstring ChainTypeStr(bool IncludeInterface=false);
	std::wstring ChainMTtr(bool IncludeInterface=false);
	std::wstring GetEnumString(CLRDATA_ADDRESS& Address); 
	~EEClass()
	{
		type = NULL;
	}
	static void GetArraySizeByMT(CLRDATA_ADDRESS MethodTable, CLRDATA_ADDRESS *BaseSize, CLRDATA_ADDRESS *ComponentSize);
	static std::wstring GetNameForMT(CLRDATA_ADDRESS MethodTable);

	std::vector<FieldStore> Fields();
	std::vector<FieldStore> FieldsByName(std::wstring Name);

	std::vector<FieldStore> FieldsByName(std::string Name);

	std::vector<FieldStore> FieldsByType(std::wstring TypeName);

	std::vector<FieldStore> FieldsByType(std::string TypeName);

	void ClearSelectThreads(); // to expedite wfrom
	std::vector<FieldStore> PointerFields();
	bool IsValidClass() { return isValidClass; }
	mdTypeDef Token() { return (mdTypeDef)eeClassData.token; }
	DWORD InstanceFields() { return eeClassData.instanceFieldsCount;  }
	//const std::string& PrintType() { EnsureMetadata(); return CCLRHelper::PrettyStringType(&modData, Token()); };
	DWORD StaticFields() { return eeClassData.staticFieldsCount; }
	DWORD Count() { return StaticFields() + InstanceFields(); };
};

std::string TypeString(CorElementType CorType);
bool IsMinidump();

bool IsiDNA();

//#ifndef _WIN64
//bool IsValidMemory(CLRDATA_ADDRESS Address, MEMORY_BASIC_INFORMATION32& MemInfo, INT64 Size = 0);
//#else
bool IsValidMemory(CLRDATA_ADDRESS Address, MEMORY_BASIC_INFORMATION64& MemInfo, INT64 Size = 0);
//#endif
bool IsValidMemory(CLRDATA_ADDRESS Address, INT64 Size = 0);


//GetUserString
class ObjDetail
{
private:
	bool isValid;
	wstring typeName;
	CLRDATA_ADDRESS addr;
	bool isValueType;
	MD_TypeData obj;
	CComPtr<IMDType> type;
public:
	EEClass classObj;

			
	const MD_TypeData TypeData() { return obj; }

	const ObjDetail operator[](UINT64 i)
	{
		if(IsArray() && i < obj.rank)
		{
			if(isValueType)
				return ObjDetail(obj.arrayStart + i* obj.elementSize, MethodTable());

			ExtRemoteData item(obj.arrayStart + i* obj.elementSize, obj.elementSize);
			CLRDATA_ADDRESS itemAddr = item.GetPtr();
			return ObjDetail(itemAddr);
		}
		return ObjDetail(); // return an invalid object
	}
	std::vector<FieldStore> GetFieldsByName(std::string Name)
	{
		std::vector<FieldStore> temp;

		for(int i=0; i<classObj.Fields().size();i++)
		{
				if(g_ExtInstancePtr->MatchPattern(CW2A(classObj.Fields()[i].FieldName.c_str()), Name.c_str(), false))
					temp.push_back(classObj.Fields()[i]);
		}

		return temp;
	}
	
	std::vector<FieldStore> GetFieldsByName(std::vector<std::string> Names)
	{
		std::vector<FieldStore> temp;

		//for(int i=0; i<Names.size()-1;i++)
		for(int i=0; i<Names.size();i++)
		{
			std::vector<FieldStore> fs = GetFieldsByName(Names[i]);
			if(fs.begin() != fs.end())
			{
				temp.insert(temp.end(), fs.begin(), fs.end());
			} else
			{
				g_ExtInstancePtr->Err("Field %s is invalid\n", Names[i]);
			}
		}

		return temp;
		//return classObj.Fields();
	}

	// accept composite fields like field1.sub1.subsib2
	static std::wstring GetFieldValueByName(std::string FieldName, ObjDetail& Obj, MD_FieldData& FieldDesc );

	mdToken Token() { return obj.token; }
	bool IsValid() { return isValid; }
	bool IsUnknown() { return (typeName.compare(L"UNKNOWN") != 0); }
	bool IsFree() { return obj.IsFree != 0; };
	bool IsArray() { return obj.IsArray != 0; };
	bool IsString() 
	{ 
		if(!obj.isString)
		{
			return false;
		}
		// Now let's check if it is a bad string
		// it may be a string, but chances are that it is not
		if(obj.size > 1 * 1024 * 1024)
		{
			return false;
		}
		return true;
	};
	bool IsObject() { return !obj.isValueType; };

	bool InnerIsClass() { return (obj.arrayCorType == ELEMENT_TYPE_CLASS); };
	bool InnerIsValueType() { return (obj.arrayCorType == ELEMENT_TYPE_VALUETYPE); };
	bool InnerIsGeneric() { return (obj.arrayCorType == ELEMENT_TYPE_GENERICINST); };
	CLRDATA_ADDRESS InnerMT() { return obj.arrayElementMT; }
	bool IsValueType() { return isValueType; }
	DWORD InnerComponentSize() { return obj.elementSize; };
	DWORD BaseSize() { return static_cast<DWORD>(obj.BaseSize); }
	CorElementType InnerComponentType() { return (CorElementType)obj.arrayCorType; };
	bool IsRuntime() { return obj.isRuntimeType == 1; }
	bool IsDebugModule() { return obj.debuggingFlag >= 4; }

	CLRDATA_ADDRESS ParentMT() {
		return obj.parentMT;
	}
	ObjDetail() { isValid = false; }
	ObjDetail(CLRDATA_ADDRESS Address)
	{
		Request(Address);
	}

	ObjDetail(CLRDATA_ADDRESS Address, CLRDATA_ADDRESS MT)
	{
		Request(Address, MT);
	}

	bool Request(CLRDATA_ADDRESS Address)
	{
		type = NULL;

		isValueType = false;
		addr = Address;
		isValid = false;
		if(!Address) return false; 

		if(!IsValidMemory(Address, 24))
			return false;
		isValid = pHeap->GetObjectType(addr, &type) == S_OK;

		if(!isValid) return false;
		
		ZeroMemory(&obj, sizeof(obj));
		type->GetHeader(Address, &obj);

		isValid = IsValidMemory(Address, obj.size);

		
		classObj.Request(obj.MethodTable);
		return isValid;
	}

	bool Request(CLRDATA_ADDRESS Address, CLRDATA_ADDRESS MT)
	{
		ZeroMemory(&obj, sizeof(MD_TypeData));	
		type = NULL;

		isValid = false;
		if(MT==0) Request(Address);
		isValueType = true;
		addr = Address;

		if(!Address) return false; 


		
		isValid = pRuntime->GetTypeByMT(MT, &type) == S_OK;

		if(!isValid) return false;
		obj.MethodTable = MT;
		isValid = type->GetHeader(Address, &obj) == S_OK;

		isValid = IsValidMemory(Address, obj.size);

		classObj.Request(obj.MethodTable);
		return isValid;
	}

	std::wstring GetRuntimeTypeName()
	{
		if(!obj.isRuntimeType)
		{
			return L"#NOTRUNTIMETYPE#";
		}

		CComBSTR rtName;
		type->GetRuntimeName(addr, &rtName);
		if(rtName == NULL)
		{
			return L"#INVALIDRUNTIMENAME#";

		}
		wstring str = ToWString(rtName);
		return str;
	}

	std::wstring TypeName()
	{
		if(typeName.size() > 0)
			return typeName;
		if(type)
		{
			CComBSTR str;
			type->GetName(&str);
			typeName = str;
			return typeName;
		}
		typeName = L"UNKNOWN";
		return typeName;
	}

	static CLRDATA_ADDRESS GetPTR(CLRDATA_ADDRESS Address)
	{
		if(Address == 0)
			return 0;
		try
		{
			ExtRemoteData ptr(Address, sizeof(void*));

			return (CLRDATA_ADDRESS)ptr.GetPtr();
		} catch(...)
		{
			return NULL;
		}
	}

	//
	// Deprecated
	//
	std::wstring FullString()
	{
		if(!IsValid() || !IsString())
		{
			return L"<<INVALID STRING OBJECT>>";
		}
		if(obj.size == 0)
		{
			return L"(null)";
		}
		CComBSTR str;
		if(type->GetString(addr, &str) == S_OK)
		{
			if(str == NULL)
				return L"(null)";
			std::wstring temp = ToWString(str);
			return temp;
		}
		return L"<<INVALIDSTRING>>";
	}

	static std::wstring FullString(CLRDATA_ADDRESS Address)
	{
		if(Address == NULL)
			return L"(null)";
		ObjDetail obj(Address);
		return obj.FullString();
	}

	const std::wstring String()
	{
		if(!IsString() || !IsValid())
		{
			return L"<<INVALID STRING OBJECT>>";

		}
		CComBSTR str;
		if(type->GetString(Address(), &str) != S_OK)
		{
			return L"<<INVALID STRING OBJECT>>";
		}
		std::wstring tempStr = ToWString(str);
		return tempStr;
	}

	static const std::wstring String(CLRDATA_ADDRESS Address)
	{
		if(!Address)
			return L"(null)";
		ObjDetail str(Address);
		if(!str.IsString() || !str.IsValid())
		{
			return L"<<INVALID STRING OBJECT>>";
			return NameBuffer;
		}
		return str.String();
		
		
	}

	static CLRDATA_ADDRESS GetFieldAddress(CLRDATA_ADDRESS Addr, MD_FieldData Field, bool isValueType = false, ObjDetail* Obj = NULL)
	{
		CorElementType CorType = (CorElementType)Field.corElementType;
		CLRDATA_ADDRESS offset = Addr+Field.offset+
			(isValueType ? 0 : sizeof(VOID*));  // there is no method table in a value type
		if(!Field.isStatic)
			return offset;
		
		ObjDetail obj;
		if(!Obj)
		{
			if(isValueType)
				obj.Request(Addr, Field.MethodTable);
			else
				obj.Request(Addr);
			if(!obj.IsValid())
				return NULL;
			Obj = &obj;
		}
		CLRDATA_ADDRESS fieldAddr = NULL;
		
		if(Obj->type->GetRawFieldAddress(Addr, isValueType, Field.generalIndex, &fieldAddr) == S_OK)
		{
			// This operation below in unecessary
			//if(fieldAddr && !Field.isValueType) return ObjDetail::GetPTR(fieldAddr);
			return fieldAddr;
		}
		else
			return NULL;
		
	}

	static std::wstring ValueString(MD_FieldData Field, CLRDATA_ADDRESS Addr, bool isValueType = false,
		CLRDATA_ADDRESS Offset = NULL)
	{
		CLRDATA_ADDRESS offset = Offset;
		CorElementType CorType = (CorElementType)Field.corElementType;
		
		if(!offset) offset = GetFieldAddress(Addr, Field, isValueType);
		//ObjDetail obj;

		//if(Field.isValueType)
		//	obj.Request(offset, Field.methodTable);
		//else
		//	obj.Request(offset);
		//
		//
		//
		//if(obj.IsString())
		//	CorType = ELEMENT_TYPE_STRING; // Force string to print

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
						return NameBuffer;
					}
					swprintf(NameBuffer, MAX_MTNAME, L"%p", ptr.GetPtr());
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
					return NameBuffer;
					}
					break;
				case ELEMENT_TYPE_CHAR          :
					{
					ExtRemoteData ptr(offset, sizeof(WCHAR));
					WCHAR ch;
					ptr.ReadBuffer(&ch,sizeof(WCHAR));
					swprintf(NameBuffer, MAX_MTNAME, L"%C", ch);
					return NameBuffer;
					}
					break;
				case ELEMENT_TYPE_I1            :
					//return "int8";
					{
					ExtRemoteData ptr(offset, sizeof(BYTE));
					INT u=(INT)(BYTE)ptr.GetBoolean();

					swprintf(NameBuffer, MAX_MTNAME, L"0x%x (0n%i)", u, u);
					return NameBuffer;
					}
					break;
				case ELEMENT_TYPE_U1            :
					{
					ExtRemoteData ptr(offset, sizeof(BYTE));
					UINT u=(UINT)ptr.GetBoolean();
					swprintf(NameBuffer, MAX_MTNAME, L"0x%x (0n%u)", u, u);
					return NameBuffer;
					}
					break;
					//return "uint8";
				case ELEMENT_TYPE_I2            :
					{
					ExtRemoteData ptr(offset, sizeof(SHORT));
					INT u=(INT)ptr.GetShort();
					swprintf(NameBuffer, MAX_MTNAME, L"0x%x (0n%i)", u, u);
					return NameBuffer;
					}
					break;
					//return "int16";
				case ELEMENT_TYPE_U2            :
					{
					ExtRemoteData ptr(offset, sizeof(SHORT));
					UINT u=(UINT)ptr.GetShort();
					swprintf(NameBuffer, MAX_MTNAME, L"0x%x (0n%u)", u, u);
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
					return NameBuffer;
					}
					break;
					//return "uint32";
				case ELEMENT_TYPE_I8            :
#ifndef _X86_
				case ELEMENT_TYPE_I             :
#endif

					{
					ExtRemoteData ptr(offset, sizeof(INT64));
					INT64 u=ptr.GetLong64();
					swprintf(NameBuffer, MAX_MTNAME, L"%I64x (0n%I64i)", u, u);
					return NameBuffer;
					}
					break;
					//return "int64";
				case ELEMENT_TYPE_U8            :
#ifndef _X86_
				case ELEMENT_TYPE_U     :
#endif

					{
					ExtRemoteData ptr(offset, sizeof(INT64));
					UINT64 u=ptr.GetUlong64();
					swprintf(NameBuffer, MAX_MTNAME, L"%I64x (0n%I64u)", u, u);
					return NameBuffer;
					}
					break;
				case ELEMENT_TYPE_STRING        :
					{
					ExtRemoteData ptr(offset, sizeof(void*));
					CLRDATA_ADDRESS p = ptr.GetPtr();
					if(!p) return L"NULL";
					return String(p);
					}
					break;
				case ELEMENT_TYPE_R4            :
					{
					ExtRemoteData ptr(offset, sizeof(float));
					float u=ptr.GetFloat();
					swprintf(NameBuffer, MAX_MTNAME, L"%f", u);
					return NameBuffer;
					}
					break;
				case ELEMENT_TYPE_R8            :
					{
						ExtRemoteData ptr(offset, sizeof(double));
						double u=ptr.GetDouble();
						swprintf(NameBuffer, MAX_MTNAME, L"%f", u);
						return NameBuffer;
					}
					break;
				case ELEMENT_TYPE_VALUETYPE    :
					{
						ExtRemoteData ptr(offset, sizeof(double));
						double u=ptr.GetDouble();
						swprintf(NameBuffer, MAX_MTNAME, L"-mt %p %p", Field.MethodTable, offset); // return pointer to value type and the method table
						return NameBuffer;
					}
					break;

				default:
				case ELEMENT_TYPE_SENTINEL      :
				case ELEMENT_TYPE_END           :
					return L"Unknown";
			} // end switch
		} catch(...)
		{
			if(!Field.isStatic)
				swprintf(NameBuffer, MAX_MTNAME, L"<Unable to read at %p>", offset);
			else
				swprintf(NameBuffer, MAX_MTNAME, L"%p:NoInit", offset);
		}
		return NameBuffer;
	}

	std::wstring AssemblyName()
	{
		CComBSTR tmp;
		if(type->GetFilename(&tmp) == S_OK)
		{
			wstring fname = ToWString(tmp);
			return fname;
		}
		else
			return L"<<INVALID>>";

	}
	CLRDATA_ADDRESS EEClassAddr() { return obj.EEClass; }
	CLRDATA_ADDRESS MethodTable() { return obj.MethodTable; };
	DWORD Size() { return static_cast<DWORD>(obj.size); };
	DWORD Rank() { return obj.rank; };
	DWORD NumComponents() { return obj.arraySize; }

	CLRDATA_ADDRESS DataPtr() { return obj.arrayStart; }
	CLRDATA_ADDRESS Address() {return addr; }
	int InstanceFieldsCount() { return obj.instanceFieldsCount; }
	int StaticFieldsCount() { return obj.staticFieldsCount; }
	int Heap() { return obj.heap; }
	int Gen() { return obj.generation; }
	CLRDATA_ADDRESS Module() { return obj.Module; }
	CLRDATA_ADDRESS Assembly() { return obj.assembly; }
	CLRDATA_ADDRESS Domain() { return obj.appDomainAddr; }
};


class StackObj
{
private:

	ULONG threadId;
	bool isManaged;
	ULONG numFrames;
	ULONG currFrame;
	std::map<CLRDATA_ADDRESS, std::vector<ULONG>> StackCache;
public:
	CLRDATA_ADDRESS stackStart;
	CLRDATA_ADDRESS stackEnd;

	StackObj()
	{
		currFrame = 0;
		isManaged = false;
		stackStart = 0;
		stackEnd = 0;
		threadId = 0;
		numFrames = 0;
	}

	void Clear()
	{
		StackCache.clear();
	}

	void DumpStackObject(bool CacheOnly=false);

	vector<ULONG> IsInStack(CLRDATA_ADDRESS addr);

	void Initialize()
	{
		NT_TIB teb;
		ULONG64 tebAddr=0;

		HRESULT status = g_ExtInstancePtr->m_System->GetCurrentThreadTeb(&tebAddr);

		if(SUCCEEDED(status))
		{
			ExtRemoteData rteb(tebAddr, sizeof(teb));
			rteb.ReadBuffer(&teb, sizeof(teb));
			stackStart = (CLRDATA_ADDRESS)teb.StackBase;
			stackEnd = (CLRDATA_ADDRESS)teb.StackLimit;
		}
		if(SUCCEEDED(g_ExtInstancePtr->m_System->GetCurrentThreadSystemId(&threadId))
			)
		{
			isManaged = true;
		}
	}

	~StackObj()
	{
	}
};

BOOL TraverseHandle(CLRDATA_ADDRESS HandleAddr,CLRDATA_ADDRESS HandleValue,int HandleType, CLRDATA_ADDRESS appDomainPtr,LPVOID token);
BOOL TraverseHandle4(CLRDATA_ADDRESS HandleAddr, CLRDATA_ADDRESS HandleValue, int HandleType,
    ULONG ulRefCount, CLRDATA_ADDRESS appDomainPtr, CLRDATA_ADDRESS Target);
extern UINT64 countH;

struct GCEnum
{
	CLRDATA_ADDRESS start;
	CLRDATA_ADDRESS end;
	CLRDATA_ADDRESS curr;
	CLRDATA_ADDRESS currRange;
	CLRDATA_ADDRESS rangeStart;
	CLRDATA_ADDRESS rangeEnd;
	int heap;
	int gen;
	bool large;
};

// start, end, heap, generation, large objects
typedef tuple<CLRDATA_ADDRESS,CLRDATA_ADDRESS,int,int,bool> HeapRange;

class Heap
{
public:
	map<CLRDATA_ADDRESS, HeapRange> ranges;
	CLRDATA_ADDRESS *heapPtrs;
	UINT currentHeap;
	bool valid;
	HRESULT lastHR;
	int throttle;

	bool wasInterrupted;

	bool IsInHeap(CLRDATA_ADDRESS Address, int* iHeap = NULL, int* iGen = NULL);

	void AddRanges(Thread &thread, CLRDATA_ADDRESS Start, CLRDATA_ADDRESS End,
					 long heap, int gen, bool isLarge);

	static void AddMT(std::string MT)
	{
		std::vector<std::string> tempMT;
		VectorSplit(MT, tempMT);
		std::vector<std::string>::const_iterator it = tempMT.begin();
		while(it!=tempMT.end())
		{
			CLRDATA_ADDRESS addr;
			addr = g_ExtInstancePtr->EvalExprU64(it->c_str());
			mtList.push_back(addr);
			it++;
		}
	}

	static void AddTypes(std::string Types)
	{
		VectorSplit(Types, typeList);
	}

	static bool IncludeType(CLRDATA_ADDRESS MT)
	{
		if(mtList.size() == 0 && typeList.size() == 0)
		{
			return true;
		}
		for(int i=0;i<mtList.size(); i++)
		{
			if(mtList[i] == MT) return true;
		}
		return false;
	}

	static bool IncludeType(CLRDATA_ADDRESS MT, std::wstring TypeName)
	{
		if(mtList.size() == 0 && typeList.size() == 0)
		{
			return true;
		}

		for(int i=0;i<typeList.size(); i++)
		{
			if(g_ExtInstancePtr->MatchPattern(CW2A(TypeName.c_str()),typeList[i].c_str()))
			{
				mtList.push_back(MT); // faster to test the MT than wildcards
				typesMap[MT]=TypeName; // To create a list of objects
				return true;
			}
		}
		return false;
	}

	CLRDATA_ADDRESS NearObject(GCEnum& Enum)
	{
		map<CLRDATA_ADDRESS, HeapRange>::const_iterator it = ranges.begin();
		while(it != ranges.end())
		{
			HeapRange range = it->second;
			if(Enum.curr <= get<0>(range))
			{
				Enum.curr = get<0>(range);
			}
			if(Enum.curr >= get<0>(range) && Enum.curr <= get<1>(range))
			{
				Enum.rangeStart = Enum.curr;
				Enum.rangeEnd = get<1>(range);
				Enum.heap = get<2>(range);
				Enum.gen = get<3>(range);
				Enum.large = get<4>(range);
				return Enum.curr;
			}
			it++;
		}
		return NULL;
	}

	CLRDATA_ADDRESS GetObj(GCEnum& Enum, ObjDetail &Obj)
	{
		Obj.Request(Enum.curr);
		size_t size;
		CLRDATA_ADDRESS mt = ObjDetail::GetPTR(Enum.curr);
		if(!Obj.IsValid())
		{
			size = sizeof(VOID*);
		} else
		{
			size = Obj.Size();
		}
		Enum.curr += Enum.large ? AlignLarge(size) : Align(size);
		if(Enum.curr >= Enum.end)
		{
			return NearObject(Enum);
		}
		return Enum.curr;
	}

	GCEnum StartEnum(CLRDATA_ADDRESS Start, CLRDATA_ADDRESS End)
	{
		GCEnum temp = {Start, End, Start, NULL, NULL, 0, 0};
		NearObject(temp);
		return temp;
	}

	Heap()
	{
		heapPtrs = NULL;
		currentHeap = 0;
		valid = AreStructuresValid();
		
		
		throttle = 500;
		typesMap.clear();
		mtList.clear();
		typeList.clear();
		wasInterrupted = false;
	}

	~Heap()
	{
		if(heapPtrs)
			delete[] heapPtrs;
	}
	mdToken token;
	BOOL DoTransverse()
	{
		//ISOSHandleEnum *hEnum;
		//const int size = 4;
		//UINT count = size; // Just to assure loop
		//SOSHandleData handles[size];
		//if(sosData->GetHandleEnum(&hEnum) == S_OK)
		//{
		//	while(count == size)
		//	{
		//		if(IsInterrupted()) break;
		//		if(FAILED(hEnum->Next(size, handles, &count)))
		//		{
		//			g_ExtInstancePtr->Out("Unable to walk root handles\n");
		//			break;
		//		}
		//		for(int i=0; i<count; i++)
		//		{
		//			//g_ExtInstancePtr->Out("%p %p %i %i\n", handles[i].Handle, handles[i].Secondary, handles[i].Type, handles[i].RefCount);
		//			if(!TraverseHandle4(handles[i].Handle, ObjDetail::GetPTR(handles[i].Handle),
		//				handles[i].Type, handles[i].RefCount, handles[i].AppDomain,
		//				handles[i].Secondary))
		//			{ 
		//				count = 0;
		//				break;
		//			};

		//		}
		//	}

		//}
		//hEnum->Release();
		return true;
	}

	void EnumRanges();

	CLRDATA_ADDRESS DisplayHeapObjects(CLRDATA_ADDRESS Start, CLRDATA_ADDRESS End, int HeapNumber, int Gen, bool IsLarge = false, UINT Limit = 500, bool IsLean = false);

	void TraverGC()
	{
		// *val;
		//val->QueryInterface(IID_ICLRMetadataLocator
	}

	void DoTraverse2()
	{
		/*
		DacpAppDomainStoreData domainStore;
		if(FAILED(domainStore.Request(sosData)))
		{
			g_ExtInstancePtr->Out("Unable to get domains info\n");
			return;
		}
		CLRDATA_ADDRESS *domain = new CLRDATA_ADDRESS[domainStore.DomainCount];
		for(int i=0;i<domainStore.DomainCount;i++)
		{
			DacpAppDomainData domainData;
			domainData.Request(sosData, domain[i]);

			VISITHEAP a;

			//DacpLoaderHeapTraverse::DoTraverse(clrData, domainData.pHighFrequencyHeap,
		}
		//DacpLoaderHeapTraverse::DoTraverse(
		*/
	}

	//DacpGcHeap GetHeapDetails(UINT Index)
	//{
	//	UINT i=Index;
	//	
	//	DacpGcHeapDetails detail;
	//	if(Index >= Count())
	//	{
	//		i=0;
	//		//DacpEHInfoTraverse trav;
	//	}
	//	if(IsServerMode())
	//	{
	//		lastHR = detail.Request(sosData, heapPtrs[i]);
	//	} else
	//	{
	//		lastHR = detail.Request(sosData);
	//	}
	//	return detail;
	//}

	bool IsValid()
	{
		return valid;
	}

	bool AreStructuresValid()
	{
		return Count() > 0;
	}

	bool IsServerMode()
	{
		long GCMode;
		pRuntime->IsServerGC(&GCMode);

		return GCMode != 0;
	}

	UINT MaxGeneration()
	{
		return 3;
	}

	UINT Count()
	{
		long count = 0;
		pRuntime->GetHeapCount(&count);

		return count; // return heapData.HeapCount;
	}
};
class KnownTypesMT
{
private:
	MD_CommonMT commonMTs;
public:
	CLRDATA_ADDRESS FreeMethodTable() { return commonMTs.FreeMethodTable; }
	CLRDATA_ADDRESS StringMethodTable() { return commonMTs.StringMethodTable; }
	CLRDATA_ADDRESS ArrayMethodTable() { return commonMTs.ArrayMethodTable; }

	KnownTypesMT()
	{
		ZeroMemory(&commonMTs,sizeof(MD_CommonMT));
	}

	void Request()
	{
		pRuntime->GetCommonMethodTable(&commonMTs);
	}
};

class DumpHeapCache
{
private:
	UINT64 address;
	BYTE* memBuffer;
	UINT32 size;
	UINT32 maxSize;
	KnownTypesMT commonMTs;
public:
	DumpHeapCache(UINT32 CacheSize=64*1024)
	{
		address = 0;
		memBuffer = new BYTE[CacheSize];
		ZeroMemory(memBuffer, CacheSize);
		size = 0;
		maxSize=CacheSize;
		commonMTs.Request();

	}

	~DumpHeapCache()
	{
		delete[] memBuffer;
	}

	MD_TypeData GetObj(CLRDATA_ADDRESS Address);
	void* Read(CLRDATA_ADDRESS Address, UINT32 Size);
	DWORD_PTR GetPtr(CLRDATA_ADDRESS Address);
};


#endif // !_CLR_HELPER_