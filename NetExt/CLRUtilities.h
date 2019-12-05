#pragma once
#include <comutil.h>
#include <atlbase.h>
#include <atlsafe.h>
#include <metahost.h>
#include <mscoree.h>
#include <string>
#pragma comment(lib, "mscoree.lib")


#import <mscorlib.tlb> raw_interfaces_only  high_property_prefixes("_get", "_put", "_putref")  auto_rename rename("Thread", "Thread_CLR") rename("StackTrace", "StackTrace_CLR") //rename("ReportEvent", "InteropServices_ReportEvent") 

//#include "mscorlib.tlh"



#ifndef LEAVEONERROR
#define LEAVEONERROR(code) \
	if (FAILED(code)) \
	{ \
		return hr; \
	};


#define FALSEONERROR(code) \
	if (FAILED(code)) \
	{ \
		return false; \
	};
#endif // !LEAVEONERROR

using namespace mscorlib;
using namespace std;

typedef void(*pOutput)(wchar_t* Text);

template <typename T>
inline long long GetPointer(T Pointer)
{
	return (long long)Pointer;
}

class CLRUtilities
{
private:
	CComPtr<ICLRMetaHost> metaHost;
	CComPtr<ICLRRuntimeInfo> runtimeInfo;
	CComPtr<ICorRuntimeHost> runtimeHost;
	CComPtr<IAppDomainSetup> appDomainSetup;
	CComPtr<IUnknown> unkAppDomainSetup;
	CComPtr<IUnknown> unkAppDomain;
	CComPtr<_Assembly> assembly;
	CComPtr<_Type> _type; // Not standard naming because type is reserved
	CComPtr<_AppDomain> appDomain;
	wstring dllPath;
	wstring appDomainName;
	bool isStarted;
	bool isAppDomainCreated;

public:

	CLRUtilities(wstring DllPath, wstring AppDomainName = L"NetExt") :
		metaHost(nullptr),
		runtimeInfo(nullptr),
		runtimeHost(nullptr),
		appDomainSetup(nullptr),
		unkAppDomainSetup(nullptr),
		unkAppDomain(nullptr),
		assembly(nullptr),
		_type(nullptr),
		appDomain(nullptr),
		dllPath(DllPath),
		appDomainName(AppDomainName),
		isStarted(false),
		isAppDomainCreated(false)
	{
	}

	~CLRUtilities()
	{
		Dispose();

	}

	void Dispose();

	static wstring GetFilePath();

	HRESULT StartRuntime();

	HRESULT FindAppDomain();

	HRESULT CreateAppDomain();

	HRESULT LoadAssembly();

	HRESULT GetType(const wstring& TypeName);

	HRESULT ExecuteStaticMethod(const wstring& TypeName, const wstring& Method, SAFEARRAY* Parameters, VARIANT& Result);

	HRESULT ExecuteStaticMethod(const wstring& TypeName, const wstring& Method, variant_t Parameter, VARIANT& Result);

	HRESULT UnloadDomain();

	HRESULT UnloadDomainByName(wstring DomainName = L"NetExt");
};