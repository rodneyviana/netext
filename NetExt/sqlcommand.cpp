#include "NetExt.h"
#include "CLRHelper.h"
#include "Indexer.h"
#include "SpecialCases.h"


struct SqlFlags
{
	CLRDATA_ADDRESS Address;
	string command;
	bool sproc;
	bool active;
};

EXT_COMMAND(wsql,
	"Dump all sql commands, a single sql command or commands matching a cookie filter criteria. Use '!whelp wsql' for detailed help",
	"{;e,o;;Address, SqlCommand Address. Optional}"
	"{command;s,o;;Dump only SQL Commands/Stored Proc containing the text pattern. Optional (e.g. -command *INSERT*INTO*)}"
	"{sproc;b,o;;Show only stored procedures commands}"
	"{active;b,o;;Show only commands with active connections}"
	)
{
	INIT_API();
	SqlFlags flag;
	flag.Address = HasUnnamedArg(0) ? GetUnnamedArgU64(0) : 0;
	if(!indc && (flag.Address == 0))
	{
		Dml("To list all sqlcommand, run <link cmd=\"!windex;!wsql\">!windex</link> first\n");
		return;
	}
	if(flag.Address != 0)
	{
		ObjDetail obj(flag.Address);
		if(!obj.IsValid() || obj.TypeName() != L"System.Data.SqlClient.SqlCommand")
		{
			Out("Could not find a valid System.Data.SqlClient.SqlCommand object at %p", flag.Address);
			return;
		}
	}
	
	flag.sproc = HasArg("sproc");
	flag.active = HasArg("active");

	if(HasArg("command"))
		flag.command = GetArgStr("command");


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

	std::map<std::wstring, std::vector<CLRDATA_ADDRESS>> mapConnection;

	adenum.Start(addresses);
	std::vector<std::string> fields;

	fields.push_back("_commandText");
	fields.push_back("_commandType");
	fields.push_back("_activeConnection");
	fields.push_back("_activeConnection._userConnectionOptions._usersConnectionString");
	fields.push_back("_activeConnection._innerConnection._state");

	int total = 0;
	int filtered = 0;
	static const std::string connStateStr[] = { "Closed     ", "Open       ", "Connecting ", "Executing  ", "Fetching   ", "Broken     " };


	while(CLRDATA_ADDRESS curr=adenum.GetNext())
	{
		if(IsInterrupted())
			return;
		varMap fieldsV;
		DumpFields(curr,fields,0,&fieldsV);
		bool include = true;
		if(flag.command.size() > 0 && !MatchPattern(CW2A(fieldsV["_commandText"].strValue.c_str()), flag.command.c_str()))
			include = false;
		if(flag.sproc && fieldsV["_commandType"].Value.i32 != 4)
			include = false;
		// Some commands waiting deesposing may show inconsistent results
		//  that we only filter if it is showing all results
		if(flag.Address == 0 && fieldsV["_commandType"].Value.i32 == 0)
			include = false;
		if(flag.active && (NULL == fieldsV["_activeConnection"].Value.ptr || 
			fieldsV["_activeConnection._innerConnection._state"].Value.i32 == 0))
			include = false;
		std::wstring connectionName = L"<NOT SET OR CLOSED>";
		if(NULL != fieldsV["_activeConnection._userConnectionOptions._usersConnectionString"].IsString())
			connectionName = fieldsV["_activeConnection._userConnectionOptions._usersConnectionString"].strValue;
		if(include)
			mapConnection[connectionName].push_back(curr);
		else
			filtered++;
		total++;
		if(total % 100 == 0)
			Out(".");
	}
	Out("\n");
	fields.push_back("_parameters._items._size");
	fields.push_back("_parameters._items._items");
	fields.push_back("_activeConnection._innerConnection._createTime.dateData");
	fields.push_back("_activeConnection._userConnectionOptions._maxPoolSize");
	fields.push_back("_activeConnection._userConnectionOptions._pooling");
	fields.push_back("_activeConnection._poolGroup._poolCount");
	//fields.push_back("_activeConnection._innerConnection._poolGroup._poolCollection");
	//fields.push_back("_activeConnection._userConnectionOptions._dataSource");
	//fields.push_back("_activeConnection._userConnectionOptions._initialCatalog");
	//fields.push_back("_activeConnection._userConnectionOptions._maxPoolSize");
	//fields.push_back("_activeConnection._userConnectionOptions._minPoolSize");
	//fields.push_back("_activeConnection._userConnectionOptions._packetSize");
	//fields.push_back("_activeConnection._userConnectionOptions._typeSystemVersion");
	//fields.push_back("_activeConnection._userConnectionOptions._connectTimeout");
	//fields.push_back("_activeConnection._userConnectionOptions._connectTimeout");

	for(auto it = mapConnection.begin(); it != mapConnection.end(); it++)
	{
		Out("===========================================================================================================\n");
		Out("Connection String: [%S]\n\n", it->first.c_str());
		for(int i=0;i<it->second.size();i++)
		{
			if(IsInterrupted())
				return;
			auto curr = it->second[i];
			Out("[%3i]: %p ",i,sizeof(void*) == 4 ? static_cast<ULONG>(curr) : curr);
			varMap fieldsV;
			DumpFields(curr,fields,0,&fieldsV);
			int cmdType = fieldsV["_commandType"].Value.i32;
			std::string cmdTypeStr;
			switch (cmdType)
			{
			case 1:
				cmdTypeStr = "Text            ";
				break;
			case 2:
				cmdTypeStr = "Table           ";
				break;
			case 4:
				cmdTypeStr = "Stored Procedure";
				break;
			case 0x100:
				cmdTypeStr = "File            ";
				break;
			case 0x200:
				cmdTypeStr = "Table Direct    ";
				break;
			default:
				cmdTypeStr = "<Unknown>       ";
				break;
			}
			std::string stateStr;
			int state = fieldsV["_activeConnection._innerConnection._state"].Value.i32;
			int p = 1;
			if(0 == state)
				stateStr = connStateStr[0];
			for(int i=1;i<6;i++)
			{
				if((state & p) == p)
					stateStr.append(connStateStr[i]);
				p*=2;
			}
			Out(" Type: %s State: %s ", cmdTypeStr.c_str(), stateStr.c_str());
			if(fieldsV["_activeConnection._innerConnection._createTime.dateData"].Value.i64 != 0)
			{
				auto ticks = SpecialCases::TicksFromTarget() - fieldsV["_activeConnection._innerConnection._createTime.dateData"].Value.i64;
				Out("Running Time: %s ",tickstotimespan(ticks).c_str());
			}

			if(fieldsV["_activeConnection._userConnectionOptions._pooling"].Value.b)
			{
				Out("Pool Connection: %i of %i ",fieldsV["_activeConnection._poolGroup._poolCount"].Value.i32, fieldsV["_activeConnection._userConnectionOptions._maxPoolSize"].Value.i32);
			}
			Out("\n\n");
			Out("%S\n\n", fieldsV["_commandText"].strValue.c_str());
			if(fieldsV["_parameters._items._size"].Value.i32 != 0)
			{
				Out("Parameters:\n");
				Out("=============\n");
				FromFlags flag;
				ZeroMemory(&flag, sizeof(flag));
				flag.cmd = "_parameterName,\"=\",$pp(_value)";
				flag.farray = true;
				flag.nofield = true;
				flag.nospace = true;
				flag.obj = fieldsV["_parameters._items._items"].Value.i64;
				wfrom_internal(flag);
				Out("\n");
			}

		}


	}

	if(flag.Address == 0)
	{
		Out("\n%S SQL Commands listed. %S Skipped by filter\n", formatnumber(static_cast<UINT64>(total-filtered)).c_str(), formatnumber(static_cast<UINT64>(filtered)).c_str());
	}
}