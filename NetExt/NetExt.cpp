/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "ClrHelper.h"
#include "VersionInfo.h"
#include "CLRHelper.h"
#include "CLRUtilities.h"
//
// From Microsoft.Diagnostics.Runtime

/*

	[Guid("5c552ab6-fc09-4cb3-8e36-22fa03c798b7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IXCLRDataProcess
*/
//


Thread EXT_CLASS::SessionThreads;

bool isCLRInit = false;

bool NET2 = false;

bool coreCLR = false;

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
        const type name = {l,w1,w2,{b1,b2,b3,b4,b5,b6,b7,b8}}
MIDL_DEFINE_GUID(IID, IID_ClrMDExt,0x5c552ab6,0xfc09,0x4cb3,0x8e,0x36,0x22,0xfa,0x03,0xc7,0x98,0xb7);

WCHAR NameBuffer[MAX_CLASS_NAME];

IUnknown* clrData = NULL;
WDBGEXTS_CLR_DATA_INTERFACE clrInterface;
std::map<std::wstring, std::wstring> varsDict;
 
#if !_DEBUG
extern "C" int _imp___vsnprintf(
char *buffer,
size_t count,
const char *format,
va_list argptr
)
 {
return vsnprintf( buffer, count, format, argptr );
 }
#endif

long IsManagedInterrupt()
{
	return IsInterrupted() ? 1 : 0;
}
// EXT_DECLARE_GLOBALS must be used to instantiate
// the framework's assumed globals.
EXT_DECLARE_GLOBALS();

HINSTANCE hDll = 0;
pSetOutput setOutputCallBack;
pOutput echo;
pIMDTarget createFromDebug;
pSetInterrupt managedInterrupted;

CComPtr<NetExtShim::IMDActivator> pAct;
CComPtr<NetExtShim::IMDTarget> pTarget;
CComPtr<NetExtShim::IMDRuntime> pRuntime;
CComPtr<NetExtShim::IMDRuntimeInfo> pRuntimeInfo;
CComPtr<NetExtShim::IMDHeap> pHeap;


std::wstring ToWString(const CComBSTR& b)
{
	// b can be NULL; Length() will be 0 in that case
	return std::wstring(static_cast<BSTR>(b), b.Length());
}

std::string localCW2A(LPCWSTR lpwstr)
{
	ATL::CW2A tmp(lpwstr);
	return std::string(tmp);
}

std::wstring localCA2W(LPSTR lpstr)
{
	ATL::CA2W tmp(lpstr);
	return std::wstring(tmp);
}

	std::string tickstotimespan(UINT64 Ticks)
	{
		// Number of 100ns ticks per time unit
		ULONG64 TicksPerMillisecond = 10000;
		ULONG64 TicksPerSecond = TicksPerMillisecond * 1000;
		ULONG64 TicksPerMinute = TicksPerSecond * 60;
		ULONG64 TicksPerHour = TicksPerMinute * 60;
		ULONG64 TicksPerDay = TicksPerHour * 24;

		ULONG64 TicksMask = 0x3FFFFFFFFFFFFFFF;

		ULONG64 ticks = Ticks & TicksMask;
		if(ticks < TicksPerSecond || Ticks > (UINT64)INT64_MAX)
			return "00:00:00";
		string days;
		if(ticks > TicksPerDay)
		{
			char daysbuffer[80];
			if(ticks / TicksPerDay > 0)
			{
				sprintf_s(daysbuffer, 80, "%u days ", ticks / TicksPerDay);
				days.assign(daysbuffer);
			}
		}

		time_t rawtime = (time_t)(ticks/10000000);
		char buff[80];
		struct tm* ti;
		ti = gmtime(&rawtime); // this is not memory leak. The memory is allocated statically and overwritten every time
		if(ti)
		{
			strftime(buff,80,"%H:%M:%S", ti);
			days.append(buff);
			return days;
		} else
		{
			return days+"#invalid#";
		}
	}

	std::string tickstoCTime(UINT64 Ticks)
	{


		time_t rawtime = (time_t)Ticks;
		char buff[80];
		struct tm* ti;
		ti = gmtime(&rawtime); // this is not memory leak. The memory is allocated statically and overwritten every time
		if(ti)
		{
			strftime(buff,80,"%m/%d/%Y %I:%M:%S %p", ti);
			return buff;
		} else
		{
			return "#invalid#";
		}
	}

	std::wstring tickstodatetime(UINT64 Ticks)
	{

		// Number of days in a non-leap year
		int DaysPerYear = 365;
		// Number of days in 4 years
		int DaysPer4Years = DaysPerYear * 4 + 1;
		// Number of days in 100 years
		int DaysPer100Years = DaysPer4Years * 25 - 1;
		// Number of days in 400 years
		int DaysPer400Years = DaysPer100Years * 4 + 1;

		// Number of days from 1/1/0001 to 12/31/1600
		int DaysTo1601 = DaysPer400Years * 4;

		// Number of 100ns ticks per time unit
		ULONG64 TicksPerMillisecond = 10000;
		ULONG64 TicksPerSecond = TicksPerMillisecond * 1000;
		ULONG64 TicksPerMinute = TicksPerSecond * 60;
		ULONG64 TicksPerHour = TicksPerMinute * 60;
		ULONG64 TicksPerDay = TicksPerHour * 24;
		ULONG64 FileTimeOffset = DaysTo1601 * TicksPerDay;

		//FileTimeOffset.QuadPart = 0x701CE1722770000;
		ULONG64 ticks = 0;
		ULONG64 TicksMask = 0x3FFFFFFFFFFFFFFF;

		ticks = Ticks & TicksMask;

		//ULONG64 kind = 0;
		//ULONG64 FlagsMask = 0xC000000000000000;
		//kind = value & FlagsMask;

		// GetSystemFileTime() = Ticks - FileTimeOffset
		SYSTEMTIME st;
		FILETIME ft;
		LARGE_INTEGER t;

		// compensate for 1/1/1600 for time offset as .NET starts as 1/1/0001 0:0:0
		t.QuadPart = ticks - FileTimeOffset;

		ft.dwHighDateTime = t.HighPart;
		ft.dwLowDateTime = t.LowPart;

		wchar_t szLocalDate[255], szLocalTime[255];

		//FileTimeToLocalFileTime( &ft, &ft );
		if(FileTimeToSystemTime( &ft, &st ))
		{
			//st.wYear-= 1600; // starting year in .NET is 1/1/1600
			GetDateFormatW( LOCALE_USER_DEFAULT, DATE_SHORTDATE, &st, NULL,
							szLocalDate, 255 );
			GetTimeFormatW( LOCALE_USER_DEFAULT, 0, &st, NULL, szLocalTime, 255 );
			//wsprintf( L"%s %s\n", szLocalDate, szLocalTime );
			std::wstring dt=szLocalDate;
			dt.append(L" ");
			dt.append(szLocalTime);
			return dt;
		} else
		{
			if(ticks < TicksPerMillisecond)
			{
				return L"1/1/0000 00:00:00";
			}

			return L"#INVALIDDATE#";
		}
	}

	std::wstring formatnumber(UINT64 Number)
	{
		NUMBERFMTW nf;
		nf.NumDigits = 0;
		nf.Grouping = 3;
		nf.lpThousandSep = (wchar_t*)L",";
		nf.lpDecimalSep = (wchar_t*)L".";
		nf.NegativeOrder = false;
		nf.LeadingZero = false;
		wchar_t str[101], str1[101];
		str1[0]='\0';
		swprintf_s(str, 100, L"%I64u", Number);

		if(GetNumberFormatW(NULL, NULL, str, &nf, str1, 100) == 0)
			return L"#INVALID";
		return str1;
	};
	std::wstring formatnumber(double Number)
	{
		NUMBERFMTW nf;
		nf.NumDigits = 2;
		nf.Grouping = 3;
		nf.lpThousandSep = (wchar_t*)L",";
		nf.lpDecimalSep = (wchar_t*)L".";
		nf.NegativeOrder = false;
		nf.LeadingZero = false;
		wchar_t str[101], str1[101];
		swprintf_s(str, 100, L"%f", Number);
		str1[0]='\0';
		if(GetNumberFormatW(NULL, NULL, str, &nf, str1, 100) == 0)
			return L"#INVALID";
		return str1;
	};

	std::string formathex(UINT64 Number)
	{
		char str[30];
		sprintf_s(str, 30, "%I64x", Number);
		return str;
	}

class CVersionInfo
{
public:
	CVersionInfo()
	{}

	~CVersionInfo()
	{}

	static string GetFileName()
	{
			char fileName[MAX_PATH];
			if(::GetModuleFileNameA((HINSTANCE)&__ImageBase, fileName, MAX_PATH))
			{
				return fileName;
			} else
			{
				return "";
			}

	}

	static wstring GetFilePath()
	{
			WCHAR fileName[MAX_PATH];
			if(::GetModuleFileNameW((HINSTANCE)&__ImageBase, fileName, MAX_PATH))
			{
				wstring path(fileName);
				size_t found = path.find_last_of(L"\\");
				return path.substr(0, found+1);
				return path;
			} else
			{
				return L"";
			}

	}

	static wstring GetExePath()
	{
			WCHAR fileName[MAX_PATH];
			if(::GetModuleFileNameW(NULL, fileName, MAX_PATH))
			{
				wstring path(fileName);
				size_t found = path.find_last_of(L"\\");
				return path.substr(0, found+1);
				return path;
			} else
			{
				return L"";
			}

	}

	static VS_FIXEDFILEINFO GetVersionInfo()
	{
		VS_FIXEDFILEINFO fi = {0};
		try
		{
			DWORD hwd = 0;
			string fileName = CVersionInfo::GetFileName();
			DWORD verInfoSize = ::GetFileVersionInfoSizeA(fileName.c_str(), &hwd);

			if(verInfoSize)
			{
				void* buffer = malloc(verInfoSize);
				if(buffer)
				{
					VS_FIXEDFILEINFO *info;
					if(!::GetFileVersionInfoA(fileName.c_str(), hwd, verInfoSize, buffer))
					{
						free(buffer);

					}
					UINT versionLen = 0;

					if(::VerQueryValueA(buffer, "\\",(void**)&info,(UINT*)&versionLen))
					{
						fi.dwProductVersionMS = info->dwProductVersionMS;
						fi.dwProductVersionLS = info->dwProductVersionLS;
						
					}
					
					free(buffer);
				}

			}

		} catch(...)
		{
			return fi;
		}
		return fi;


	}

	static string GetVersionString()
	{
		VS_FIXEDFILEINFO fi = GetVersionInfo();
		try
		{
					char versionStr[30] = {0};


						sprintf_s(versionStr, 30, "%u.%u.%u.%u",
							HIWORD (fi.dwProductVersionMS),
							LOWORD (fi.dwProductVersionMS),
							HIWORD (fi.dwProductVersionLS),
							LOWORD (fi.dwProductVersionLS)
							);
						
					return versionStr;

		} catch (...)
		{
			return "<error reading version>";
		}
		return "<version not found>";
	}
};

HRESULT INIT_API()
{
	wasInterrupted = false;				// reset any previous interrupted-by-the-user (resolved in 2.0.1.5550)
										//   before this fix everything after a Ctrl+Break would not work
	EXT_CLASS::SessionThreads.Clear();
	long isFlushed = 0;
	if(isCLRInit && pRuntime != NULL)
	{
		pRuntime->IsFlush(&isFlushed);
		if(isFlushed != 0)
		{
			if(clrData == NULL)
			{
				g_ExtInstancePtr->Out("ERROR: Unable to create Runtime after flush\r");
				isCLRInit = false;
			}
			pRuntime = NULL;
			pHeap = NULL; // release previous
			HRESULT	hr=pTarget->CreateRuntimeFromIXCLR(clrData, &pRuntime);
			
			EXITPOINTEXT("Unable to create runtime\nTry running .cordll -l");
			hr = pRuntime->GetHeap(&pHeap);
			EXITPOINTEXT("Unable to create heap\nTry running .cordll -l");
		}
	}
	if(isCLRInit)
	{
		return S_OK;
	}
	else
	{
		string clr = EXT_CLASS::Execute("lmv mclr");
		NET2 = clr.find("Microsoft") == string::npos; 
		clr = EXT_CLASS::Execute("lmv mcoreclr");
		if(clr.find("Microsoft") != string::npos)
		{
			coreCLR = true;
			NET2 = false;
		}
		HRESULT hr = S_OK;
		
		if(pTarget != NULL) hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
		EXITPOINTEXT("Init was performed but it could not start CLR\nTry running .cordll -l");

		if(!clrData)
		{
			clrInterface.Iid=&IID_ClrMDExt;
			Ioctl(IG_GET_CLR_DATA_INTERFACE,&clrInterface,sizeof(clrInterface));
			clrData=(IUnknown*)clrInterface.Iface; // We do not own the pointer
		}

		if(!clrData) hr=E_APPLICATION_ACTIVATION_EXEC_FAILURE;
		EXITPOINTEXT("Unable to acquire .NET debugger interface\nTry running .cordll -l");

		using namespace NetExtShim;
		::CoInitialize(NULL);

		CallCSDll::GetInterface();
		if(pTarget)
		{
			hr=pTarget->CreateRuntimeFromIXCLR(clrData, &pRuntime);
			
			EXITPOINTEXT("Unable to create runtime\nTry running .cordll -l");
			if(pRuntime == NULL) hr=E_APPLICATION_ACTIVATION_EXEC_FAILURE;
			EXITPOINTEXT("Runtime returned NULL");
			isCLRInit = true;
			// Heap may be null as some commands may not use heap
			pRuntime->GetHeap(&pHeap);
			return S_OK;
		} else
		{
			hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE;
			EXITPOINTEXT("Unable to start NetExtShim.Dll. Make sure all the files are in the WinDbg root folder");
		}
	}
	return E_FAIL;
}

void CallCSDll::GetInterface()
{
	LoadDll();
}


void CallCSDll::LoadDll()
{
	static bool hasInited = false;
	VARIANT result;
	CComPtr<NetExtShim::IMDActivator> actObj;
	if(!hasInited)
	{
		CLRUtilities reflection(L"NetExtShim");

		HRESULT hr = reflection.ExecuteStaticMethod(L"NetExt.Shim.Exports", L"GetMDActivator", NULL /* nullArray */, result);



		if(hr == S_OK)
		{
			IUnknown* iu = (IUnknown*)(result.ppunkVal);

			hr = iu->QueryInterface<NetExtShim::IMDActivator>(&actObj);
		}

		if(hr == S_OK)
		{
			hr = actObj->CreateFromIDebugClientAndPath(const_cast<wchar_t*>(CVersionInfo::GetFilePath().c_str()), g_ExtInstancePtr->m_Client, &pTarget);
		}

		if(hr != S_OK)
		{
			g_ExtInstancePtr->Out("ERROR: Unable to get the MDActivator (0x%x)\r", hr);
			return; // it could not initialize, return
		}

		ULONG_PTR ptr;
		actObj->SetOutputCallBack((LONG_PTR)&CallOutput, (LONG_PTR)&CallOutputDml);
		actObj->SetStopCallBack((LONG_PTR)&IsManagedInterrupt);

		hasInited = true;



	}
}


/*
void CallCSDll::LoadDll()
{
	static bool hasInited = false;
	if(!hasInited)
	{


		if(!hasInited) 
		{
			wstring library = CVersionInfo::GetFilePath()+L"NetExtShim.dll";
			
#if _DEBUG
			wstring wdbgFolder = CVersionInfo::GetExePath();

			g_ExtInstancePtr->Out("Library: %S\n", library.c_str());

			g_ExtInstancePtr->Out("WinDBG path: %S\n", wdbgFolder.c_str());
#endif
			::SetDllDirectory(CVersionInfo::GetFilePath().c_str());

			if(!hDll) 
				hDll = ::LoadLibraryEx(library.c_str(), NULL, LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS
				| LOAD_LIBRARY_SEARCH_USER_DIRS);
			if(hDll)
			{
				hasInited = true;
				if(setOutputCallBack = (pSetOutput)GetProcAddress(hDll, "SetOutputCallBack"))
				{
					setOutputCallBack(&CallOutput, &CallOutputDml);
				}
				setOutputCallBack = NULL;
				echo = NULL;
				createFromDebug = NULL;
				echo = (pOutput)GetProcAddress(hDll, "Echo");
				managedInterrupted = (pSetInterrupt)GetProcAddress(hDll, "SetStopCallBack");
				createFromDebug = (pIMDTarget)GetProcAddress(hDll, "CreateFromIDebugClient");
				managedInterrupted(&IsManagedInterrupt);

			}
		} else
		{
			g_ExtInstancePtr->Out("Unable to load NetExtShim, LoadLibrary returned: 0x%x\n",::GetLastError());
		}

	}
}
*/

//
//  This method is deprecated
//
void CallCSDll::Echo(wchar_t* Message)
{
	LoadDll();
	if(echo)
		echo(Message);
}

inline CLRDATA_ADDRESS GetRegAddr(const char* Reg)
{
	return g_ExtInstancePtr->EvalExprU64(Reg);
}

void CallOutput(wchar_t* Text)
{
	
	g_ExtInstancePtr->Out("%S", Text);
}

void CallOutputDml(wchar_t* Text)
{
	
	g_ExtInstancePtr->Dml("%S", Text);
}





EXT_COMMAND(wupdate,
	"Try to open download page in default browser",
	"{{custom}}")
{
	using namespace NetExtShim;
	//CComPtr<IMDTarget> tmpTarget; 
	::CoInitialize(NULL);
	CallCSDll::LoadDll();
	VS_FIXEDFILEINFO fi = CVersionInfo::GetVersionInfo();
	pTarget->CompareVersion(HIWORD (fi.dwProductVersionMS),
									LOWORD (fi.dwProductVersionMS),
									HIWORD (fi.dwProductVersionLS),
									LOWORD (fi.dwProductVersionLS));
}

EXT_COMMAND(wopendownloadpage,
	"Check latest version and open download page if there is an update",
	"{{custom}}")
{
	using namespace NetExtShim;

	::CoInitialize(NULL);
	CallCSDll::LoadDll();
	pTarget->OpenDownloadPage();	

}

void ShowRuntimeInfo(int Index)
{
#define Out g_ExtInstancePtr->Out
		CComPtr<IMDRuntimeInfo> pInfo;
		HRESULT hr=pTarget->GetRuntimeInfo(Index, &pInfo);
		EXITPOINT("Unable to retrieve runtime info (unexpected");
		CComBSTR filename;
		CComBSTR location;

		hr = pInfo->GetDacRequestFilename(&filename);
		EXITPOINT("Unable to retrieve filename (unexpected)");
		if(filename)
			Out("%i: Filename: %S ",Index,filename);
		hr=pInfo->GetDacLocation(&location);
		EXITPOINT("Unable to retrieve Dac location");
		if(location)
			Out("Location: %S", location);
		Out("\n");
		CComBSTR versionStr;
		hr=pInfo->GetRuntimeVersion(&versionStr);
		EXITPOINT("Unable to retrieve Dac Version");
		if(versionStr)
		{
			Out("+--> %S\n", versionStr);
		}
}

EXT_COMMAND(wver,
	"Load .NET and display its version",
	"{{custom}}")
{
	DO_INIT_API;
	using namespace NetExtShim;
	long count;
	HRESULT hr = pTarget->GetRuntimeCount(&count);
	EXITPOINT("Error: unable to retrieve the runtime list");
	Out("Runtime(s) Found: %u\n", count);
	for(long i=0;i<count;i++)
	{
		ShowRuntimeInfo(i);
		Out("\n");

	}
	int runIndex = pTarget->GetCurrentRuntime();

	Out("NetExt (this extension) Version: %s\n", CVersionInfo::GetVersionString().c_str()); 
	if(count > 1)
	{
		Out("\n*** Selected Runtime '%i' because it contains more threads\n", runIndex);
		Out("*** Tip: To change selected runtime use !wsetruntime <n>\n");

	}
}

inline void Init()
{
    IDebugClient *DebugClient;
    PDEBUG_CONTROL DebugControl;
    HRESULT Hr;

    Hr = S_OK;

    if ((Hr = DebugCreate(__uuidof(IDebugClient),
                          (void **)&DebugClient)) != S_OK)
    {
        return;
    }

    if ((Hr = DebugClient->QueryInterface(__uuidof(IDebugControl),
                                  (void **)&DebugControl)) == S_OK)
    {
        //
        // Get the windbg-style extension APIS
        //
        //ExtensionApis.nSize = sizeof (ExtensionApis);
        //Hr = DebugControl->GetWindbgExtensionApis64(&ExtensionApis);
		NameBuffer[0]=L'\0';
		DWORD size = GetModuleFileNameW((HINSTANCE)&__ImageBase, NameBuffer, MAX_MTNAME);
		if(size > 4)
		{
			NameBuffer[size-4]=L'\0';
		}
		using namespace boost::algorithm;

		std::vector< std::wstring > splitV;
		split( splitV, NameBuffer, is_any_of(L"\\"), token_compress_on );
		std::wstring moduleName = splitV[splitV.size()-1];

		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "<b>%S version %s</b> ", moduleName.c_str(), CVersionInfo::GetVersionString().c_str());
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "<b>" ver_date "</b>");
#if _DEBUG
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, " (*** Debug Build ***)");
#endif
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "\n");
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "License and usage can be seen here: <link cmd=\"!whelp license\">!whelp license</link>\n");
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "Check Latest version: <link cmd=\"!wupdate\">!wupdate</link>\n");
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "For help, type <link cmd=\"!whelp\">!whelp</link> (or in WinDBG run: '<b>.browse !whelp</b>')\n");
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "<b>Questions and Feedback:</b> https://github.com/rodneyviana/netext/issues \n");
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "<b>Copyright (c) 2014-2015 Rodney Viana</b> (http://blogs.msdn.com/b/rodneyviana) \n");
		DebugControl->ControlledOutput(DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML, DEBUG_OUTPUT_NORMAL, "Type: <link cmd=\"!windex -tree\">!windex -tree</link> or <link cmd=\"~*e!wstack\">~*e!wstack</link> to get started\n\n");
        DebugControl->Release();
    }
    DebugClient->Release();
}

void EXT_CLASS::OnSessionAccessible(ULONG64 Argument)
{
	UNREFERENCED_PARAMETER(Argument);
	static bool showStart = true;
	if(showStart)
	{
		Init();
		showStart = false;

	}


}

EXT_COMMAND(wgchandle,
			"Dump GC Handles. Use '!whelp wgchandle' for detailed help",
			"{summary;b,o;;summary;Display only the summary}"
			"{type;s,o;;type;List only handles with the type (eg. -type sharepoint). * not accepted. Partial name only}"
			"{handletype;s,o;;handletype;List only handles of the type (eg. -handletype pinned)}")

{
	DO_INIT_API;
	bool summary = HasArg("summary");
	PCSTR type = NULL;
	if(HasArg("type")) type = GetArgStr("type");
	PCSTR handleType = NULL;
	if(HasArg("handletype")) handleType = GetArgStr("handletype");
	
	pRuntime->DumpHandles(summary ? 1 : 0, handleType, type);
}

EXT_COMMAND(wdomain,
			"Dump Application Domains. Use '!whelp wdomain' for detailed help",
			"{{custom}}")

{
	DO_INIT_API;
	pRuntime->DumpDomains();
}

EXT_COMMAND(wopensource,
			"Open the managed source code based on IP. Use '!whelp wopensource' for detailed help",
			"{;e,r;;Address,IP Address}")

{
	DO_INIT_API;
	CLRDATA_ADDRESS addr = GetUnnamedArgU64(0);
	pTarget->OpenSource(addr);
}

EXT_COMMAND(wthreads,
			"Dump Managed Threads. Use '!whelp wthreads' for detailed help",
			"{{custom}}")

{
	DO_INIT_API;
	pRuntime->DumpThreads();
}

#include "Indexer.h"
EXT_COMMAND(wdae,
			"Dump All Exceptions. Use '!whelp wdae' for detailed help",
			"{{custom}}")

{
	if(!indc)
	{
		Dml("You need to index heap before running !wdae. Run <link cmd=\"!windex;!wdae\">!windex</link>\n");
		return;
	}
	using namespace NetExtShim;

	INIT_API();
	CComPtr<IMDObjectEnum> objEnum;
	pTarget->CreateEnum(&objEnum);
	MatchingAddresses Addresses;
	indc->GetByDerive("System.Exception", Addresses);
	if(Addresses.size()==0)
	{
		Out("No exception in the heap (this is not normal as .NET creates at least 3 exceptions by default\n");
		return;
	}
	AddressEnum enumAdd;
	enumAdd.Start(Addresses);
	CLRDATA_ADDRESS objAddr;
	while(objAddr = enumAdd.GetNext())
	{
		objEnum->AddAddress(objAddr);
	}
	pHeap->DumpAllExceptions(objEnum);
}

//
// Adding fork from Sebastian Solnica to adjust wpe to print current thread exception if there is one and no parameter is passed
// More on Sebastian Solnica here: https://lowleveldesign.wordpress.com/2015/07/09/netext-sos-on-steroids/
//
EXT_COMMAND(wpe,
			"Dump Exception Object. Use '!whelp wpe' for detailed help",
		      "{;e,o;;Address,Exception Address}")  

{   
    INIT_API();   
   
    CLRDATA_ADDRESS addr = 0;   
    
    if (m_NumUnnamedArgs == 0)   
    {   
        // check if the current thread has an exception info attached   
        CComPtr<IMDThreadEnum>  threadEnum;   
    
        HRESULT hr = pRuntime->EnumerateThreads(&threadEnum);   
        EXITPOINT("Unable to enumerate threads");   
    
        ULONG threadId;   
        ULONG sysId;   
        g_ExtInstancePtr->m_System3->GetCurrentThreadSystemId(&sysId);   
        g_ExtInstancePtr->m_System3->GetCurrentThreadId(&threadId);   
        while (hr == S_OK)   
        {   
            CComPtr<IMDThread> thread;   
            hr = threadEnum->Next(&thread);   
    
            long osId;   
    
            if (hr == S_OK && thread->GetOSThreadId(&osId) == S_OK)   
            {   
                if ((ULONG)osId == sysId)   
                {   
                    CComPtr<IMDException> ex;   
                    thread->GetCurrentException(&ex);   
                    if (!ex)   
                    {   
                        Out("No exception in the current thread.");   
                        return;   
                    }   
                    ex->GetObjectAddress(&addr);   
                }   
            }   
        }   
    }   
    else   
    {   
        addr = GetUnnamedArgU64(0);   
    }   
    pHeap->DumpException(addr);   
}  


EXT_COMMAND(wvar,
			"Dump Environment Variables. Use '!whelp wvar' for detailed help",
		      "{;s,o;;variable,Variable Pattern}")  

{ 
	std::string varName;

	if(HasUnnamedArg(0))
	{
		varName.assign(GetUnnamedArgStr(0));
	}

	auto vars = this->GetProcessEnvVar();

	auto curr = vars.begin();
	int count = 0;
	while(curr != vars.end())
	{
		if(varName.size() == 0 || MatchPattern(CW2A(curr->first.c_str()), varName.c_str()))
		{
			Out("%S=", curr->first.c_str());
			Out("%S\n", curr->second.c_str());
			count++;
		}
		curr++;
	}

	if(count == vars.size())
	{
		Out("\n%i item(s) listed\n", count);
	} else
	{
		Out("\n%i item(s) listed. %i skipped by the filter\n", count, vars.size()-count);
	}

}

EXT_COMMAND(wsetruntime,
			"Dump object. Use '!whelp wsetruntime' for detailed help",
			"{;ed,r;;Runtime index}"
			)
{
	int i = GetUnnamedArgU64(0);
    INIT_API();   
	NetExtShim::IMDRuntime *runTime = NULL;

	HRESULT hr = pTarget->SetCurrentRuntime(i, &runTime);
	if(hr == S_OK)
	{
		if(indc)
		{
			delete indc;
			indc=NULL;
		}
		ShowRuntimeInfo(i);
		pRuntime = runTime;
	} else
	{
		Out("Unable to change Runtime\n");
	}

}