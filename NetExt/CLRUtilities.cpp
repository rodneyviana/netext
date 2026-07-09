#include "CLRUtilities.h"

void CLRUtilities::Dispose()
{
	metaHost = nullptr;
	runtimeInfo = nullptr;
	runtimeHost = nullptr;
	appDomainSetup = nullptr;
	unkAppDomainSetup = nullptr;
	unkAppDomain = nullptr;
	assembly = nullptr;
	_type = nullptr;
	appDomain = nullptr;
}

wstring CLRUtilities::GetFilePath()
{
	WCHAR fileName[MAX_PATH];
	if (::GetModuleFileNameW((HINSTANCE)&__ImageBase, fileName, MAX_PATH))
	{
		wstring path(fileName);
		size_t found = path.find_last_of(L"\\");
		return path.substr(0, found + 1);
		return path;
	}
	else
	{
		return L"";
	}

}

HRESULT CLRUtilities::StartRuntime()
{
	if (isStarted)
		return S_OK;

	HRESULT hr = S_OK;

	hr = CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID*)&metaHost);
	LEAVEONERROR(hr);
	hr = metaHost->GetRuntime(L"v4.0.30319", IID_ICLRRuntimeInfo, (LPVOID*)&runtimeInfo);
	LEAVEONERROR(hr);

	BOOL isLoadable = FALSE;
	hr = runtimeInfo->IsLoadable(&isLoadable);
	LEAVEONERROR(hr);

	if (!isLoadable)
	{
		hr = E_APPLICATION_ACTIVATION_EXEC_FAILURE; // If you see this, it is because it cannot be loaded
		return hr;
	}

	hr = runtimeInfo->GetInterface(CLSID_CorRuntimeHost, IID_ICorRuntimeHost, (void**)&runtimeHost);
	LEAVEONERROR(hr);

	hr = runtimeHost->Start();
	LEAVEONERROR(hr);
	isStarted = true;
	return S_OK;
}

HRESULT CLRUtilities::FindAppDomain()
{
	HRESULT hr = StartRuntime();
	LEAVEONERROR(hr);
	HDOMAINENUM appDomainEnum = NULL;

	hr = runtimeHost->EnumDomains(&appDomainEnum);
	LEAVEONERROR(hr);



	CComPtr<IUnknown> unkAppDomain;

	hr = runtimeHost->NextDomain(appDomainEnum, &unkAppDomain);
	LEAVEONERROR(hr);

	while (S_OK == hr)
	{
		CComBSTR domainName;

		appDomain = nullptr;
		unkAppDomain->QueryInterface(__uuidof(_AppDomain), (VOID**)&appDomain);
		hr = appDomain->get_FriendlyName(&domainName);
		if (SUCCEEDED(hr) && 0 == wcscmp(domainName, appDomainName.c_str()))
		{
			runtimeHost->CloseEnum(appDomainEnum);
			isAppDomainCreated = true;
			return S_OK;
		}
		unkAppDomain = nullptr;
		hr = runtimeHost->NextDomain(appDomainEnum, &unkAppDomain);
	}
	appDomain = nullptr;
	runtimeHost->CloseEnum(appDomainEnum);
	return ERROR_FILE_NOT_FOUND;


}

HRESULT CLRUtilities::CreateAppDomain()
{
	if (isAppDomainCreated)
		return S_OK;
	HRESULT hr = FindAppDomain();
	if (S_OK == hr)
		return S_OK; // If App Domain already exists, it is all good
	LEAVEONERROR(hr);
	hr = runtimeHost->CreateDomainSetup(&unkAppDomainSetup);
	LEAVEONERROR(hr);
	hr = unkAppDomainSetup->QueryInterface(__uuidof(appDomainSetup), (void**)&appDomainSetup);
	LEAVEONERROR(hr);

	CComBSTR baseDir;
	auto path = GetFilePath();
	hr = baseDir.Append(path.c_str());
	LEAVEONERROR(hr);
	hr = appDomainSetup->put_ApplicationBase(baseDir);
	LEAVEONERROR(hr);

	hr = runtimeHost->CreateDomainEx(appDomainName.c_str(),
		appDomainSetup,
		nullptr,
		&unkAppDomain);

	LEAVEONERROR(hr);

	hr = unkAppDomain->QueryInterface(__uuidof(appDomain), (LPVOID*)&appDomain);

	isAppDomainCreated = true;
	return hr;

}

HRESULT CLRUtilities::LoadAssembly()
{
	if (nullptr != assembly)
	{
		return S_OK;
	}
	HRESULT hr = CreateAppDomain();
	LEAVEONERROR(hr);

	CComBSTR bstrAssembly;
	hr = bstrAssembly.Append(dllPath.c_str());
	LEAVEONERROR(hr);
	hr = appDomain->Load_2(bstrAssembly, &assembly);

	return hr;

}

HRESULT CLRUtilities::GetType(const wstring& TypeName)
{
	if (_type != NULL)
		return S_OK;
	HRESULT hr = LoadAssembly();
	LEAVEONERROR(hr);

	CComBSTR bstrType;
	hr = bstrType.Append(TypeName.c_str());
	LEAVEONERROR(hr);

	hr = assembly->GetType_2(bstrType, &_type);
	LEAVEONERROR(hr);
	return S_OK;
}

HRESULT CLRUtilities::ExecuteStaticMethod(const wstring& TypeName, const wstring& Method, SAFEARRAY* Parameters, VARIANT& Result)
{
	HRESULT hr = GetType(TypeName);
	LEAVEONERROR(hr);

	CComBSTR bstrMethod;
	hr = bstrMethod.Append(Method.c_str());
	LEAVEONERROR(hr);
	variant_t vtEmpty;

	VariantInit(&vtEmpty);
	VariantInit(&Result);
	hr = _type->InvokeMember_3(bstrMethod,
		(BindingFlags)(BindingFlags_InvokeMethod | BindingFlags_Public | BindingFlags_Static),
		nullptr,
		vtEmpty,
		Parameters,
		&Result
	);
	LEAVEONERROR(hr);

	return S_OK;
}

HRESULT CLRUtilities::ExecuteStaticMethod(const wstring& TypeName, const wstring& Method, variant_t Parameter, VARIANT& Result)
{
	SAFEARRAY* params = SafeArrayCreateVector(VT_VARIANT, 0, 1);
	LONG i = 0;

	HRESULT hr = SafeArrayPutElement(params, &i, &Parameter);
	LEAVEONERROR(hr);

	hr = ExecuteStaticMethod(TypeName, Method, params, Result);
	SafeArrayDestroy(params);
	return hr;

}

HRESULT CLRUtilities::UnloadDomain()
{
	if (NULL == appDomain)
		return E_POINTER;
	HRESULT hr = runtimeHost->UnloadDomain(appDomain);
	return hr;
}

HRESULT CLRUtilities::UnloadDomainByName(wstring DomainName)
{
	appDomainName = DomainName;
	HRESULT hr = FindAppDomain();
	if (S_OK == hr && (nullptr != appDomain))
	{
		return UnloadDomain();
	}
	return hr;
}
