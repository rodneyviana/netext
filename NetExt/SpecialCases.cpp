/*=========================================================================================================
Copyright (c) 2013-2015 Rodney Viana
http://netext.codeplex.com/

Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "SpecialCases.h"
#include "CLRHelper.h"

//
// Avoid hassless of including winsock2.h and having all redefined
//






#ifndef _WINSOCK2API_
typedef USHORT ADDRESS_FAMILY;

#define WINSOCK_API_LINKAGE DECLSPEC_IMPORT

#define WSAAPI                  FAR PASCAL

#define WSAPROTOCOL_LEN  255

#define MAX_PROTOCOL_CHAIN 7

#define BASE_PROTOCOL      1
#define LAYERED_PROTOCOL   0

#if !defined(_WINSOCK_DEPRECATED_BY)
#if ((defined(_WINSOCK_DEPRECATED_NO_WARNINGS) || defined(BUILD_WINDOWS)) && !defined(_WINSOCK_DEPRECATE_WARNINGS)) || defined(MIDL_PASS)
#define _WINSOCK_DEPRECATED_BY(replacement)
#else
#define _WINSOCK_DEPRECATED_BY(replacement) __declspec(deprecated("Use " ## replacement ## " instead or define _WINSOCK_DEPRECATED_NO_WARNINGS to disable deprecated API warnings"))
#endif
#endif

typedef struct _WSAPROTOCOLCHAIN {
	int ChainLen;                                 /* the length of the chain,     */
	/* length = 0 means layered protocol, */
	/* length = 1 means base protocol, */
	/* length > 1 means protocol chain */
	DWORD ChainEntries[MAX_PROTOCOL_CHAIN];       /* a list of dwCatalogEntryIds */
} WSAPROTOCOLCHAIN, FAR * LPWSAPROTOCOLCHAIN;

typedef struct _WSAPROTOCOL_INFOW {
	DWORD dwServiceFlags1;
	DWORD dwServiceFlags2;
	DWORD dwServiceFlags3;
	DWORD dwServiceFlags4;
	DWORD dwProviderFlags;
	GUID ProviderId;
	DWORD dwCatalogEntryId;
	WSAPROTOCOLCHAIN ProtocolChain;
	int iVersion;
	int iAddressFamily;
	int iMaxSockAddr;
	int iMinSockAddr;
	int iSocketType;
	int iProtocol;
	int iProtocolMaxOffset;
	int iNetworkByteOrder;
	int iSecurityScheme;
	DWORD dwMessageSize;
	DWORD dwProviderReserved;
	WCHAR  szProtocol[WSAPROTOCOL_LEN+1];
} WSAPROTOCOL_INFOW, FAR * LPWSAPROTOCOL_INFOW;

typedef struct _WINSOCK_DEPRECATED_BY("WSAPROTOCOL_INFOW") _WSAPROTOCOL_INFOA {
	DWORD dwServiceFlags1;
	DWORD dwServiceFlags2;
	DWORD dwServiceFlags3;
	DWORD dwServiceFlags4;
	DWORD dwProviderFlags;
	GUID ProviderId;
	DWORD dwCatalogEntryId;
	WSAPROTOCOLCHAIN ProtocolChain;
	int iVersion;
	int iAddressFamily;
	int iMaxSockAddr;
	int iMinSockAddr;
	int iSocketType;
	int iProtocol;
	int iProtocolMaxOffset;
	int iNetworkByteOrder;
	int iSecurityScheme;
	DWORD dwMessageSize;
	DWORD dwProviderReserved;
	CHAR   szProtocol[WSAPROTOCOL_LEN+1];
} WSAPROTOCOL_INFOA, FAR * LPWSAPROTOCOL_INFOA;



#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP)
_WINSOCK_DEPRECATED_BY("WSAAddressToStringW()")
	WINSOCK_API_LINKAGE
	INT
	WSAAPI
	WSAAddressToStringA(
	_In_reads_bytes_(dwAddressLength) LPSOCKADDR lpsaAddress,
	_In_     DWORD               dwAddressLength,
	_In_opt_ LPWSAPROTOCOL_INFOA lpProtocolInfo,
	_Out_writes_to_(*lpdwAddressStringLength,*lpdwAddressStringLength) LPSTR lpszAddressString,
	_Inout_  LPDWORD             lpdwAddressStringLength
	);
#endif /* WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP) */
#pragma endregion

WINSOCK_API_LINKAGE
	INT
	WSAAPI
	WSAAddressToStringW(
	_In_reads_bytes_(dwAddressLength) LPSOCKADDR lpsaAddress,
	_In_     DWORD               dwAddressLength,
	_In_opt_ LPWSAPROTOCOL_INFOW lpProtocolInfo,
	_Out_writes_to_(*lpdwAddressStringLength,*lpdwAddressStringLength) LPWSTR lpszAddressString,
	_Inout_  LPDWORD             lpdwAddressStringLength
	);
#ifdef UNICODE
#define WSAAddressToString  WSAAddressToStringW
#else
#define WSAAddressToString  WSAAddressToStringA
#endif /* !UNICODE */


#define AF_INET6        23              // Internetwork Version 6

typedef struct in6_addr {
	union {
		UCHAR       Byte[16];
		USHORT      Word[8];
	} u;
} IN6_ADDR, *PIN6_ADDR, FAR *LPIN6_ADDR;

typedef struct {
	union {
		struct {
			ULONG Zone : 28;
			ULONG Level : 4;
		};
		ULONG Value;
	};
} SCOPE_ID, *PSCOPE_ID;

typedef struct sockaddr_in6 {
	ADDRESS_FAMILY sin6_family; // AF_INET6.
	USHORT sin6_port;           // Transport level port number.
	ULONG  sin6_flowinfo;       // IPv6 flow information.
	IN6_ADDR sin6_addr;         // IPv6 address.
	union {
		ULONG sin6_scope_id;     // Set of interfaces for a scope.
		SCOPE_ID sin6_scope_struct; 
	};
} SOCKADDR_IN6_LH, *PSOCKADDR_IN6_LH, FAR *LPSOCKADDR_IN6_LH;

#endif /* Not Defined _WINSOCK2API_ */

int DaysToMonth365[13] = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};
int DaysToMonth366[13] = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366};

inline bool IsLeapYear(int year)
{
	return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
}

SpecialCases::SpecialCases(void)
{
}

SpecialCases::~SpecialCases(void)
{
}

void SpecialCases::DumpHash(CLRDATA_ADDRESS Address, std::string NameMatch, std::string ValueMatch, std::vector<varMap>* KeyPair)
{
	if(Address == NULL)
	{
		return;
	}
	ObjDetail obj(Address);
	if(!obj.IsValid() || !obj.classObj.Implement(L"System.Collections.Hashtable"))
	{
		return;
	}
	std::vector<std::string> fields;
	fields.push_back("buckets");
	fields.push_back("count");
	varMap fieldV;
	DumpFields(Address,fields,0,&fieldV);
	int n=fieldV["count"].Value.i32;
	CLRDATA_ADDRESS bucket = fieldV["buckets"].Value.ptr;
	CLRDATA_ADDRESS mt = fieldV["buckets"].MT;

	if(n<=0 || bucket == NULL)
	{

		return;
	}
	std::vector<CLRDATA_ADDRESS> addresses;
	obj.Request(bucket);
	mt=obj.InnerMT();
	SpecialCases::EnumArray(bucket,0,NULL,&addresses);
	std::vector<CLRDATA_ADDRESS>::const_iterator it=addresses.begin();
	int i=0;
	while(it!=addresses.end())
	{
		if(*it != NULL)
		{
			fieldV.clear();
			fields.clear();
			fields.push_back("key");
			fields.push_back("val");
			fields.push_back("hash_coll");
			if(KeyPair)
				DumpFields(*it,fields,mt,&fieldV);
			else
				DumpFields(*it,fields,mt);
			if(fieldV["hash_coll"].Value.i32 != 0)
			{

				fields.pop_back();
				fieldV.clear();
				DumpFields(*it,fields,mt,&fieldV);
				if(
					(NameMatch.size() == 0 || g_ExtInstancePtr->MatchPattern(CW2A(fieldV["key"].strValue.c_str()), NameMatch.c_str())) &&
					(ValueMatch.size() == 0 || g_ExtInstancePtr->MatchPattern(CW2A(fieldV["val"].strValue.c_str()), ValueMatch.c_str())) &&
					KeyPair
					)
				{
					KeyPair->push_back(fieldV);
				}
			}
		}
		it++;
	}
}

std::wstring SpecialCases::IPV4Address(INT64 Address, int Port)
{
	std::wstring ipstring;
	INT64 addr = Address;
	WCHAR buffer[16] = { 0 };
	for (int i = 0; i < 4; i++)
	{
		wsprintf(buffer, L"%i", (int)(addr & 0xff));
		addr = addr >> 8;
		ipstring.append(buffer);
		if (i != 3)
			ipstring.append(L".");
	}
	if (Port != 0)
	{
		wsprintf(buffer, L":%i", Port);

		ipstring.append(buffer);
	}
	return ipstring;
}
std::wstring SpecialCases::IPV6Address(WORD* SocketAddress, int Port, int ScopeId)
{
	std::wstring ipstring;
	try
	{

		WORD wVersionRequested;
		WSADATA wsaData;
		int err = 0;
		wVersionRequested = MAKEWORD(2, 2);

		err = ::WSAStartup(wVersionRequested, &wsaData);
		if (err != 0) {
			return ipstring;
		}

		WCHAR addressString[256] = { 0 };
		DWORD size = 256;
		ULONG port = Port;
		SOCKADDR_IN6 addr;
		ZeroMemory(&addr, sizeof(addr));

		ULONG scope_id = ScopeId;



		addr.sin6_family = AF_INET6;
		addr.sin6_port = (BYTE)(port)* 0x100 + (BYTE)(port >> 8);
		addr.sin6_scope_id = scope_id;
		addr.sin6_flowinfo = 0;
		BYTE a[4] = { 0 };

		a[0] = (BYTE)scope_id;
		a[1] = (BYTE)(scope_id >> 8);
		a[2] = (BYTE)(scope_id >> 16);
		a[3] = (BYTE)(scope_id >> 24);
		memcpy(&addr.sin6_scope_struct, a, 4);


		int j = 0;
		for (int i = 0; i < 8; i++)
		{
			addr.sin6_addr.u.Byte[j++] = (BYTE)(SocketAddress[i] >> 8);
			addr.sin6_addr.u.Byte[j++] = (BYTE)(SocketAddress[i] & 0xff);
		}

		int result = WSAAddressToString(
			(LPSOCKADDR)&addr,
			sizeof(addr),
			0,
			(LPWSTR)&addressString,
			&size
			);

		if (result == 0)
		{
			ipstring.assign(addressString);
		}



	}
	catch (...)
	{
		ipstring = L"";
	}
	::WSACleanup();
	return ipstring;
}
std::wstring SpecialCases::IPAddress(CLRDATA_ADDRESS IPAddress)
{

	if(IPAddress == NULL)
		return L"";
	ObjDetail obj(IPAddress);
	if(!obj.IsValid() || (obj.TypeName() != L"System.Net.IPEndPoint"
		&& obj.TypeName() != L"System.Net.IPAddress")
		)
		return L"";
	CLRDATA_ADDRESS address = IPAddress;
	int port = 0;

	std::vector<std::string> fields;
	varMap fieldV;

	if(obj.TypeName() == L"System.Net.IPEndPoint")
	{
		fields.push_back("m_Address");
		fields.push_back("m_Port");

		DumpFields(address,fields,0,&fieldV);
		port = fieldV["m_Port"].Value.i32;
		address = fieldV["m_Address"].Value.ptr;
		if(address == NULL)
			return L"";
	}

	fields.clear();
	fields.push_back("m_Family");
	fields.push_back("m_Address");
	fields.push_back("m_ScopeId");
	fields.push_back("m_Numbers");



	fieldV.clear();
	DumpFields(address,fields,0,&fieldV);
	if(fieldV["m_Family"].Value.i32 == 2)
		return SpecialCases::IPV4Address(fieldV["m_Address"].Value.i64,port);

	ObjDetail numbers(fieldV["m_Numbers"].Value.ptr);

	if(!numbers.IsValid() || numbers.NumComponents() != 8)
		return L"";

	WORD ipbytes[8] = {0};
	unsigned long read = 8;
	ReadMemory(numbers.DataPtr(),&ipbytes,read *sizeof(WORD), &read);
	return IPV6Address((WORD*)&ipbytes,port,fieldV["m_ScopeId"].Value.i32);
}

std::wstring SpecialCases::HtmlEncode(std::wstring HtmlString, bool EncodeSpaces)
{
	std::wstring newStr;
	for(int i=0;i<HtmlString.size();i++)
	{
		switch(HtmlString[i])
		{
		case L'<':
			newStr+=L"&lt;";
			break;
		case L'>':
			newStr+=L"&gt;";
			break;
		case L'&':
			newStr+=L"&amp;";
			break;
		case L'"':
			newStr+=L"&quot;";
			break;
		case L'\n':
			newStr+=L"<br />";
			break;
		case L' ':
			if(EncodeSpaces && (i>0 && HtmlString[i-1] == L' '))
			{
				newStr += L"&nbsp;";
			} else
			{
				newStr += L' ';
			}
			break;
		default:
			if(HtmlString[i] > (WCHAR)127 || HtmlString[i] < (WCHAR)32)
			{
				newStr+=L"&#"+boost::lexical_cast<wstring>((UINT16)HtmlString[i])+L";"; 
			} else
				newStr+=HtmlString[i];
			break;
		}
	}
	return newStr;
}

bool SpecialCases::GetSocketData(CLRDATA_ADDRESS Address, SocketData* Socket)
{
	if(Address == NULL)
		return false;
	ObjDetail obj(Address);
	if(!obj.IsValid() || obj.TypeName() != L"System.Net.Sockets.Socket")
		return false;	
	std::vector<std::string> fields;
	varMap fieldV;
	fields.push_back("m_Handle.handle");
	fields.push_back("m_Handle._state");
	fields.push_back("m_Handle._ownsHandle");
	fields.push_back("m_Handle._fullyInitialized");
	fields.push_back("m_Handle.m_Released");
	fields.push_back("m_Handle.m_InnerSocket");
	fields.push_back("m_RightEndPoint");
	fields.push_back("m_RemoteEndPoint");
	fields.push_back("m_IsConnected");
	fields.push_back("m_IsDisconnected");
	fields.push_back("m_CloseTimeout");
	fields.push_back("isListening");
	fields.push_back("m_IntCleanedUp");
	fields.push_back("willBlock");

	DumpFields(Address,fields,0,&fieldV);

	Socket->Handle = (HANDLE)fieldV["m_Handle.handle"].Value.ptr;
	Socket->state = fieldV["m_Handle._state"].Value.i32;
	Socket->ownsHandle = fieldV["m_Handle._ownsHandle"].Value.b;
	Socket->isFullyInitialized = fieldV["m_Handle._fullyInitialized"].Value.b;
	Socket->isReleased = fieldV["m_Handle.m_Released"].Value.b;
	Socket->innerSocket = fieldV["m_Handle.m_InnerSocket"].Value.ptr;
	Socket->rightEndpoint = fieldV["m_RightEndPoint"].Value.ptr;
	Socket->remoteEndpoint = fieldV["m_RemoteEndPoint"].Value.ptr;
	Socket->isConnected = fieldV["m_IsConnected"].Value.b;
	Socket->isDisconnected = fieldV["m_IsDisconnected"].Value.b;
	Socket->closeTimeout = fieldV["m_CloseTimeout"].Value.i32;
	Socket->isListening = fieldV["isListening"].Value.b;
	Socket->cleanedUp = fieldV["m_IntCleanedUp"].Value.i32;
	Socket->willBlock = fieldV["willBlock"].Value.b;

	return true;

}

bool SpecialCases::IsEnumType(CLRDATA_ADDRESS MT)
{
	EEClass objClass;

	objClass.Request(MT);
	if(objClass.IsValidClass())
	{
		return objClass.Implement(L"System.Enum");
	}
	return false;
}

std::wstring SpecialCases::GetEnumString(const MD_FieldData& Field, UINT64 Value)
{
	return GetEnumString(Field.MethodTable, Value, (mdTypeDef)Field.token);
}
std::wstring SpecialCases::GetEnumString(CLRDATA_ADDRESS MethodTable, UINT64 Value, mdTypeDef Token)
{
	EEClass objClass;

	objClass.Request(MethodTable);
	if(objClass.IsValidClass())
	{
		return objClass.GetEnumString(Value);
	}

	/*
	CComPtr<IMetaDataImport> mi = NULL;
	if(!GetModuleInterfaceFromMT(&mi, MethodTable))
	{
	return L"*<INVALID MODULE>*";
	}
	HRESULT hr=S_OK;
	if(!Token)
	{
	EEClass objClass;
	objClass.Request(MethodTable);
	if(objClass.IsValidClass())
	{
	Token = objClass.Token();
	} else
	{
	return L"*<INVALID TOKEN>*";
	}
	}


	std::map<UINT64,std::wstring> enums;
	mdFieldDef tokens[64]={0};
	HCORENUM henum = 0;
	PCCOR_SIGNATURE ppvSigBlob=NULL;
	UVCP_CONSTANT ppValue=NULL;
	ULONG total = 0;

	hr=mi->EnumFields(&henum,Token,tokens,64,&total);
	if(hr == S_OK)
	{
	for(int i=0;i<total;i++)
	{
	mdToken mdTypeDef=0;
	DWORD pdwAttr,pdwCPlusTypeFlag=0;
	ULONG pchField, pcchValue, pcbSigBlob=0;
	mi->GetFieldProps(tokens[i], &mdTypeDef, NameBuffer, MAX_MTNAME, &pchField,
	&pdwAttr, &ppvSigBlob, &pcbSigBlob, &pdwCPlusTypeFlag, &ppValue, &pcchValue);
	if(pdwAttr == 0x8056)
	{
	UINT64 v;
	switch(IntSize((CorElementType)pdwCPlusTypeFlag))
	{
	case 1:
	v = *(const UINT8*)ppValue;
	break;
	case 2:
	v = *(const UINT16*)ppValue;
	break;
	case 4:
	v = *(const UINT32*)ppValue;
	break;
	case 8:
	v = *(const UINT64*)ppValue;
	break;
	}
	enums[v]=NameBuffer;
	}
	}
	bool found = false;

	mi=NULL;
	if(enums.find(Value) != enums.end())
	{
	return enums[Value];
	}
	// it may be a System.EnumFlags
	std::map<UINT64,std::wstring>::const_iterator it=enums.begin();
	std::wstring tempStr = L"";
	//g_ExtInstancePtr->Out("***%I64x***",Value);
	while(it!=enums.end())
	{
	UINT m = Value & it->first;
	if(m!=0 && m == it->first)
	{
	if(tempStr.size()>0) tempStr.append(L"|");
	tempStr.append(it->second);
	}
	it++;
	}
	return tempStr;
	}
	*/
	return L"#invalid#";
}

void SpecialCases::EnumArray(CLRDATA_ADDRESS Address, CLRDATA_ADDRESS MethodTable, ObjDetail *Obj, std::vector<CLRDATA_ADDRESS>* Addresses)
{
	ObjDetail obj;
	ObjDetail *pObj = Obj;
	if(Obj==NULL)
	{
		if(MethodTable == 0)
		{
			obj.Request(Address);
		} else
		{
			obj.Request(Address, MethodTable);
		}
		pObj = &obj;
	}
	int start = 0;
	int end =  pObj->NumComponents(); //End < 0 ? (*pObj).obj.dwNumComponents : min(End, (*pObj).obj.dwNumComponents);
	while(start < end)
	{
		CLRDATA_ADDRESS itemAddr = pObj->DataPtr() + start* pObj->InnerComponentSize();
		CorElementType eType = pObj->InnerComponentType();

		if(IsCorObj(eType) || eType == ELEMENT_TYPE_STRING)
		{
			itemAddr=ObjDetail::GetPTR(itemAddr);
		}
		Addresses->push_back(itemAddr);

		start++;
	}
}

std::string SpecialCases::GetHexArray(CLRDATA_ADDRESS Obj, bool Padded, int Limit)
{
	string result="";

	ObjDetail obj(Obj);
	if(!obj.IsValid())
		return result;
	string bytes;
	std::wstring className = obj.TypeName();
	char buff[6] = {0};
	int limit = (Limit == 0 || static_cast<DWORD>(Limit) > obj.NumComponents()) ? obj.NumComponents() : Limit;  
	if(className == L"System.Byte[]" || className == L"System.SByte[]")
	{
		for(int i=0;i<limit;i++)
		{
			ExtRemoteData ptr(obj.DataPtr()+i*obj.InnerComponentSize(), sizeof(char));
			if(!IsValidMemory(ptr.m_Offset))
			{
				if(Padded)
				{
					result.append("?? ");
				}
				else
				{
					result.append("??");
				}
				if(Limit != 0)
					bytes += '?';
			} else
			{
				byte b=ptr.GetUchar();
				if(Padded)
					sprintf_s(buff, 6, "%02x ", b);
				else
					sprintf_s(buff, 6, "%02x", b);

				if(Limit != 0)
				{
					bytes += b >= 0x20 && b <= 0x7f ? (char)b : '.';
				}
				result.append(buff);
			}
		}
	}
	if(className == L"System.Char[]" || className == L"System.Int16[]" || className == L"System.UInt16[]" )
	{
		for(unsigned int i=0;i<limit;i++)
		{
			ExtRemoteData ptr(obj.DataPtr()+i*obj.InnerComponentSize(), sizeof(unsigned short));
			if(!IsValidMemory(ptr.m_Offset))
			{
				if(Padded)
					result.append("???? ");
				else
					result.append("????");
				if(Limit != 0)
					bytes += '?';
			} else
			{
				unsigned short s=ptr.GetUshort();
				if(Padded)
					sprintf_s(buff, 6, "%04x ", s);
				else
					sprintf_s(buff, 6, "%04x", s);

				if(Limit != 0)
				{
					bytes += s >= 0x20 && s <= 0x7f ? (char)(s & 0xff) : '.';
				}

				result.append(buff);
			}
		}
	}
	if(result.size() != 0 && !Padded)
		result.resize(result.size()-1); // remove extra trailing space
	if(bytes.size() != 0)
		result += " " + bytes;
	if(limit != obj.NumComponents() && result.size() != 0)
	{
		result.append(" (...more...)");
	}

	return result;

}
SVAL SpecialCases::GetBoxedValue(CLRDATA_ADDRESS Address)
{
	SVAL s;
	s.MakeInvalid();
	if(Address == 0) return s;
	ObjDetail obj(Address);
	if(!obj.IsValid() || !obj.classObj.Implement(L"System.ValueType"))
		return s;

	std::vector<std::string> fields;
	fields.push_back("m_value");
	varMap fieldV;
	DumpFields(Address,fields,0,&fieldV);
	if(fieldV["m_value"].IsInt() || fieldV["m_value"].IsReal()
		|| fieldV["m_value"].IsUnsigned() || fieldV["m_value"].corBaseType == ELEMENT_TYPE_BOOLEAN)
		return fieldV["m_value"];
	// all failed, return invalid
	return s;
}

void SpecialCases::PrintableString(std::string& Str, bool AllowNewLine)
{
	std::string tmpString;
	for(int k=0;k<Str.size();k++)
	{
		bool valid = ((Str[k] == 13 || Str[k] == 10 || Str[k] == 9) && AllowNewLine) || (Str[k] > 31);
		if(!valid)
		{
			char buff[8] = {0};
			sprintf_s(buff, 8, "\\x%02x", static_cast<BYTE>(Str[k]));
			tmpString.append(buff);
		} else
		{
			tmpString.append(Str.substr(k, 1));

		}
	}
	Str = tmpString;
}

std::string SpecialCases::PrettyPrint(CLRDATA_ADDRESS Address, CLRDATA_ADDRESS MethodTable)
{
	if(Address == 0) return "";
	ObjDetail obj;
	if(MethodTable)
		obj.Request(Address, MethodTable);
	else
	{
		SVAL v = GetBoxedValue(Address);
		if(v.IsValid && v.strValue.size() != 0)
		{
			string ret = CW2A(v.strValue.c_str());
			return ret;
		}
		obj.Request(Address);
	}
	if(!obj.IsValid())
		return "";
	std::wstring methName = obj.TypeName();

	if(obj.IsRuntime())
	{
		return CW2A(obj.GetRuntimeTypeName().c_str());
	}

	if(obj.IsString())
	{
		string ret = CW2A(obj.String().c_str());
		return ret;
	}

	if(methName == L"System.DateTime")
	{
		ExtRemoteData dt(Address + (MethodTable ? 0 : sizeof(void*)) , sizeof(UINT64));
		UINT64 ticks = dt.GetUlong64();
		string ret = CW2A(tickstodatetime(ticks).c_str());
		return ret;
	}
	if(methName == L"System.TimeSpan")
	{
		ExtRemoteData dt(Address + (MethodTable ? 0 : sizeof(void*)), sizeof(UINT64));
		UINT64 ticks = dt.GetUlong64();
		return tickstotimespan(ticks);
	}
	if(methName == L"System.Guid")
	{
		return SpecialCases::ToGuid(Address + (MethodTable ? 0 : sizeof(void*)));
	}
	if(methName == L"System.Uri")
	{
		std::vector<std::string> fields;
		fields.push_back("m_String");
		varMap fieldV;
		DumpFields(Address,fields,0,&fieldV);
		string ret = CW2A(fieldV["m_String"].strValue.c_str());
		return ret;
	}

	if(methName == L"System.Net.IPEndPoint" || methName == L"System.Net.IPAddress")
	{
		string ret = CW2A(SpecialCases::IPAddress(Address).c_str());
		return ret;
	}

	if(methName == L"System.DBNull")
	{
		return "NULL";
	}

	if(obj.IsArray())
	{
		if(obj.InnerComponentType() == ELEMENT_TYPE_STRING)
		{
			string tmpString;
			if(obj.NumComponents() > 0)
				tmpString = "{ ";
			for(int i=0;static_cast<DWORD>(i)<min(5,obj.NumComponents());i++)
			{
				if(i>0) tmpString.append(", ");
				CLRDATA_ADDRESS address = ObjDetail::GetPTR(obj.DataPtr() + i*obj.InnerComponentSize());

				if(NULL == address)
				{
					tmpString.append("null");
				} else
				{
					string part(CW2A(ObjDetail::String(address).c_str()));
					tmpString.append("\"");
					for(int k=0;k<min(30,part.size());k++)
					{
						if(part[k] < 32 || part[k] > 127)
						{
							char buff[8] = {0};
							sprintf_s(buff, 8, "\\x%02x", part[k]);
							tmpString.append(buff);
						} else
						{
							if(part[k] == '\\' || part[k] == '"')
								tmpString.append("\\");
							tmpString.append(part.substr(k, 1));

						}
					}
					if(part.size() > 30) tmpString.append("...");
					tmpString.append("\"");
				}
			}
			if(obj.NumComponents() > 5) tmpString.append(", ...");
			tmpString.append(" }");
			return tmpString;
		}
		string tmpString = GetHexArray(obj.Address(), true, 32);
		if(tmpString.size() != 0)  // This is for future expansions, it will only return if there is a match
			return tmpString;
	}

	return "";
}

std::string SpecialCases::GetRawArray(CLRDATA_ADDRESS Obj, bool IsNoAdjusted)
{
	string result="";

	ObjDetail obj(Obj);
	if(!obj.IsValid())
		return result;
	std::wstring className = obj.TypeName();

	if(obj.NumComponents() == 0)
		return result;
	ExtRemoteData buff(obj.DataPtr(), obj.InnerComponentSize() * obj.NumComponents());
	ULONG maxBytes = min(65536, obj.InnerComponentSize() * obj.NumComponents());
	if(className == L"System.Byte[]")
	{
		char* rawBuf = new char[maxBytes];
		ZeroMemory(rawBuf, maxBytes);
		try
		{
			maxBytes = buff.ReadBuffer(rawBuf, maxBytes);
			if(maxBytes != 0) rawBuf[maxBytes - 1]=0;

		} catch (...)
		{
			rawBuf[0]=0;
		}
		result.append(rawBuf);
		delete[] rawBuf;

	}
	if(className == L"System.Char[]")
	{

		wchar_t* rawBuf = new wchar_t[obj.NumComponents()];
		ZeroMemory(rawBuf, maxBytes);
		try
		{
			maxBytes = buff.ReadBuffer(rawBuf, maxBytes);
			if(maxBytes != 0) rawBuf[(maxBytes / 2) - 1]=0;

		} catch(...)
		{
			rawBuf[0]=0;
		}
		result.append(CW2A(rawBuf));
			
		delete[] rawBuf;

	}
	if(!IsNoAdjusted)
		PrintableString(result, true);
	return result;
}
SVAL SpecialCases::GetDbgVar(int DBGVar)
{
	SVAL v;
	if(DBGVar <0 || DBGVar > 19)
	{
		v.MakeInvalid();
	}
	string var="@$t";
	var.append(CW2A(formatnumber((UINT64)DBGVar).c_str()));
	v.SetPtr(g_ExtInstancePtr->EvalExprU64(var.c_str()));
	v.fieldName = var;
	return v;
}
bool SpecialCases::SetDbgVar(int DBGVar, SVAL v)
{
	if(DBGVar <0 || DBGVar > 19)
	{
		return false;
	}
	string var="r @$t";
	var.append(CW2A(formatnumber((UINT64)DBGVar).c_str()));
	var.append("=");
	var.append(formathex(v.Value.u64));
	EXT_CLASS::Execute(var);
	return true;
}

std::string SpecialCases::ToGuid(CLRDATA_ADDRESS Addr)
{
	UUID guid;
	ZeroMemory(&guid,sizeof(guid));
	ULONG read;
	ReadMemory(Addr, &guid, sizeof(guid), &read);

	RPC_CSTR guidStr = NULL;
	UuidToStringA(&guid, &guidStr);
	std::string strGuid;
	if(guidStr != NULL)
	{
		strGuid = "{";
		strGuid.append((char*)guidStr);
		strGuid.append("}");
		::RpcStringFreeA(&guidStr);
	}
	return strGuid;
}

INT64 SpecialCases::TicksFromTarget()
{
	ULONG t = 0;
	g_ExtInstancePtr->m_Control2->GetCurrentTimeDate(&t);
	UINT64 v = Ticksfrom1970 + TicksPerSecond*t + TicksMask + 1;

	return v;
}
INT64 SpecialCases::DateToTicks(INT64 year, INT64 month, INT64 day)
{
	INT64 v=0;
	if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
	{
		int* days;
		if(IsLeapYear(static_cast<int>(year)))
			days = DaysToMonth366;
		else
			days = DaysToMonth365;
		if (day >= 1 && day <= days[month] - days[month - 1])
		{
			INT64 y = year - 1;
			INT64 n = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
			v = (INT64)(n * TicksPerDay);
		}
	}

	return v;
}
INT64 SpecialCases::TimeToTicks(INT64 hour, INT64 minute, INT64 second)
{
	INT64 v = hour*TicksPerHour + minute*TicksPerMinute + second*TicksPerSecond;
	return v;
}

// Dump Array
void SpecialCases::DumpArray(CLRDATA_ADDRESS Address, CLRDATA_ADDRESS MethodTable, int Start, int End, bool ShowIndex, ObjDetail *Obj, bool shownull)
{
	ObjDetail obj;
	ObjDetail *pObj = Obj;
	if(Obj==NULL)
	{
		if(MethodTable == 0)
		{
			obj.Request(Address);
		} else
		{
			obj.Request(Address, MethodTable);
		}
		pObj = &obj;
	}
	if(pObj->NumComponents()==0) return; // nothing to do here
	DWORD start = (DWORD)Start;
	DWORD end =  End < 0 ? pObj->NumComponents() - 1 : min((DWORD)End, pObj->NumComponents() - 1);
	DWORD rank = pObj->Rank();
	while(start <= end)
	{

		CorElementType eType = pObj->InnerComponentType();

		CLRDATA_ADDRESS itemAddr = pObj->DataPtr() + start* pObj->InnerComponentSize();
		ObjDetail innerObj;

		if(eType == ELEMENT_TYPE_CLASS || eType == ELEMENT_TYPE_OBJECT)
		{
			if(itemAddr!=NULL && ObjDetail::GetPTR(itemAddr) != NULL)
			{
				innerObj.Request(ObjDetail::GetPTR(itemAddr));
				if(innerObj.IsValid())
				{
					if(innerObj.IsString())
					{
						eType = ELEMENT_TYPE_STRING;
					}
				}
			}
		}
		if(IsInterrupted())
			return;
		if(!shownull && IsCorObj(eType))
		{
			CLRDATA_ADDRESS addr = ObjDetail::GetPTR(itemAddr);
			if(addr==NULL)
			{
				start++;
				continue;
			}
		}
		if(ShowIndex)
		{
			PrintArrayIndex(Address, start, rank);
		}
		MD_FieldData dummyField;
		dummyField.corElementType = eType;
		dummyField.MethodTable = pObj->InnerMT();
		std::wstring vl = ObjDetail::ValueString(dummyField, itemAddr, true, itemAddr);
		if(eType == ELEMENT_TYPE_VALUETYPE)
		{
			string pretty = SpecialCases::PrettyPrint(itemAddr, pObj->InnerMT());
			if(pretty.size() > 0)
				vl.assign(CA2W(pretty.c_str()));
			g_ExtInstancePtr->Dml("<link cmd=\"!wdo -mt %p %p\">%S</link>",pObj->InnerMT(), itemAddr,
				vl.c_str());
		} else
			if(IsCorObj(eType) || eType == ELEMENT_TYPE_STRING)
			{
				CLRDATA_ADDRESS addr=ObjDetail::GetPTR(itemAddr);

				g_ExtInstancePtr->Dml("<link cmd=\"!wdo %p\">%p</link>",addr, addr);
				if(eType == ELEMENT_TYPE_STRING)
					g_ExtInstancePtr->Out(" %S", vl.c_str());
				else
				{
					string pretty = SpecialCases::PrettyPrint(addr);
					if(pretty.size() > 0)
						g_ExtInstancePtr->Out(" %s", pretty.c_str());
				}
			} else
				g_ExtInstancePtr->Out("%S", vl.c_str());
			g_ExtInstancePtr->Out("\n");
			start++;
	}
}

void DumpNamedKeys(CLRDATA_ADDRESS Address, string Key, namedKey *Keys)
{
	if(Address ==NULL) return;
	ObjDetail obj;

	obj.Request(Address);
	if(!obj.classObj.Implement(L"System.Collections.Specialized.NameObjectCollectionBase"))
	{
		if(!Keys)
			g_ExtInstancePtr->Warn("The class %S does not inherits from System.Collections.Specialized.NameObjectCollectionBase\n", obj.TypeName().c_str());
		return;
	}
	if(!Keys && Key.size() == 0) g_ExtInstancePtr->Out("[%S]\n", obj.TypeName().c_str());
	std::vector<std::string> fields;
	std::vector<std::string> innerFields;


	varMap fieldV;

	fields.push_back("_entriesArray._items");


	DumpFields(Address, fields, 0, &fieldV);
	if(fieldV["_entriesArray._items"].Value.ptr == NULL)
	{
		if(!Keys)
			g_ExtInstancePtr->Warn("The Key/Pair array is empty\n");
		return;
	}

	std::vector<CLRDATA_ADDRESS> items;
	SpecialCases::EnumArray(fieldV["_entriesArray._items"].Value.ptr,0,0,&items);

	std::vector<CLRDATA_ADDRESS>::const_iterator it;
	fields.clear();

	fields.push_back("Key");
	fields.push_back("Value");
	fields.push_back("Value._items");



	for (it=items.begin(); it!=items.end(); it++)
	{
		if(*it != NULL)
		{
			fieldV.clear();
			DumpFields(*it, fields, 0, &fieldV);
			ObjDetail obj;
			CLRDATA_ADDRESS ptr = fieldV["Value"].Value.ptr;
			CLRDATA_ADDRESS mt = NULL;
			if (fieldV["Value"].IsValueType) mt = fieldV["Value"].MT;
			vector<CLRDATA_ADDRESS> innerItems;
			vector<CLRDATA_ADDRESS>::const_iterator inner;
			if(ptr != NULL)
			{
				// Get Inner Array instead if it is a multiple value Key
				if(fieldV["Value._items"].Value.ptr != NULL)
				{
					mt=0;
					ptr=fieldV["Value._items"].Value.ptr;
				}
				if(mt)
					obj.Request(ptr, mt);
				else
					obj.Request(ptr);

				if(obj.IsArray())
				{
					SpecialCases::EnumArray(ptr, mt, 0, &innerItems);

				} else
				{
					innerItems.push_back(ptr);
				}
			} else
			{
				innerItems.push_back(NULL); // show NULL if it is null
			}

			string key = CW2A(fieldV["Key"].strValue.c_str());
			if(Key.size() > 0) key = Key; // override the key for a HttpCollection case
			for (inner=innerItems.begin(); inner!=innerItems.end(); inner++)
			{
				ObjDetail obj1;

				varMap innerFieldV;
				if(*inner!=NULL)
				{


					if(mt)
						obj1.Request(*inner, mt);
					else
						obj1.Request(*inner);

					std::vector<FieldStore> fstore = obj1.GetFieldsByName("*");
					std::vector<FieldStore>::const_iterator fs;
					innerFields.clear();
					for (fs=fstore.begin(); fs!=fstore.end(); ++fs)
					{
						string s = CW2A(fs->FieldName.c_str());
						innerFields.push_back(s);
					}
					DumpFields(ptr, innerFields, mt, &innerFieldV);
				}
				if(!Keys)
				{

					if(Key.size() == 0) g_ExtInstancePtr->Out("=====================================\n");
					g_ExtInstancePtr->Out("%s=",key.c_str());
					if(obj1.IsString())
					{
						g_ExtInstancePtr->Out("%S", obj1.String().c_str());
					}
					else
					{
						if(Key.size() == 0)
						{
							g_ExtInstancePtr->Out("[%p:", *inner);
							g_ExtInstancePtr->Out("%S]", obj1.TypeName().c_str());
						}
						PrintCompact(innerFieldV);
					}
					g_ExtInstancePtr->Out("\n");
				} else
				{
					if(Keys->count(key) == 0)
					{
						vector<SVAL> empty;
						(*Keys)[key] = empty;
					}
					if(obj1.IsString())
					{
						SVAL v;
						wstring st = obj1.String();
						v = st;
						(*Keys)[key].push_back(v);
					}
					else
					{
						/*if(fieldV["Value._items"].Value.ptr != NULL)
						{

						(*Keys)[key].push_back(fieldV["Value._items"]);						}
						else
						{
						(*Keys)[key].push_back(fieldV["Value"]);
						}*/
						(*Keys)[key].push_back(fieldV["Value"]);
					}
				}
			}
		}
	}

}

void DumpFields(CLRDATA_ADDRESS Address, std::vector<std::string> Fields, CLRDATA_ADDRESS MethodTable, varMap* Vars)
{
	ObjDetail obj;
	if(MethodTable == 0)
		obj.Request(Address);
	else
		obj.Request(Address, MethodTable);

	vector<FieldStore> fields;
	for(int s=0; s < Fields.size(); s++)
	{
		ObjDetail *currObj = (ObjDetail *)&obj;
		ObjDetail tempObj;
		vector<string> fieldsDepth;
		bool p=boost::spirit::qi::parse(Fields[s].begin(), Fields[s].end(),
			+(boost::spirit::qi::alnum | '_' | ':' | '$' | '[' | ']' | '*' | '?') % '.' ,
			fieldsDepth);
		if(fieldsDepth.size() <= 1)
		{
			fields = obj.GetFieldsByName(Fields[s]);
			if(fields.size() > 1)
			{
				fields.resize(1);
			}
		}
		else
		{
			vector<string>::const_iterator it = fieldsDepth.begin();
			while(it!=fieldsDepth.end())
			{
				fields = currObj->GetFieldsByName(*it);
				if(fields.size() > 1)
				{
					fields.resize(1);
				}
				if(fields.size() != 1)
				{
					if(!Vars) g_ExtInstancePtr->Err("Field %s: <INVALID>\n", Fields[s].c_str());
					break;
				}
				if(it == fieldsDepth.end()-1)
					break;
				if(IsCorObj((CorElementType)fields[0].FieldDesc.corElementType))
				{
					CLRDATA_ADDRESS offset = ObjDetail::GetFieldAddress(currObj->Address(), fields[0].FieldDesc, currObj->IsValueType(), currObj);
					CLRDATA_ADDRESS fieldAddr = ObjDetail::GetPTR(offset);
					if(!tempObj.Request(fieldAddr))
					{
						fields.clear();
						if(!Vars) g_ExtInstancePtr->Err("%s: <INVALID>\n", Fields[s].c_str());
						break;
					}
					currObj = (ObjDetail*)&tempObj;

					if(!currObj->IsValid())
					{
						// obj is NULL by some extent
						break;
					}
				} else
					if(fields[0].FieldDesc.corElementType == ELEMENT_TYPE_VALUETYPE)
					{
						if(!tempObj.Request(ObjDetail::GetFieldAddress(currObj->Address(), fields[0].FieldDesc, currObj->IsValueType(), currObj), fields[0].FieldDesc.MethodTable))
						{
							fields.clear();
							if(!Vars) g_ExtInstancePtr->Err("%s: <INVALID>\n", Fields[s].c_str());
							break;
						}

						currObj = (ObjDetail*)&tempObj;
						if(!currObj->IsValid())
						{
							// obj is NULL by some extent
							break;
						}
					} else
						// it is not a pointer so it has to be last
					{
						if(*it != *fieldsDepth.rbegin())
						{
							if(!Vars) g_ExtInstancePtr->Err("Field %S: <INVALID>\n", Fields[s]);
							break;
						}
					}
					it++;
			}
			if(fields.size()>0) fields[0].FieldName.assign(CA2W(Fields[s].c_str()));
		}
		for(int i=0; i<fields.size(); i++)
		{
			if(!Vars)
			{
				if(fields[i].FieldDesc.isStatic && !fields[i].FieldDesc.isThreadStatic)
				{
					if(Vars) g_ExtInstancePtr->Out("static ");
				}
				if(fields[i].FieldDesc.isThreadStatic)
				{
					if(Vars) g_ExtInstancePtr->Out("thread local ");
				}
			}
			if(IsCorObj((CorElementType)fields[i].FieldDesc.corElementType))
			{
				CLRDATA_ADDRESS ptr = ObjDetail::GetPTR(ObjDetail::GetFieldAddress(currObj->Address(), fields[i].FieldDesc, currObj->IsValueType(),currObj));
				CLRDATA_ADDRESS ptr1 = ObjDetail::GetFieldAddress(currObj->Address(), fields[i].FieldDesc, currObj->IsValueType(),currObj);
				if(fields[i].mtName == L"System.Object" || fields[i].mtName == L"System.__Canon")
				{
					if(!Vars) g_ExtInstancePtr->Dml("%S %S = <link cmd=\"!wselect * from %p\">%p</link>", fields[i].mtName.c_str(), fields[i].FieldName.c_str(), ptr, ptr);
					else
						(*Vars)[(string)CW2A(fields[i].FieldName.c_str())]=GetValue(ptr1, (CorElementType)fields[i].FieldDesc.corElementType);

					if(ptr!=NULL)
					{
						ObjDetail objTemp(ptr);
						if(objTemp.IsValid())
						{
							if(objTemp.TypeName() == L"System.String")
							{
								if(!Vars) g_ExtInstancePtr->Out(" %S",
									objTemp.String().c_str());
								else
								{
									(*Vars)[(string)CW2A(fields[i].FieldName.c_str())]=GetValue(ptr1, ELEMENT_TYPE_STRING);
									fields[i].FieldDesc.corElementType = ELEMENT_TYPE_STRING;
								}
							} else
							{
								if(!Vars)
								{
									string tmpString = SpecialCases::PrettyPrint(ptr);
									if(tmpString.size() > 0)
									{
										g_ExtInstancePtr->Out(" %s",
											tmpString.c_str());
									}
								}
							}
						}
					}
					if(!Vars) g_ExtInstancePtr->Out("\n");
				} else
					if(fields[i].FieldDesc.isString)
					{
						if(!Vars) g_ExtInstancePtr->Dml("%S %S = <link cmd=\"!wselect * from %p\">%p</link> %S\n", fields[i].mtName.c_str(), fields[i].FieldName.c_str(), ptr, ptr,
							currObj->ValueString(fields[i].FieldDesc, currObj->Address(), currObj->IsValueType()).c_str());
						else
						{
							(*Vars)[(string)CW2A(fields[i].FieldName.c_str())]=GetValue(ptr1, ELEMENT_TYPE_STRING);
							fields[i].FieldDesc.corElementType = ELEMENT_TYPE_STRING;
						}
					} else
					{
						string tmpString = SpecialCases::PrettyPrint(ptr);
						if(!Vars)
						{
							if(tmpString.size() == 0)
								g_ExtInstancePtr->Dml("%S %S = <link cmd=\"!wselect * from %p\">%S</link>\n", fields[i].mtName.c_str(), fields[i].FieldName.c_str(), ptr,
								currObj->ValueString(fields[i].FieldDesc, currObj->Address(), currObj->IsValueType()).c_str());
							else
								g_ExtInstancePtr->Dml("%S %S = <link cmd=\"!wselect * from %p\">%s</link>\n", fields[i].mtName.c_str(), fields[i].FieldName.c_str(), ptr,
									tmpString.c_str());
						}
						else
							(*Vars)[(string)CW2A(fields[i].FieldName.c_str())]=GetValue(ptr1, (CorElementType)fields[i].FieldDesc.corElementType);
						if(!Vars)
						{
							string tmpString = SpecialCases::PrettyPrint(ptr);
							if(tmpString.size() > 0)
							{
								g_ExtInstancePtr->Out(" %s",
								tmpString.c_str());
							}

						}

					}
			} else
				if(fields[i].FieldDesc.corElementType == ELEMENT_TYPE_VALUETYPE)
				{
					CLRDATA_ADDRESS ptr = ObjDetail::GetFieldAddress(currObj->Address(), fields[i].FieldDesc, currObj->IsValueType(),currObj);
					//Dml("<link cmd=\"!wdo -mt %p %p\">%S</link>", field->MTOfType ,ptr, ObjDetail::ValueString(*field, addr, true).c_str());
					if(!Vars)
					{
						string pp = SpecialCases::PrettyPrint(ptr, fields[i].FieldDesc.MethodTable);

						if(pp.size() > 0)
						{
							
							g_ExtInstancePtr->Dml("%S %S = <link cmd=\"!wselect mt %p * from %p\">%s</link>", fields[i].mtName.c_str(), fields[i].FieldName.c_str(),
								fields[i].FieldDesc.MethodTable, ptr,
								pp.c_str());

						} else
						{

			
							g_ExtInstancePtr->Dml("%S %S = <link cmd=\"!wselect mt %p * from %p\">%S</link>", fields[i].mtName.c_str(), fields[i].FieldName.c_str(),
								fields[i].FieldDesc.MethodTable, ptr,
								currObj->ValueString(fields[i].FieldDesc, currObj->Address(), currObj->IsValueType()).c_str());
						}
						//wstring methName = GetMethodName(fields[i].FieldDesc.MethodTable);
						/*
						if(methName == L"System.DateTime")
						{
							ExtRemoteData dt(ptr, sizeof(UINT64));
							UINT64 ticks = dt.GetUlong64();
							g_ExtInstancePtr->Out(" %s", tickstodatetime(ticks).c_str());
						}
						if(methName == L"System.TimeSpan")
						{
							ExtRemoteData dt(ptr, sizeof(UINT64));
							UINT64 ticks = dt.GetUlong64();
							g_ExtInstancePtr->Out(" %s", tickstotimespan(ticks).c_str());
						}
						if(methName == L"System.Guid")
						{
							g_ExtInstancePtr->Out(" %s",SpecialCases::ToGuid(ptr).c_str());
						}
						*/
						g_ExtInstancePtr->Out("\n");
					}
					else
						(*Vars)[(string)CW2A(fields[i].FieldName.c_str())]=GetValue(ptr, (CorElementType)fields[i].FieldDesc.corElementType, fields[i].FieldDesc.MethodTable);
				} else
				{
					CLRDATA_ADDRESS ptr = ObjDetail::GetFieldAddress(currObj->Address(), fields[i].FieldDesc, currObj->IsValueType(),currObj);

					if(!Vars)
					{
						g_ExtInstancePtr->Out("%S %S = %S", fields[i].mtName.c_str(), fields[i].FieldName.c_str(),
							obj.ValueString(fields[i].FieldDesc, currObj->Address(), currObj->IsValueType()).c_str());
						if(IsInt((CorElementType)fields[i].FieldDesc.corElementType) && SpecialCases::IsEnumType(fields[i].FieldDesc.MethodTable))
						{
							UINT64 v;
							ExtRemoteData dt(ptr,IntSize((CorElementType)fields[i].FieldDesc.corElementType));

							switch(IntSize((CorElementType)fields[i].FieldDesc.corElementType))
							{
							case 1:
								v = dt.GetUchar();
								break;
							case 2:
								v = dt.GetUshort();
								break;
							case 4:
								v = dt.GetUlong();
								break;
							case 8:
								v = dt.GetUlong64();
								break;
							}
							g_ExtInstancePtr->Out(" %S", SpecialCases::GetEnumString(fields[i].FieldDesc, v).c_str());
						}
						g_ExtInstancePtr->Out("\n");
					}
					else
						(*Vars)[(string)CW2A(fields[i].FieldName.c_str())]=GetValue(ptr, (CorElementType)fields[i].FieldDesc.corElementType, fields[i].FieldDesc.MethodTable);
				}
				if(Vars)
				{
					string key = CW2A(fields[i].FieldName.c_str());
					(*Vars)[key].fieldName=key;
					(*Vars)[key].corType = (CorElementType)fields[i].FieldDesc.corElementType;
					(*Vars)[key].typeName = fields[i].mtName;
					(*Vars)[key].IsStatic = fields[i].FieldDesc.isStatic != 0;
					(*Vars)[key].Offset = fields[i].FieldDesc.offset;
					(*Vars)[key].MT = fields[i].FieldDesc.MethodTable;
					(*Vars)[key].Token = fields[i].FieldDesc.token;
					(*Vars)[key].Module = fields[i].FieldDesc.Module;
				}
		}
	}
};

SVAL GetValue(CLRDATA_ADDRESS offset, CorElementType CorType, CLRDATA_ADDRESS MethodTable, const char* ObjectString, const char* ValueTypeString, bool Print)
{
	SVAL response;
	response.corBaseType = response.corType = CorType;
	try
	{
		response.ObjAddress = offset;
		response.IsValueType = (CorType == ELEMENT_TYPE_VALUETYPE);
		response.MT = MethodTable;
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
		case ELEMENT_TYPE_ARRAY			:
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
					response.strValue.assign(NameBuffer);
					response.Value.ptr = NULL;
					return response;
				}
				CLRDATA_ADDRESS addr = ptr.GetPtr();
				swprintf(NameBuffer, MAX_MTNAME, L"%p", addr);
				response.Value.ptr = addr;
				response.prettyPrint.assign(NameBuffer);
				response.corBaseType = ELEMENT_TYPE_PTR;
				if(Print)
				{
					g_ExtInstancePtr->Dml(ObjectString, addr, addr, addr);
				}
				response.Value.ptr=addr;
				response.DoubleValue = static_cast<double>(addr);
				response.strValue.assign(NameBuffer);
				return response;
			}
			break;
		case ELEMENT_TYPE_BOOLEAN       :
			//return "bool";
			{
				ExtRemoteData ptr(offset, sizeof(BOOLEAN));
				USHORT u=ptr.GetBoolean();
				if(u>0)
				{
					swprintf(NameBuffer, MAX_MTNAME, L"%x", u);
				} else
				{
					swprintf(NameBuffer, MAX_MTNAME, L"%x", u);
				}
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				response.Value.b=(u != 0);
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
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
				response.Value.ch=ch;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)ch;
				return response;
			}
			break;
		case ELEMENT_TYPE_I1            :
			//return "int8";
			{
				ExtRemoteData ptr(offset, sizeof(BYTE));
				INT u=(INT)(BYTE)ptr.GetBoolean();

				swprintf(NameBuffer, MAX_MTNAME, L"0n%i", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				response.Value.bt=(char)u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
			}
			break;
		case ELEMENT_TYPE_U1            :
			{
				ExtRemoteData ptr(offset, sizeof(BYTE));
				UINT u=(UINT)ptr.GetBoolean();
				swprintf(NameBuffer, MAX_MTNAME, L"0n%u", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				response.Value.ubt=(BYTE)u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
			}
			break;
			//return "uint8";
		case ELEMENT_TYPE_I2            :
			{
				ExtRemoteData ptr(offset, sizeof(SHORT));
				INT u=(INT)ptr.GetShort();
				swprintf(NameBuffer, MAX_MTNAME, L"0n%i", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				response.Value.sh=(short)u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
			}
			break;
			//return "int16";
		case ELEMENT_TYPE_U2            :
			{
				ExtRemoteData ptr(offset, sizeof(SHORT));
				UINT u=(UINT)ptr.GetShort();
				swprintf(NameBuffer, MAX_MTNAME, L"0n%u", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				response.Value.ush=(USHORT)u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
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
				swprintf(NameBuffer, MAX_MTNAME, L"0n%i", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				response.corBaseType = ELEMENT_TYPE_I4;
				response.Value.i32=u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
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
				swprintf(NameBuffer, MAX_MTNAME, L"0n%u", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}

				response.corBaseType = ELEMENT_TYPE_U4;
				response.Value.u32=u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
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
				swprintf(NameBuffer, MAX_MTNAME, L"0n%I64i", u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				response.corBaseType = ELEMENT_TYPE_I8;
				response.Value.i64=u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
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
				swprintf(NameBuffer, MAX_MTNAME, L"%I64i", u, u);
				if(Print)
				{
					g_ExtInstancePtr->Out("%S", NameBuffer);
				}
				response.corBaseType = ELEMENT_TYPE_U8;
				response.Value.u64=u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
			}
			break;
		case ELEMENT_TYPE_STRING        :
			{
				ExtRemoteData ptr(offset, sizeof(void*));
				CLRDATA_ADDRESS p = ptr.GetPtr();
				response.strValue.clear();
				if(Print)
				{
					g_ExtInstancePtr->Dml(ObjectString, p, p);
					g_ExtInstancePtr->Out(" %S", ObjDetail::FullString(p).c_str());
				}
				if(!p)
					response.strValue = L"NULL";
				else
					response.strValue = ObjDetail::FullString(p);

				response.Value.ptr=p;
				//response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)p;

				return response;
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
				response.Value.ft=u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
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
				response.Value.db=u;
				response.strValue.assign(NameBuffer);
				response.DoubleValue = (double)u;

				return response;
			}
			break;
		case ELEMENT_TYPE_VALUETYPE    :
			{
				//ExtRemoteData ptr(offset, sizeof(double));
				//double u=ptr.GetDouble();
				swprintf(NameBuffer, MAX_MTNAME, L"-mt %p %p", MethodTable, offset); // return pointer to value type and the method table
				if(Print)
				{
					g_ExtInstancePtr->Dml(ValueTypeString, MethodTable, offset, NameBuffer);
				}
				response.Value.ptr=offset;
				response.strValue.assign(NameBuffer);
				//response.DoubleValue = offset;

				return response;
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
			response.strValue = L"Unknown";
			response.Value.ptr = NULL;
			response.MakeInvalid();
			return response;
			break;
		} // end switch
	} catch(...)
	{
		swprintf(NameBuffer, MAX_MTNAME, L"<Unable to read at %p>", offset);
	}
	response.strValue.assign(NameBuffer);
	response.Value.ptr = NULL;
	response.MakeInvalid();
	return response;
}

std::string StrOrEmpty(const char *Text)
{
	if(Text[0] == '\0')
		return "(empty)";
	return Text;
}

void SpecialCases::StringXml( TiXmlNode * Parent, std::string& Result, unsigned int indent)
{
	if ( !Parent ) return;



	TiXmlNode::NodeType t = (TiXmlNode::NodeType)Parent->Type();
	std::string indentstr;
	if(indent > 0)
	{
		if(indent > 1)
		{
			indentstr.insert(0,indent,' ');

		}
		indentstr += "+- ";
		Result += indentstr;
	}

	switch ( t )
	{
	case TiXmlNode::TINYXML_DOCUMENT:
		Result += "DOCUMENT";
		break;

	case TiXmlNode::TINYXML_ELEMENT:
		{
			Result += "ELEMENT \"";
			Result.append(Parent->Value());
			Result += "\"";
			TiXmlElement *elem = Parent->ToElement();
			TiXmlAttribute* attrib=elem->FirstAttribute();

			while (attrib)
			{
				Result += ", ";
				Result += attrib->Name();
				Result += ": ";
				Result += StrOrEmpty(attrib->Value());

				attrib=attrib->Next();
			}
		}
		break;

	case TiXmlNode::TINYXML_COMMENT:
		Result += "COMMENT: /* ";
		Result.append(Parent->Value());
		Result.append(" */");
		break;

	case TiXmlNode::TINYXML_UNKNOWN:
		Result += "?UNKNOW";
		break;

	case TiXmlNode::TINYXML_TEXT:
		{
			TiXmlText *text = Parent->ToText();
			Result += "TEXT: [";
			Result.append(StrOrEmpty(text->Value()));
			Result += ']';
		}
		break;

	case TiXmlNode::TINYXML_DECLARATION:
		{
			Result += "DECLARATION version: ";
			TiXmlDeclaration *decl = Parent->ToDeclaration();
			Result += StrOrEmpty(decl->Version());
			Result += " encoding: ";
			Result += StrOrEmpty(decl->Encoding());
			Result += " standalone: ";
			Result += StrOrEmpty(decl->Standalone());
			break;
		}
	default:
		break;
	}
	Result += "\n";

	TiXmlNode * child;

	for ( child = Parent->FirstChild(); child != 0; child = child->NextSibling()) 
	{
		StringXml( child, Result, indent+2 );
	}
}

std::string SpecialCases::IndentedXml(std::wstring XmlDoc)
{
	TiXmlDocument doc;
	doc.Parse(CW2A(XmlDoc.c_str()));
	TiXmlPrinter printer;
	doc.Accept(&printer);
	return printer.CStr();
}

std::string SpecialCases::XmlTree(std::wstring XmlDoc)
{
	TiXmlDocument doc;
	doc.Parse(CW2A(XmlDoc.c_str()));
	std::string stru;
	StringXml(&doc, stru);
	return stru;
}



SVAL operator+(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v = (x.strValue+y.strValue);
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = x.DoubleValue + y.DoubleValue;
	else
		v = x1.Value.i64 + y1.Value.i64;
	return v;
}
SVAL operator-(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = x.DoubleValue - y.DoubleValue;
	else
		v = x1.Value.i64 - y1.Value.i64;
	return v;
}
SVAL operator*(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = x.DoubleValue * y.DoubleValue;
	else
		v = x1.Value.i64 * y1.Value.i64;
	return v;
}

SVAL operator/(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = x.DoubleValue / y.DoubleValue;
	else
		v = x1.Value.i64 / y1.Value.i64;
	return v;
}
SVAL operator%(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = ((INT64)x.DoubleValue) % ((INT64)y.DoubleValue);
	else
		v = x1.Value.u64 % y1.Value.u64;
	return v;
}
SVAL operator==(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v=(x.strValue == y.strValue);
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = (x.DoubleValue == y.DoubleValue);
	else
		v = x1.Value.u64 == y1.Value.u64;
	return v;
}
SVAL operator!=(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v=(x.strValue != y.strValue);
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = (x.DoubleValue != y.DoubleValue);
	else
		v = x1.Value.u64 != y1.Value.u64;
	return v;
};
SVAL operator>(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v=(x.strValue > y.strValue);
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = (x.DoubleValue > y.DoubleValue);
	else
		v = x1.Value.i64 > y1.Value.i64;
	return v;
};

SVAL operator<(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v=(x.strValue < y.strValue);
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = (x.DoubleValue < y.DoubleValue);
	else
		v = x1.Value.i64 < y1.Value.i64;
	return v;
};

SVAL operator>=(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v=(x.strValue >= y.strValue);
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = (x.DoubleValue >= y.DoubleValue);
	else
		v = x1.Value.i64 >= y1.Value.i64;
	return v;
};
SVAL operator<=(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v=(x.strValue <= y.strValue);
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = (x.DoubleValue <= y.DoubleValue);
	else
		v = x1.Value.i64 <= y1.Value.i64;
	return v;
};
SVAL operator&(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}

	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = ((INT64)x.DoubleValue & (INT64)y.DoubleValue);
	else
		v = x1.Value.u64 & y1.Value.u64;
	return v;
};
SVAL operator|(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}

	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = ((INT64)x.DoubleValue | (INT64)y.DoubleValue);
	else
		v = x1.Value.u64 | y1.Value.u64;
	return v;
};
SVAL operator&&(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}
	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = (x.DoubleValue && y.DoubleValue);
	else
		v = x1.Value.u64 && y1.Value.u64;
	return v;
};
SVAL operator||(const SVAL& x, const SVAL& y)
{
	SVAL v;

	CorElementType tp = SVAL::GetCast(x.corBaseType, y.corBaseType);
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}

	SVAL x1, y1;
	x1=x;
	y1=y;

	SVAL::Normalize(x1, y1);
	if(x1.IsReal() || y1.IsReal())
		v = (x.DoubleValue || y.DoubleValue);
	else
		v = x1.Value.u64 || y1.Value.u64;
	return v;
};
SVAL operator!(const SVAL& x)
{
	SVAL v;

	CorElementType tp = x.corBaseType;
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v = (NULL == v.Value.ptr);
		return v;
	}
	SVAL x1;
	x1=x;
	if(x1.IsInt())
		v=!x.Value.u64;
	else
		v = !((INT64)x.DoubleValue);

	return v;
};
SVAL operator-(const SVAL& x)
{
	SVAL v;

	CorElementType tp = x.corBaseType;
	if(tp==ELEMENT_TYPE_END)
	{
		v.MakeInvalid();
		return v;
	}
	if(tp==ELEMENT_TYPE_STRING)
	{
		v.MakeInvalid();
		return v;
	}
	SVAL x1;
	x1=x;
	if(x1.IsReal())
	{
		v = -x.DoubleValue;
	} else v=-x.Value.i64;
	return v;
}

std::ostream& operator<< (std::ostream &os, SVAL& val)
{
	os << CW2A(val.prettyPrint.c_str());
	if(val.IsInt() || (val.IsReal() && (val.DoubleValue - (INT64)val.DoubleValue ==0)))
	{
		swprintf(NameBuffer, MAX_MTNAME, L"%I64x", (INT64)val.DoubleValue);

		os << " 0x" << CW2A(NameBuffer);
	}
	return os;
}
