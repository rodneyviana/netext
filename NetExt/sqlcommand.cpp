#include "NetExt.h"
#include "CLRHelper.h"
#include "Indexer.h"

struct SqlFlags
{
	CLRDATA_ADDRESS Address;
	string param;
	string value;
	bool summary;
	bool sproc;
};

EXT_COMMAND(wsql,
	"Dump all sql commands, a single sql command or commands matching a cookie filter criteria. Use '!whelp wsql' for detailed help",
	"{;e,o;;Address, SqlCommand Address. Optional}"
	"{param;s,o;;Dump only commands containing parameter with this name. Optional (e.g -param *ID)}"
	"{value;s,o;;Dump only if parameter value matches. Optional (e.g. -value a4ghj8abcd*)}"
	"{command;s,o;;Dump only SQL Commands containing the pattern. Optional (e.g. -command *INSERT*INTO*)}"
	"{summary;b,o;;Show only the summary (count by SQL Command)}"
	"{sproc;b,o;;Show only stored procedures commands}"
	)
{
	SqlFlags flag;

	if(!indc && !HasUnnamedArg(0))
	{
		Dml("To list all sqlcommand, run <link cmd=\"!windex;!wsql -summary\">!windex</link> first\n");
		return;
	}
	flag.Address = HasUnnamedArg(0) ? GetUnnamedArgU64(0) : 0;
	ObjDetail obj(flag.Address);
	if(!obj.IsValid() || obj.TypeName() != L"System.Data.SqlClient.SqlCommand")
	{
		Out("Could not find a valid System.Data.SqlClient.SqlCommand object at %p", flag.Address);
		return;
	}
	flag.summary = HasArg("summary");
	flag.sproc = HasArg("sproc");

	if(HasArg("param"))
		flag.param = GetArgStr("param");

	if(HasArg("value"))
		flag.value = GetArgStr("value");

	MatchingAddresses addresses;
	AddressList al;
	al.push_back(flag.Address);
	std::map<string, wstring> summary;

	if(flag.Address)
	{
		addresses.push_back(&al);
	} else
	{
		indc->GetByType(L"System.Data.SqlClient.SqlCommand", addresses);
	}
	AddressEnum adenum;
	if(addresses.size()==0)
	{
		Out("Found no SQL Command object in heap\n");
		return;
	}
	AddressList tempVector;


	adenum.Start(addresses);
	std::vector<std::string> fields;
	fields.push_back("_commandText");
	fields.push_back("_commandType");
	fields.push_back("_commandTimeout");
	fields.push_back("_activeConnection._connectionString");
	fields.push_back("_activeConnection._innerConnection._state");
	fields.push_back("_activeConnection._innerConnection._connectionIsDoomed");
	fields.push_back("_activeConnection._innerConnection._poolGroup._poolCollection");
	fields.push_back("_activeConnection._userConnectionOptions._dataSource");
	fields.push_back("_activeConnection._userConnectionOptions._initialCatalog");
	fields.push_back("_activeConnection._userConnectionOptions._maxPoolSize");
	fields.push_back("_activeConnection._userConnectionOptions._minPoolSize");
	fields.push_back("_activeConnection._userConnectionOptions._packetSize");
	fields.push_back("_activeConnection._userConnectionOptions._typeSystemVersion");
	fields.push_back("_activeConnection._userConnectionOptions._connectTimeout");
	fields.push_back("_activeConnection._userConnectionOptions._connectTimeout");

/*
0:074> !wdo 000000f9ecc5c7a8
[DEBUG] Change Debugee Status: REFRESH - 1
Address: 000000f9ecc5c7a8
Method Table/Token: 00007ffefb053820/200028d04 
Class Name: System.Data.SqlClient.SqlConnectionString
Size : 208
EEClass: 00007ffefae98ee8
Instance Fields: 41
Static Fields: 4
Total Fields: 82
Heap/Generation: 2/1
Module: 00000000fae70000
Assembly: 00000000b8f935f0
Domain: 00000000b8a9f3b0
Assembly Name: C:\windows\Microsoft.Net\assembly\GAC_64\System.Data\v4.0_4.0.0.0__b77a5c561934e089\System.Data.dll
Inherits: System.Data.Common.DbConnectionOptions System.Object (00007FFEFB0548F8 00007FFF0AF3FCA0)
00007fff0af3f6c0                                    System.String +0000                   _usersConnectionString 000000f9ecc5c560 Data Source=SP16SQL092;Initial Catalog=SVC_PP_SVC_User_Profile_DB_AD;Integrated Security=True;Enlist=False;Pooling=True;Min Pool Size=0;Max Pool Size=100;Connect Timeout=60;Packet Size=8000;Application Name=SharePoint[w3wp][11][SVC_PP_SVC_User_Profile_DB_AD]
00007fff0af43608                     System.Collections.Hashtable +0008                              _parsetable 000000f9ecc5c878
00007ffefb055768                 System.Data.Common.NameValuePair +0010                                 KeyChain 000000f9ecc5c8f8
00007fff0af40ec8                    System.Security.PermissionSet +0018                           _permissionset 000000f9ecc5cf08
00007fff0af47500                                   System.Boolean +0020                       HasPasswordKeyword 0 (False)
00007fff0af47500                                   System.Boolean +0021                             UseOdbcRules 0 (False)
00007fff0af47500                                   System.Boolean +0022                      _integratedSecurity 1 (True)
00007fff0af47500                                   System.Boolean +0023                         _connectionReset 1 (True)
00007fff0af42238                                     System.Int32 +0024                          _connectTimeout 3c (0n60)
00007fff0af3f6c0                                    System.String +0028                         _applicationName 000000f9ecc5cdb0 SharePoint[w3wp][11][SVC_PP_SVC_User_Profile_DB_AD]
00007fff0af3f6c0                                    System.String +0030                        _attachDBFileName 000000fa6c0f1420 
00007fff0af3f6c0                                    System.String +0038                         _currentLanguage 000000fa6c0f1420 
00007fff0af3f6c0                                    System.String +0040                              _dataSource 000000f9ecc5c8c8 SP16SQL092
00007fff0af3f6c0                                    System.String +0048                         _localDBInstance 0000000000000000 (null)
00007fff0af3f6c0                                    System.String +0050                         _failoverPartner 000000fa6c0f1420 
00007fff0af3f6c0                                    System.String +0058                          _initialCatalog 000000f9ecc5c928 SVC_PP_SVC_User_Profile_DB_AD
00007fff0af3f6c0                                    System.String +0060                                _password 000000fa6c0f1420 
00007fff0af3f6c0                                    System.String +0068                                  _userID 000000fa6c0f1420 
00007fff0af3f6c0                                    System.String +0070                          _networkLibrary 000000fa6c0f1420 
00007fff0af3f6c0                                    System.String +0078                           _workstationId 0000000000000000 (null)
00007fff0af44000                                   System.Version +0080               _typeSystemAssemblyVersion 000000fd2c1497d8
00007fff0af3f6c0                                    System.String +0088                _expandedAttachDBFilename 0000000000000000 (null)
00007fff0af42238                                     System.Int32 +0090                      _loadBalanceTimeout 0 (0n0)
00007fff0af42238                                     System.Int32 +0094                             _maxPoolSize 64 (0n100)
00007fff0af42238                                     System.Int32 +0098                             _minPoolSize 0 (0n0)
00007fff0af42238                                     System.Int32 +009c                              _packetSize 1f40 (0n8000)
00007fff0af42238                                     System.Int32 +00a0                       _connectRetryCount 1 (0n1)
00007fff0af42238                                     System.Int32 +00a4                    _connectRetryInterval a (0n10)
00007ffefb05f570          System.Data.SqlClient.ApplicationIntent +00a8                       _applicationIntent 0 (0n0) ReadWrite
00007ffefb071860         System.Data.SqlClient.SqlConnectionStrin +00ac                       _typeSystemVersion 7d8 (0n2008) SQLServer2008
00007ffefb069608         System.Data.SqlClient.SqlConnectionStrin +00b0                      _transactionBinding 0 (0n0) ImplicitUnbind
00007fff0af47500                                   System.Boolean +00b4                       _contextConnection 0 (False)
00007fff0af47500                                   System.Boolean +00b5                                 _encrypt 0 (False)
00007fff0af47500                                   System.Boolean +00b6                  _trustServerCertificate 0 (False)
00007fff0af47500                                   System.Boolean +00b7                                  _enlist 0 (False)
00007fff0af47500                                   System.Boolean +00b8                                    _mars 0 (False)
00007fff0af47500                                   System.Boolean +00b9                     _persistSecurityInfo 0 (False)
00007fff0af47500                                   System.Boolean +00ba                                 _pooling 1 (True)
00007fff0af47500                                   System.Boolean +00bb                             _replication 0 (False)
00007fff0af47500                                   System.Boolean +00bc                            _userInstance 0 (False)
00007fff0af47500                                   System.Boolean +00bd                     _multiSubnetFailover 0 (False)

0:074> !wdo 000000f96c128208
[DEBUG] Change Debugee Status: REFRESH - 1
Address: 000000f96c128208
Method Table/Token: 00007ffefb0547e0/200022904 
Class Name: System.Data.ProviderBase.DbConnectionPool
Size : 168
EEClass: 00007ffefaebdd68
Instance Fields: 23
Static Fields: 2
Total Fields: 46
Heap/Generation: 0/2
Module: 00000000fae70000
Assembly: 00000000b8f935f0
Domain: 00000000b8a9f3b0
Assembly Name: C:\windows\Microsoft.Net\assembly\GAC_64\System.Data\v4.0_4.0.0.0__b77a5c561934e089\System.Data.dll
Inherits: System.Object (00007FFF0AF3FCA0)
00007ffefb057920         System.Data.ProviderBase.DbConnectionPoo +0000                                _identity 000000f96c157de0
00007ffefb052de0         System.Data.ProviderBase.DbConnectionFac +0008                       _connectionFactory 000000fd2c141810
00007ffefb056998         System.Data.ProviderBase.DbConnectionPoo +0010                     _connectionPoolGroup 000000f96c126fd0
00007ffefb055a28         System.Data.ProviderBase.DbConnectionPoo +0018              _connectionPoolGroupOptions 000000f96c126fa8
00007ffefb067410         System.Data.ProviderBase.DbConnectionPoo +0020              _connectionPoolProviderInfo 0000000000000000
00007ffefae771b0         System.Collections.Concurrent.Concurrent +0028                                _stackOld 000000f96c1282b0
00007ffefae771b0         System.Collections.Concurrent.Concurrent +0030                                _stackNew 000000f96c1282d0
00007ffefae77268         System.Collections.Concurrent.Concurrent +0038                            _pendingOpens 000000f96c1282f0
00007fff0af4b040                    System.Threading.WaitCallback +0040                       _poolCreateRequest 000000f96c1289e0
00007ffefb051998         System.Data.ProviderBase.DbConnectionPoo +0048                             _waitHandles 000000f96c1284b8
00007fff0af3f8f0                                 System.Exception +0050                                _resError 0000000000000000
00007fff0af49428                           System.Threading.Timer +0058                              _errorTimer 0000000000000000
00007fff0af49428                           System.Threading.Timer +0060                            _cleanupTimer 000000f96c128a60
00007ffefb052268         System.Data.ProviderBase.DbConnectionPoo +0068                _transactedConnectionPool 000000f96c128968
00007ffefae77330         System.Collections.Generic.List<System.D +0070                              _objectList 000000f96c128600
00007fff0af42238                                     System.Int32 +0078                             _cleanupWait 30d40 (0n200000)
00007ffefb05ff20         System.Data.ProviderBase.DbConnectionPoo +007c                                   _state 1 (0n1) Running
00007fff0af42238                                     System.Int32 +0080                     _pendingOpensWaiting 0 (0n0)
00007fff0af42238                                     System.Int32 +0084                               _waitCount 0 (0n0)
00007fff0af42238                                     System.Int32 +0088                               _errorWait 1388 (0n5000)
00007fff0af42238                                     System.Int32 +008c                            _totalObjects 2 (0n2)
00007fff0af42238                                     System.Int32 +0090                                _objectID 9 (0n9)
00007fff0af47500                                   System.Boolean +0094                           _errorOccurred 0 (False)
00007fff0af4a698 Static                             System.Random +0b98                                  _random 000000f96c157ea8
00007fff0af42238 Static                              System.Int32 +09f8                         _objectTypeCount c (0n12)

[DEBUG] Change Debugee Status: REFRESH - 2

*/
	//std::map<string, long> sumList;
	wstring url;
	int statusCode;
	wstring status;
	INT64 ticks;
	//while(CLRDATA_ADDRESS curr=adenum.GetNext())
}