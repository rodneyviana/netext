/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#include "CLRHelper.h"
#include "Indexer.h"
#include "SpecialCases.h"


void PrintSocketInternal(SocketData& sock, CLRDATA_ADDRESS addr)
{
		g_ExtInstancePtr->Out("Socket Details\n");
		g_ExtInstancePtr->Out("========================\n");
		g_ExtInstancePtr->Out("Address         : %p\n",addr);
		if(sock.rightEndpoint)
			g_ExtInstancePtr->Out("IP Address      : %S\n",SpecialCases::IPAddress(sock.rightEndpoint).c_str());
		else
			g_ExtInstancePtr->Out("IP Address      : (null)\n",SpecialCases::IPAddress(sock.rightEndpoint).c_str());
		if(sock.remoteEndpoint && (sock.remoteEndpoint != sock.rightEndpoint))
			g_ExtInstancePtr->Out("Remote IP       : %S\n",SpecialCases::IPAddress(sock.remoteEndpoint).c_str());
		g_ExtInstancePtr->Out("Is Disposed?    : %s\n", sock.IsDisposed() ? "Yes" : "No");
		if(sock.IsDisposed())
		{
			g_ExtInstancePtr->Out("\n");
			return;
		}
		g_ExtInstancePtr->Out("Is Listening?   : %s\n", sock.isListening ?  "Yes" : "No");
		g_ExtInstancePtr->Out("Is Connected?   : %s\n", sock.isConnected ?  "Yes" : "No");
		g_ExtInstancePtr->Out("Fully Initiated?: %s\n", sock.IsFullyInitialized() ?  "Yes" : "No");
		g_ExtInstancePtr->Out("Owns Handle     : %s\n", sock.OwnsHandle() ?  "Yes" : "No");
		
		if(sock.closeTimeout == -1)
			g_ExtInstancePtr->Out("Close Timeout   : Not Set\n");
		else
			g_ExtInstancePtr->Out("Close Timeout   : %i\n", sock.closeTimeout);
		g_ExtInstancePtr->Dml("OS Handle       : <link cmd=\"!handle %p f\">%x</link>\n", sock.Handle, sock.Handle);
		g_ExtInstancePtr->Out("\n");
}

//
// Mostly contribution by James Davis from his post at http://www.debugthings.com/2015/03/31/netext-windbg/
//
EXT_COMMAND(wsocket,
	"Dump a single socket or a summary of all sockets. Use '!whelp wsocket' for detailed help",
	"{;e,o;;Address, Socket Address (optional)}"
	"{ip;s,o;;Filter Socket information for partial IP addresses summary (eg. -ip 192.168.0)}"
	)
{
	DO_INIT_API;
	CLRDATA_ADDRESS addr = 0;


	if (HasUnnamedArg(0))
	{
		addr = GetUnnamedArgU64(0);
		ObjDetail obj(addr);

		if (!obj.IsValid())
		{
			Out("Error: Could not find a valid object at %p\n", addr);
			return;
		}
		if (obj.TypeName() != L"System.Net.Sockets.Socket")
		{
			Out("Error: Expected type Socket. Found: %S\n", obj.TypeName().c_str());
			return;
		}
		SocketData sock;
		if(!SpecialCases::GetSocketData(addr, &sock))
		{
			Out("Error: Unable to retrieve Socket data\n");
			return;

		}

		PrintSocketInternal(sock, addr);
		return;

	}

	wstring ipaddress;
	bool searchIP = HasArg("ip");
	if (searchIP)
	{
		ipaddress.assign(CA2W(GetArgStr("ip")));
	}

	if (!indc && !addr)
	{
		Dml("To list all sockets, run <link cmd=\"!windex;!wsocket\">!windex</link> first\n");
		return;
	}

	MatchingAddresses addresses;

	indc->GetByType(L"System.Net.Sockets.Socket", addresses);

	AddressEnum adenum;
	if (addresses.size() == 0)
	{
		Out("Found no Socket object(s) in heap\n");
		return;
	}

	AddressList tempVector;
	adenum.Start(addresses);
	int totalSockets = 0;
	int disposed = 0;
	int connectedSockets = 0;
	int disconnected = 0;
	int listeningSockets = 0;
	int displayedSockets = 0;
	map<wstring, SocketDetail> summaryIPMap;
	while (CLRDATA_ADDRESS curr = adenum.GetNext())
	{
		++totalSockets;
		if (IsInterrupted())
			return;


		SocketData sock;
		SpecialCases::GetSocketData(curr, &sock);

		if(sock.rightEndpoint)
		{
			wstring sockAddr = SpecialCases::IPAddress(sock.rightEndpoint);
			if(!searchIP || (sockAddr.find(ipaddress) != wstring::npos))
			{
				displayedSockets++;
				summaryIPMap[sockAddr].total++;
				if(sock.IsListening())
				{
					summaryIPMap[sockAddr].isServer = true; // for server it is only one "listening" socket
					listeningSockets++;
				}
				if(sock.IsClosed())
				{
					summaryIPMap[sockAddr].disconnected++;
					disconnected++;
				}
				if(sock.IsDisposed())
				{
					summaryIPMap[sockAddr].disposed++;
					disposed++;
				}
				if(sock.IsConnected())
				{
					connectedSockets++;
					summaryIPMap[sockAddr].connected++;
				}

			}
		}
		
	}
	Out("Type      Total Connected Disconnected Disposed Target IP\n");
	Out("===================================================================================\n");


	for (auto &x : summaryIPMap)
	{
		Out("%s",x.second.isServer ? "Server" : "Client");
		Out("%9S",formatnumber((UINT64)x.second.total).c_str());
		Out("%10S",formatnumber((UINT64)x.second.connected).c_str());
		Out("%13S",formatnumber((UINT64)x.second.disconnected).c_str());
		Out("%9S ",formatnumber((UINT64)x.second.disposed).c_str());
		Out("%S\n", x.first.c_str());
	}
	Out("\n");
	Out("Total Sockets: %10S\n",formatnumber((UINT64)totalSockets).c_str());
	if(totalSockets - displayedSockets > 0)
			Out("Skipped      : %10S\n",formatnumber((UINT64)(totalSockets - displayedSockets)).c_str());
	Out("Disposed     : %10S\n",formatnumber((UINT64)disposed).c_str());
	Out("Connected    : %10S\n",formatnumber((UINT64)connectedSockets).c_str());
	Out("Disconnected : %10S\n",formatnumber((UINT64)disconnected).c_str());
	Out("\n");
	Out("You may be also interested in:\n");
	Out("==============================\n");
	wstring str = L"!wfrom -type System.Net.Sockets.Socket where(%s) $a(\"\\n==================================\\nAddress         \",$addr()),$a(\"IP Address      \",$ipaddress(m_RightEndPoint)),$a(\"Remote IP       \",$ipaddress(m_RemoteEndPoint)),$a(\"Is Disposed?    \",$if(m_IntCleanedUp,\"Yes\", \"No\")),$a(\"Is Connected?   \",$if(m_IsConnected,\"Yes\", \"No\"))";
	
	wstring strHtml = L"<link cmd=\"" + SpecialCases::HtmlEncode(str) + L"\">" + SpecialCases::HtmlEncode(str) + L"</link>\n";

	Out("Connected Sockets: ");
	//Out(str.c_str(),"m_IsConnected");
	Dml(strHtml.c_str(), L"m_IsConnected", L"m_IsConnected");
	Out("Listening Sockets: ");
	//Out(str.c_str(), "isListening");
	Dml(strHtml.c_str(), L"isListening", L"isListening");

	Out("Disconnected     : ");
	//Out(str.c_str(), "m_IsDisconnected");
	Dml(strHtml.c_str(), L"m_IsDisconnected", L"m_IsDisconnected");

	Out("Disposed Sockets : ");
	//Out(str.c_str(), "m_IntCleanedUp");
	Dml(strHtml.c_str(), L"m_IntCleanedUp", L"m_IntCleanedUp");
	
	Out("\n");

}
