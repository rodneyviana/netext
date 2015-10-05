/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#pragma once
#include "CLRHelper.h"
#include "tinyXml.h"
#include <rpc.h>
// Const below are adapted from SSCLI20\src\bcl\system\datetime.cs
const UINT64 TicksPerMillisecond = 10000;
const UINT64 TicksPerSecond = TicksPerMillisecond * 1000;
const UINT64 TicksPerMinute = TicksPerSecond * 60;
const UINT64 TicksPerHour = TicksPerMinute * 60;
const UINT64 TicksPerDay = TicksPerHour * 24;

// Number of milliseconds per time unit
const UINT64 MillisPerSecond = 1000;
const UINT64 MillisPerMinute = MillisPerSecond * 60;
const UINT64 MillisPerHour = MillisPerMinute * 60;
const UINT64 MillisPerDay = MillisPerHour * 24;

// Number of days in a non-leap year
const int DaysPerYear = 365;

// Number of days in 4 years
const int DaysPer4Years = DaysPerYear * 4 + 1;

// Number of days in 100 years
const int DaysPer100Years = DaysPer4Years * 25 - 1;

// Number of days in 400 years
const int DaysPer400Years = DaysPer100Years * 4 + 1;

// Number of days from 1/1/0001 to 12/31/1600
const int DaysTo1601 = DaysPer400Years * 4;

// Number of days from 1/1/0001 to 12/30/1899
const int DaysTo1899 = DaysPer400Years * 4 + DaysPer100Years * 3 - 367;

// Number of days from 1/1/0001 to 12/31/9999
const int DaysTo10000 = DaysPer400Years * 25 - 366;

const UINT64 MinTicks = 0;
const UINT64 MaxTicks = DaysTo10000 * TicksPerDay - 1;
const UINT64 MaxMillis = (INT64)DaysTo10000 * MillisPerDay;

const UINT64  FileTimeOffset = DaysTo1601 * TicksPerDay;
const UINT64  DoubleDateOffset = DaysTo1899 * TicksPerDay;
const ULONG64 TicksMask = 0x3FFFFFFFFFFFFFFF;
extern int DaysToMonth365[13];
extern int DaysToMonth366[13];
// End Time Constants from DateTime.cs

const UINT64 Ticksfrom1970 = 621355968000000000;

using namespace NetExtShim;
using namespace std;

inline bool IsCorObj(CorElementType Sig)
{
	return			(Sig == ELEMENT_TYPE_PTR ||
						Sig == ELEMENT_TYPE_BYREF ||
						Sig == ELEMENT_TYPE_TYPEDBYREF ||
						Sig == ELEMENT_TYPE_OBJECT ||
						Sig == ELEMENT_TYPE_VOID ||
						Sig == ELEMENT_TYPE_GENERICINST ||
						Sig == ELEMENT_TYPE_CLASS ||
						Sig == ELEMENT_TYPE_SZARRAY ||
						Sig == ELEMENT_TYPE_ARRAY ||
						Sig == ELEMENT_TYPE_VAR ||
						Sig == ELEMENT_TYPE_MVAR ||
						Sig == ELEMENT_TYPE_FNPTR ||
						Sig == ELEMENT_TYPE_PINNED);
}

inline bool IsInt(CorElementType Sig)
{
	return			( (Sig >= ELEMENT_TYPE_I1 && Sig <= ELEMENT_TYPE_U8) ||
						Sig == ELEMENT_TYPE_I || Sig == ELEMENT_TYPE_U );
}

inline bool IsReal(CorElementType Sig)
{
	return (Sig == ELEMENT_TYPE_R4) || (Sig == ELEMENT_TYPE_R8);
}

inline bool IsSigned(CorElementType Sig)
{
	return IsInt(Sig) && (Sig % 2==0);
}

inline bool IsUnsigned(CorElementType Sig)
{
	return IsInt(Sig) && (Sig % 2==1);
}

inline int IntSize(CorElementType Sig)
{
	if(IsCorObj(Sig))
		return sizeof(void*);
	switch(Sig)
	{
	case ELEMENT_TYPE_I1:
	case ELEMENT_TYPE_U1:
		return 1;
	case ELEMENT_TYPE_I2:
	case ELEMENT_TYPE_U2:
		return 2;
	case ELEMENT_TYPE_I4:
	case ELEMENT_TYPE_U4:
		return 4;
	case ELEMENT_TYPE_I8:
	case ELEMENT_TYPE_U8:
		return 8;
	case ELEMENT_TYPE_I:
	case ELEMENT_TYPE_U:
		return sizeof(void*);
	}
	return 0;
}

union CLRValue
{
	// ELEMENT_TYPE_PTR
	CLRDATA_ADDRESS ptr;
	// ELEMENT_TYPE_BOOLEAN
	bool b;
	// ELEMENT_TYPE_CHAR
	WCHAR ch;
	// ELEMENT_TYPE_I1
	char bt;
	// ELEMENT_TYPE_U1
	BYTE ubt;
	// ELEMENT_TYPE_I2
	SHORT sh;
	// ELEMENT_TYPE_U2
	USHORT ush;
	// ELEMENT_TYPE_I4
	INT32 i32;
	// ELEMENT_TYPE_U4
	UINT32 u32;
#ifndef _WIN64
	// ELEMENT_TYPE_U
	UINT32 u;
	// ELEMENT_TYPE_I
	INT32 i;
#else
	// ELEMENT_TYPE_U
	UINT64 u;
	// ELEMENT_TYPE_I
	INT64 i;
#endif
	// ELEMENT_TYPE_I8
	INT64 i64;
	// ELEMENT_TYPE_U8
	UINT64 u64;
	// ELEMENT_TYPE_R4
	float ft;
	// ELEMENT_TYPE_R8
	double db;
};

//
// Structure containing a field value
//
class SVAL
{
private:
	void DoublePrint(double i)
	{
		if(IsReal() && (i-(INT64)i!=0))
		{
			swprintf(NameBuffer, MAX_MTNAME, L"%f", i);
		} else
		{
			swprintf(NameBuffer, MAX_MTNAME, L"0n%I64i", (INT64)i);
			Value.i64 = (INT64)i;
		}

		prettyPrint.assign(NameBuffer);

		//strValue.assign(NameBuffer);
		DoubleValue = i;
	}

	void PointerPrint(DWORD_PTR i)
	{
		swprintf(NameBuffer, MAX_MTNAME, L"%p", i);
		prettyPrint.assign(NameBuffer);
		DoubleValue = (INT64)i;
	}

public:
	SVAL()
	{
		ZeroMemory(&Value, sizeof(Value));
		IsValid = true;
		fieldName = "calculated";
	}
	SVAL& operator=(const double rs)
	{
		Value.db = rs;
		corBaseType = corType = ELEMENT_TYPE_R8;
		DoublePrint(rs);
		Size = sizeof(rs);
		return *this;
	}
	SVAL& operator=(const float rs)
	{
		Value.ft = rs;
		corBaseType = corType = ELEMENT_TYPE_R4;
		DoublePrint(rs);
		Size = sizeof(rs);
		return *this;
	}
	SVAL& operator=(const INT32 rs)
	{
		Value.i32 = rs;
		corBaseType = corType = ELEMENT_TYPE_I4;
		DoublePrint(rs);
		Size = sizeof(rs);
		return *this;
	}
	SVAL& operator=(const INT64 rs)
	{
		Value.i64 = rs;
		corBaseType = corType = ELEMENT_TYPE_I8;
		DoublePrint(rs);
		Size = sizeof(rs);
		return *this;
	}
	SVAL& operator=(const UINT32 rs)
	{
		Value.u32 = rs;
		corBaseType = corType = ELEMENT_TYPE_U4;
		Size = sizeof(rs);
		DoublePrint(rs);

		return *this;
	}
	SVAL& operator=(const UINT64 rs)
	{
		Value.i64 = rs;
		corBaseType = corType = ELEMENT_TYPE_U8;
		DoublePrint(rs);
		Size = sizeof(rs);
		return *this;
	}
	SVAL& operator=(const bool rs)
	{
		Value.b = rs;
		corBaseType = corType = ELEMENT_TYPE_BOOLEAN;
		Size = sizeof(rs);
		DoublePrint(rs);

		return *this;
	}

	SVAL& operator=(const WCHAR rs)
	{
		Value.ch = rs;
		corBaseType = corType = ELEMENT_TYPE_CHAR;
		Size = sizeof(rs);
		DoublePrint(rs);

		return *this;
	}

	SVAL& operator=(const char rs)
	{
		Value.bt = rs;
		corBaseType = corType = ELEMENT_TYPE_I1;
		Size = sizeof(rs);
		DoublePrint(rs);

		return *this;
	}

	SVAL& operator=(const BYTE rs)
	{
		Value.ubt = rs;
		corBaseType = corType = ELEMENT_TYPE_U1;
		Size = sizeof(rs);
		DoublePrint(rs);

		return *this;
	}
	SVAL& operator=(const SHORT rs)
	{
		Value.sh = rs;
		corBaseType = corType = ELEMENT_TYPE_I2;
		DoublePrint(rs);

		Size = sizeof(rs);
		return *this;
	}
	SVAL& operator=(const USHORT rs)
	{
		Value.ush = rs;
		corBaseType = corType = ELEMENT_TYPE_U2;
		Size = sizeof(rs);
		DoublePrint(rs);

		return *this;
	}

	SVAL& operator=(const std::wstring& rs)
	{
		strValue = rs;
		DoubleValue=0;
		prettyPrint = rs;
		corType = ELEMENT_TYPE_STRING;
		corBaseType = ELEMENT_TYPE_STRING;

		Size = sizeof(rs.length());
		return *this;
	}

	SVAL& SetPtr(DWORD_PTR rs)
	{
		Value.u64 = (UINT64)rs;
		corBaseType = corType = ELEMENT_TYPE_PTR;
		Size = sizeof(rs);
		PointerPrint(rs);

		return *this;
	}
	friend SVAL operator+(const SVAL& x, const SVAL& y);
	friend SVAL operator-(const SVAL& x, const SVAL& y);
	friend SVAL operator*(const SVAL& x, const SVAL& y);
	friend SVAL operator/(const SVAL& x, const SVAL& y);
	friend SVAL operator%(const SVAL& x, const SVAL& y);
	friend SVAL operator==(const SVAL& x, const SVAL& y);
	friend SVAL operator!=(const SVAL& x, const SVAL& y);
	friend SVAL operator>(const SVAL& x, const SVAL& y);
	friend SVAL operator<(const SVAL& x, const SVAL& y);
	friend SVAL operator>=(const SVAL& x, const SVAL& y);
	friend SVAL operator<=(const SVAL& x, const SVAL& y);
	friend SVAL operator&(const SVAL& x, const SVAL& y);
	friend SVAL operator|(const SVAL& x, const SVAL& y);
	friend SVAL operator&&(const SVAL& x, const SVAL& y);
	friend SVAL operator||(const SVAL& x, const SVAL& y);
	friend SVAL operator!(const SVAL& x);
	friend SVAL operator-(const SVAL& x);
    friend std::ostream& operator<< (std::ostream &os, SVAL& val);

	void PrettyPrint()
	{
	}
	template<class T>
	T* GetVal()  //const T& operator&()
	{
		return (T*)&Value;
	}
	CorElementType corType;
	CorElementType corBaseType;
	CLRValue Value;
	std::wstring strValue;
	mdToken Token;
	std::wstring prettyPrint;
	std::wstring typeName;
	CLRDATA_ADDRESS Module;

	bool IsValid;
	bool IsStatic;
	bool IsValueType;
	bool IsUnsigned()
	{
		return ((corBaseType % 2 == 0) && (corBaseType >=  ELEMENT_TYPE_I1) && (corBaseType <=  ELEMENT_TYPE_U8))
			|| (corBaseType == ELEMENT_TYPE_R4) || (corBaseType == ELEMENT_TYPE_R8);
	}
	bool IsInt()
	{
		return (
			        ( (corBaseType >=  ELEMENT_TYPE_I1) && (corBaseType <=  ELEMENT_TYPE_U8) ) 
					|| 
					( (corBaseType >= ELEMENT_TYPE_PTR) && (corBaseType < ELEMENT_TYPE_MAX) )
					)
					;
	}
	bool IsReal()
	{
		return (corBaseType == ELEMENT_TYPE_R4) || (corBaseType == ELEMENT_TYPE_R8);
	}
	bool IsString()
	{
		return (corType == ELEMENT_TYPE_STRING);
	}

	void MakeInvalid()
	{
		IsValid = false;
		corType = corBaseType = ELEMENT_TYPE_END;
		prettyPrint = L"#INVALID#";
	}

	static void NormalizeLeft(SVAL &ls, SVAL &rs)
	{
		if(ls.IsReal())
		{
			if(rs.IsReal() || rs.IsString())
			{
				return;
			}
			if(rs.IsInt())
			{
				// Not realy a float value
				if((ls.DoubleValue - (INT64)ls.DoubleValue) == 0)
				{
					ls = (INT64)ls.DoubleValue;
					return;
				}
				if(rs.IsUnsigned())
				{
					// a big number cannot be "doubled"
					if(rs.corBaseType == ELEMENT_TYPE_U8 && (rs.Value.i64 < 0))
					{
						ls = (INT64)rs.DoubleValue;
					}
				}
			}
		}
	}

	static void Normalize(SVAL &ls, SVAL &rs)
	{
		NormalizeLeft(ls, rs);
		NormalizeLeft(rs, ls);
	}

	static CorElementType GetCast(CorElementType ls, CorElementType rs)
	{
		switch(ls)
		{
		case ELEMENT_TYPE_STRING:
			return rs == (ELEMENT_TYPE_STRING) ? ELEMENT_TYPE_STRING : ELEMENT_TYPE_END;
		case ELEMENT_TYPE_I1:
		case ELEMENT_TYPE_I2:
		case ELEMENT_TYPE_I4:
		case ELEMENT_TYPE_I8:
		case ELEMENT_TYPE_U1:
		case ELEMENT_TYPE_U2:
		case ELEMENT_TYPE_U4:
		case ELEMENT_TYPE_U8:
		case ELEMENT_TYPE_R4:
		case ELEMENT_TYPE_R8:
		case ELEMENT_TYPE_BOOLEAN:
		case ELEMENT_TYPE_PTR:
			if((rs >= ELEMENT_TYPE_I1 && rs <= ELEMENT_TYPE_R8) || rs==ELEMENT_TYPE_BOOLEAN || rs == ELEMENT_TYPE_PTR)
			{
				return max(ls, rs);
			}
			return ELEMENT_TYPE_END;
		default:
			return ELEMENT_TYPE_END;
		}
	}

	double DoubleValue;
	DWORD_PTR ObjAddress;
	UINT Offset;
	DWORD_PTR DataPtr;
	CLRDATA_ADDRESS MT;
	UINT Size;
	std::string fieldName;
};

typedef std::map<std::string, SVAL> varMap;
typedef std::map<std::string, std::vector<SVAL>> namedKey;

void DumpFields(CLRDATA_ADDRESS Address, std::vector<std::string> Fields, CLRDATA_ADDRESS MethodTable=NULL, varMap* Vars=NULL);
SVAL GetValue(CLRDATA_ADDRESS offset, CorElementType CorType, CLRDATA_ADDRESS MethodTable=NULL, const char* ObjectString=NULL, const char* ValueTypeString=NULL, bool Print=false);

void DumpNamedKeys(CLRDATA_ADDRESS Address, std::string Key = "", namedKey *Keys = NULL);

class SocketData
{

public:
	int state;
	bool ownsHandle;
	bool willBlock;
	HANDLE Handle;
	bool isFullyInitialized;
	bool isReleased;
	bool isListening;
	bool isConnected;
	bool isDisconnected;
	int closeTimeout;
	int cleanedUp;
	CLRDATA_ADDRESS rightEndpoint;
	CLRDATA_ADDRESS remoteEndpoint;
	CLRDATA_ADDRESS innerSocket;
	SocketData():
		state(0),
		ownsHandle(0),
		Handle(0),
		isFullyInitialized(false),
		isReleased(false),
		isListening(false),
		isConnected(false),
		isDisconnected(false),
		willBlock(false),
		closeTimeout(0),
		cleanedUp(0),
		rightEndpoint(0),
		remoteEndpoint(0),
		innerSocket(0)
	{};



	bool OwnsHandle()
	{
		return ownsHandle;
	}



	bool IsListening()
	{
		return isListening;
	}

	bool IsConnected()
	{
		return isConnected;
	}

	bool IsReleased()
	{
		return isReleased;
	}
	bool IsFullyInitialized()
	{
		return isFullyInitialized;
	}
	bool IsClosed()
	{
		return (state & 1) == 1;
	};
	bool IsDisposed()
	{
		return (cleanedUp != 0) || ((state & 2) == 2);
	};

	int RefCount()
	{
		return state >> 2;
	}
};

class SocketDetail
{
public:
	bool isServer;
	UINT total;
	UINT connected;
	UINT disconnected;
	UINT disposed;
	std::vector<SocketData> sockets;
	SocketDetail():
		isServer(false),
		total(0),
		connected(0),
		disconnected(0),
		disposed(0)
	{};

};

class SpecialCases
{
public:

	SpecialCases(void);
	~SpecialCases(void);
	// Dump Array
	static void DumpArray(CLRDATA_ADDRESS Address, CLRDATA_ADDRESS MethodTable=0, int Start=0, int End=-1, bool ShowIndex=true, ObjDetail *Obj=NULL, bool shownull=false);
	static void EnumArray(CLRDATA_ADDRESS Address, CLRDATA_ADDRESS MethodTable=0, ObjDetail *Obj=NULL, std::vector<CLRDATA_ADDRESS>* Addresses=NULL);
	static std::wstring GetEnumString(const MD_FieldData& Field, UINT64 Value);
	static std::wstring GetEnumString(CLRDATA_ADDRESS MethodTable, UINT64 Value, mdTypeDef Token=0);
	static bool IsEnumType(CLRDATA_ADDRESS MT);
	static std::string GetRawArray(CLRDATA_ADDRESS Obj);
	static std::string GetHexArray(CLRDATA_ADDRESS Obj, bool Padded=true);
	static SVAL GetDbgVar(int DBGVar);
	static bool SetDbgVar(int DBGVar, SVAL v);
	static std::string ToGuid(CLRDATA_ADDRESS Addr);
	static INT64 TicksFromTarget();
	static INT64 DateToTicks(INT64 year, INT64 month, INT64 day);
	static INT64 TimeToTicks(INT64 hour, INT64 minute, INT64 second);
	static std::string PrettyPrint(CLRDATA_ADDRESS Address, CLRDATA_ADDRESS MethodTable=0);
	static void StringXml( TiXmlNode * Parent, std::string& Result, unsigned int indent = 0 );
	static std::string IndentedXml(std::wstring XmlDoc);
	static std::string XmlTree(std::wstring XmlDoc);
	static void DumpHash(CLRDATA_ADDRESS Address, std::string NameMatch, std::string ValueMatch, std::vector<varMap>* KeyPair=NULL);
	static bool GetSocketData(CLRDATA_ADDRESS Address, SocketData* Socket);
	static std::wstring IPV4Address(INT64 Address, int Port);
	static std::wstring IPV6Address(WORD* SocketAddress, int Port, int ScopeId=0);
	static std::wstring IPAddress(CLRDATA_ADDRESS IPAddress);
	static std::wstring HtmlEncode(std::wstring HtmlString, bool EncodeSpaces=false);


};

inline void PrintCompact(varMap Vars, bool NewLine = false)
{
	varMap::const_iterator it;
	g_ExtInstancePtr->Out("{ ");

	for(it=Vars.begin(); it!=Vars.end(); it++)
	{
		if(NewLine) g_ExtInstancePtr->Out("\n ");
		g_ExtInstancePtr->Out(" { \"%s\":", it->second.fieldName.c_str());
		
		if(it->second.typeName == L"System.String")
			g_ExtInstancePtr->Out("\"");
		if(it->second.strValue.size() > 0)
			g_ExtInstancePtr->Out("%S", it->second.strValue.c_str());
		else
			g_ExtInstancePtr->Out("%S } ", it->second.prettyPrint.c_str());
		if(it->second.typeName == L"System.String")
			g_ExtInstancePtr->Out("\"");
		g_ExtInstancePtr->Out(" }");
	}
	g_ExtInstancePtr->Out(" }");
}

inline void AdHocExecute(const std::string &Command, std::string &Result)
{
	ExtCaptureOutputA* execContext = new ExtCaptureOutputA();

	execContext->Execute(Command.c_str());

	Result.append(execContext->m_Text);
	if(execContext) execContext->Delete();
	delete execContext;
}

inline void PrintArrayIndex(CLRDATA_ADDRESS Address, int Start, int Rank)
{
	if(Rank==1)
	{
		g_ExtInstancePtr->Out("[%u]: ", Start);
		return;
	}
	DWORD bound=1;
	vector<DWORD> idx;
	for(int i=Rank-1;i>=0;i--)
	{
		//g_ExtInstancePtr->Out("%p\n", Address+sizeof(void*)*3+i*sizeof(DWORD));
		ExtRemoteData ptr(Address+sizeof(void*)*3+i*sizeof(DWORD), sizeof(DWORD));
		DWORD f = ptr.GetUlong();

		idx.push_back((Start % (bound* (f == 0 ? 1 : f))) / bound);
		bound *= f;
		if(bound==0) bound = 1; // to avoid divided by zero in corrupted memory
	}
	for(int i=idx.size()-1;i>=0;i--)
	{
		if(Rank>1)
			g_ExtInstancePtr->Out("[%u]", (DWORD)idx[i]);
		else
			g_ExtInstancePtr->Out("[%u]", Start);

	}

	g_ExtInstancePtr->Out(": ");
}

inline DWORD GetBound(CLRDATA_ADDRESS Address, int Rank, int Dimension)
{
		if(Dimension>=Rank) return 0;
		ExtRemoteData ptr(Address+sizeof(void*)*3+Dimension*sizeof(DWORD), sizeof(DWORD));
		DWORD f = ptr.GetUlong();
		return f;
}

inline void PrintArrayBounds(CLRDATA_ADDRESS Address, int Rank)
{
	if(Rank <= 1)
	{
		return;
	}
	g_ExtInstancePtr->Out("( ");

	for(int i=0;i<Rank;i++)
	{
		ExtRemoteData ptr(Address+sizeof(void*)*3+i*sizeof(DWORD), sizeof(DWORD));
		DWORD f = ptr.GetUlong();
		g_ExtInstancePtr->Out("%u ", f);
	}

	g_ExtInstancePtr->Out(")");
}

extern SVAL WEval(std::string Expression);
