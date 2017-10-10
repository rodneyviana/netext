#include "NetExt.h"
#include "CLRHelper.h"

EXT_COMMAND(wapppool,
	"Display the Application Pool details",
	"{custom}"
)
{

	ExtRemoteTyped Peb("(ntdll!_PEB*)@$extin", EvalExprU64("@$peb"));
	ExtRemoteTyped size = Peb.Field("ProcessParameters.CommandLine.Length");
	ExtRemoteTyped Command = Peb.Field("ProcessParameters.CommandLine.Buffer");
	ZeroMemory(NameBuffer, sizeof(NameBuffer));
	
	USHORT realSize = size.GetUshort();
	
	if (realSize > (MAX_MTNAME - 1))
		realSize = MAX_MTNAME - 1;

	ULONG64 Addr = Command.GetPtr();
	ExtRemoteData buffer(Addr, realSize);
	buffer.ReadBuffer(NameBuffer, realSize, false);
	

	std::wstring cmdLine((wchar_t*)NameBuffer);

	std::wregex exp(L"(w3wp.exe)\\s+-ap\\s+\"([^\"]+)\"\\s+-v\\s+\"([^\"]+)\"");
	
	std::wsmatch matches;

	if (!std::regex_search(cmdLine, matches, exp))
	{
		Out("It is not a w3wp process or Application Pool is not defined\n");
		return;
	}

	Out("AppPool Name         : %S\n", matches[2].str().c_str());
	Out("AppPool .NET Version : %S\n", matches[3].str().c_str());
	NativeModule mod("w3wp");
	if(mod.IsValid())
	{
		ModuleVersion version = mod.GetVersionInfo();
		Out("IIS Version          : %i.%i.%i.%i\n", version.Major, version.Minor, version.Build, version.Revision);
	}
	auto envVars = EXT_CLASS::GetProcessEnvVar();
	Out("Full Command Line    : %S\n", NameBuffer);
	Out("Process Account      : %S\n", EXT_CLASS::GetProcessAccount().c_str());
	Out("Machine Name         : %S\n", GetVar(L"COMPUTERNAME").c_str());

	wstring domainName = GetVar(L"USERDOMAIN");

	if(domainName.size() != 0)
	{
		Out("Domain Name          : %S\n", domainName.c_str());
	}

}