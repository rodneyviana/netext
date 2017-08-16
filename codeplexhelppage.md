# UPDATE
NetExt is back. New commands added not yet included in this help. Use !whelp for all commands


# LATEST VERSION: 2.1.10.5000


# Description



*Getting started*


- Open WinDBG. Load netext
- Make sure you open the appropriate 32-bits or 64-bits extension (32-bits dumps require winbg 32-bits and netext 32-bits)
- For detailed help, run: 
".browse !whelp"
- Run: "[!windex](#windex) -tree" and follow the instructions
- All the rest will be intuitive
- For scripts, see [!wfrom](#wfrom) and [!wselect](#wselect)
- Download the training material here: [https://netext.codeplex.com/releases/view/611486] - Training material is NOW up-to-date.

*COMPATIBLE WITH .NET 2.0 to 4.5.x*


*Common Resources*


[List of available commands](#menu)
[Examples](#examples)


*This windbg debug extension works as data mining for .NET.*


You can do select like queries to .NET objects including sublevel fields.


For example, to get the url of a HttpContext it is necessary to
1. !do the httpcontext instance, get address of _request
2. !do the HttpRequest instance, get the address of _url
3. !do the URI instance, get the address of m_String
4. !do the instance of the string object.


*Using netext you only need to issue a command like:*
`!wselect _request._url.m_String, _response._statusCode from 0x242afe8`

*Or to list all requests that are "http:" and the status code is not 200 (OK) from ALL HttpRequests:*

`!wfrom -type *.HttpContext 
  where ( ($contains(_request._url.m_String, "http:")) && (_response._statuscode != 0n200) ) 
  select $addr(), _request._url.m_String, _response._statusCode

calculated: 0n5731369072
_request._url.m_String: http://rviana-serv.contoso.com:80/TestClass/Service.svc/net
_response._statusCode: 0n401
calculated: 0n6802002784
_request._url.m_String: http://rviana-serv.contoso.com:80/TestClass/Service.svc/net
_response._statusCode: 0n401 `

It also works very well showing arrays (!wdo, !wselect and !wfrom) and providing link to the objects or showing the items value depending on the content of the array.

*THE EXTENSION DOES NOT REQUIRE SOS OR PSSCORX TO WORK. It access .NET debugging API directly without intermediary.*

Please use netext.dll for .NET 2.0-4.5.x. There are both 32 and 64-bits versions.

<a link id='examples'></a>
*Examples:*
`
.load netext
0:000> !windex -type *.httpcontext

(...)
00000001956e5360 000007feda232488      336   1 0 System.Web.HttpContext
0000000195702098 000007feda232488      336   1 0 System.Web.HttpContext
`

`
0:000> !wselect _request._url.m_String, _response._statuscode from 0000000195702098
System.String _request._url.m_String = 00000001559cb3a8 http://rviana-serv.contoso.com:80/TestClass/Service.svc/net
(int32)System.Int32 _response._statuscode = c8 (0n200)

0:000> !wselect * from 000000015579ec00
System.String Key = 000000015578c6c8 assembly
System.Object Value = 000000015579EBD8
`

_Note: !wselect does not accept expressions or conditionals but it accepts wildcard fields_

`
0:000> !wdo 0000000195702098
Address: 0000000195702098
EEClass: 000007fed9e923b8
Method Table: 000007feda232488
Class Name: System.Web.HttpContext
(...)
Inherits: System.Web.HttpContext System.Object (000007FEDA232488 000007FEEFC07370)
07feda234fb0 System.Web.IHttpAsyncHandler +0000   _asyncAppHandler 0000000000000000
07feda234ae0 System.Web.HttpApplication +0008         _appInstance 0000000000000000
(...)
07feefc47fb8 System.DateTime +0120                   _utcTimestamp -mt 07FEEFC47FB8 01957021C0 10/26/2011 9:16:11 PM
(...)
07feefc47eb8 System.TimeSpan +0138                       _timeout -mt 07FEEFC47EB8 01957021D8 03:14:07
007feefc0ecf0 System.Int32 +010c                     _timeoutState 0 (0n0)
(...)
007feefc06c50 System.Boolean +0118      _finishPipelineRequestCalled 1 (True)
(...)
`

`
0:000> !wdo 00000001556d1dc8
Address: 00000001556d1dc8
EEClass: 000007feef80eb58
Method Table: 000007feefbf5870
Class Name: System.Object[]
Size : 48
Rank: 1
Components: 2
[0]: 00000001556d27e0 <IPermission class="System.Security.Permissions.MediaPermission, WindowsBase,
Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" version="1" Audio="SafeAudio" Video="SafeVideo" Image="SafeImage"/>
[1]: 00000001556d33d8 <IPermission class="System.Security.Permissions.WebBrowserPermission, WindowsBase,
Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" version="1" Level="Safe"/>
`

`
0:000> ~*e!wstack
Listing objects from: 0000000000284000 to 0000000000290000 from thread: 0 [21a0]
Listing objects from: 000000000088e000 to 0000000000890000 from thread: 1 [263c]
(...)
Listing objects from: 0000000004208000 to 0000000004210000 from thread: 20 [1780]
@rcx=00000001956e6318 000007feefc39f88 System.Threading._TimerCallback 1 0
(...)
0000000195701d78 000007feda23a620 System.Web.Hosting.IIS7WorkerRequest 1 0
0000000195720380 000007feefc01808 System.Threading.ContextCallback 1 0
00000001556e5490 000007feda22c148 System.Web.HttpRuntime 0 0
(...)

`
<a link id='menu'></a>

*List of commands*

*Commands to Show Object Details*
*--------------------------------------*
- [!wdo](#wdo) - Display ad-hoc objects or arrays from GAC or Stack	
- [!wselect](#wselect) - Display ad-hoc fields (and level fields) for an object or for all item in an array
- [!wfrom](#wfrom) - Perform SQL-like analysis of Heap objects enabling comparison, expression evaluation and indexed filtering.

*Enumerate objects*
*---------------------*
- [!windex](#windex) - index and display objects based in different filters like object with of type HttpContext
- [!wstack](#wstack) - dump unique stack objects
- [!wheap](#wheap) - list objects without indexing and show thottled heap sampling
- [!wgchandles](#wgchandles) - Dump GC root handles

*Special Purpose*
*------------------*
- [!wdict](#wdict) - Display dictionary objects
- [!whash](#whash) - Display HashTable objects
- [!whttp](#whttp) - List HttpContext Objects
- ![wconfig](#wconfig) - Show all .config file lines in memory 
- [!wservice](#wservice) - List WCF service Objects
- [!weval](#weval) - Evaluate expression list
- [!wclass](#wclass) - Show "reflected" class definition (fields, properties and methods)\(*new*) 
- [!wkeyvalue](#wkeyvalue) - Display pair key/value for NameObjectCollection type objects
- [!wcookie](#wcookie) - Display HTTP cookies using filters and grouping
- [!wruntime](#wruntime) - Display HTTP Runtime Info including Active Requests count
- [!wtoken](#wtoken) - Display WIF tokens and claims
- [!wthreads](#wthreads) - Dump thread information
- [!wver](#wver) - Show version of the .NET framework(s) present in the process or dump and extension version
- [!wupdate](#wupdate) - Check for new versions and compare with current. If a new version is found, it tries to open the update page
- [!wdomain ](#wdomain) - Dump all Application Domains
- [!wdae ](#wdae) - Dump all exceptions in the heap
- [!wpe ](#wpe) - Dump Exception Object

----


[expression syntax](#expression)


[functions list](#functions] *new functions*


<a link id='wpe'></a>
*!wpe - Dump Details of an Exception Object*

`
Syntax:
---------
!wpe <expr>
Where:
	<expr> is the Exception object address (you can use an expression).
 
Examples:
---------

Dump a particular Exception
----------------------------------
0:00> !wpe 00000001045f0c68
Address: 00000001045f0c68
Exception Type: System.InvalidOperationException
Message: Could not retrieve a valid Windows identity.
Inner Exception: 00000001045de690 System.ServiceModel.EndpointNotFoundException There was no endpoint listening at net.pipe://localhost/s4u/022694f3-9fbd-422b-b4b2-312e25dae2a2 that could accept the message. This is often caused by an incorrect address or SOAP action. See InnerException, if present, for more details.
Stack:
SP               IP               Function
000000000c347490 000007feef2fd82c Microsoft.SharePoint.SPSecurityContext.GetWindowsIdentity()
000000000c349d60 000007feed97086a Microsoft.SharePoint.Administration.SPFarm.CurrentClaimsUserIsBoxAdministrator(Microsoft.IdentityModel.Claims.IClaimsIdentity)

HResult: 80131509

`

<a link id='wdae></a>
*!wdae - Dump all exceptions in the heap (you must index via !windex first)*
`


Syntax:
---------
!wdae
 
Examples:
---------

Show all Exceptions in heap
----------------------------------
0:00> !wdae

      71 of Type: System.ArgumentException 00000001011f78c8 00000001012074a0 000000010126b268
Message: 
Inner Exception: (none)
Stack:
IP               Function
000007feef31cff6 Microsoft.SharePoint.Administration.Claims.SPClaimEncodingManager.DecodeClaimFromFormsSuffix(System.String)
000007ff0026ba43 Microsoft.SharePoint.IdentityModel.SPTokenCache.ReadToken(Byte[])


(...)

       7 of Type: System.IO.PipeException 00000001045de600 0000000141c00398 0000000142096cc8
Message: The pipe endpoint 'net.pipe://localhost/s4u/022694f3-9fbd-422b-b4b2-312e25dae2a2' could not be found on your local machine. 
Inner Exception: (none)
Stack:
(no managed stack found)


       7 of Type: System.InvalidOperationException 00000001045f0c68 0000000141c165a0 00000001420b1420
Message: Could not retrieve a valid Windows identity.
Inner Exception: System.ServiceModel.EndpointNotFoundException
Stack:
IP               Function
000007feef2fd82c Microsoft.SharePoint.SPSecurityContext.GetWindowsIdentity()
000007feed97086a Microsoft.SharePoint.Administration.SPFarm.CurrentClaimsUserIsBoxAdministrator(Microsoft.IdentityModel.Claims.IClaimsIdentity)

(...)

     135 of Type: System.Threading.ThreadAbortException 00000001011fc1e8 0000000101738cc0 0000000101742a70
Message: Thread was being aborted.
Inner Exception: (none)
Stack:
IP               Function
000007feeab96798 System.Web.HttpApplication.ExecuteStep(IExecutionStep, Boolean ByRef)


      67 of Type: System.Threading.ThreadAbortException 0000000101738258 0000000101776210 00000001021a8580
Message: Thread was being aborted.
Inner Exception: (none)
Stack:
IP               Function
0000000000000000 System.Threading.Thread.AbortInternal()
000007fef2ea4419 System.Threading.Thread.Abort(System.Object)
000007feeabc6415 System.Web.HttpResponse.End()
000007feed84fb60 Microsoft.SharePoint.Utilities.SPUtilityInternal.SendResponse(System.Web.HttpContext, Int32, System.String)
000007feed808f9a Microsoft.SharePoint.Utilities.SPUtility.SendAccessDeniedHeader(System.Exception)
000007feedca07bd Microsoft.SharePoint.ApplicationRuntime.SPRequestModule.PostAuthenticateRequestHandler(System.Object, System.EventArgs)
000007feeaba59b0 System.Web.HttpApplication+SyncEventExecutionStep.System.Web.HttpApplication.IExecutionStep.Execute()
000007feeab9671b System.Web.HttpApplication.ExecuteStep(IExecutionStep, Boolean ByRef)

(...)
      67 of Type: System.UnauthorizedAccessException 0000000101737f98 0000000101775f50 00000001021a82c0
Message: Attempted to perform an unauthorized operation.
Inner Exception: (none)
Stack:
(no managed stack found)


      58 of Type: System.UriFormatException 0000000101229ab0 0000000101396670 00000001014c8560
Message: Invalid URI: The format of the URI could not be determined.
Inner Exception: (none)
Stack:
IP               Function
000007fef1d976bf System.Uri.CreateThis(System.String, Boolean, System.UriKind)
000007feeda41031 Microsoft.SharePoint.Utilities.SPUrlUtility.IsUrlFull(System.String)


491 Exceptions in 13 unique type/stack combinations (duplicate types in similar stacks may be rethrows)


`

<a link id='wdomain'></a>
*!wdomain - Dump domain information including name, base folder, config file and modules loaded*
`


Syntax:
---------
!wdomain
 
Examples:
---------

Dump all domains
----------------------------------
0:00> !wdomain

Address          Domain Name                                   Modules Base Path and Config
000007fef49c3820 System                                            112 
000007fef49c2f20 Shared                                                              
000000000153b9f0 DefaultDomain                                      44 Base Path: c:\windows\system32\inetsrv\ Config: w3wp.exe.config 
000000000153c300 /LM/W3SVC/1067026433/ROOT-1-130029361030647561    238 Base Path: C:\inetpub\wwwroot\wss\VirtualDirectories\80\ Config: web.config 


`


<a link id='wupdate'></a>
*!wupdate - Check for new versions and compare with current. If a new version is found, it tries to open the update page*
*If the page is not in http://netext.codeplex.com, do not download*
`


Syntax:
---------
!wuptade

`

<a link id='wver'></a>
*!wver - Show version of the .NET framework(s) present in the process or dump. It may have more than one framework in memory*
*It also shows the extension version*
`


Syntax:
---------
!wver
 
Examples:
---------

Show version of .NET and extension
-----------------------------------

0:00> !wver
Runtime(s) Found: 1
0: Filename: mscordacwks_amd64_Amd64_4.0.30319.18010.dll 
.NET Version: v4.0.30319.18010
NetExt (this extension) Version: 2.0.1.5000


`

<a link id='wthreads'></a>
*!wthreads - Dump thread information*
`


Syntax:
---------
!wthreads
 
Examples:
---------

Show all managed threads
--------------------------------

0:00> !wthreads
   Id OSId Address          Domain           Allocation Start:End              COM  GC Type  Locks Type / Status             Last Exception
    1 0bd4 000000000211c410 000000000153b9f0 0000000184ce85f0:0000000184cea5b0 MTA  Preemptive   0 Background               
    2 147c 0000000002160150 000000000153b9f0 0000000000000000:0000000000000000 MTA  Preemptive   0 Background|Finalizer     
    3 1068 00000000021a22e0 000000000153b9f0 0000000000000000:0000000000000000 MTA  Preemptive   0 Background|Timer         
    4 12b8 00000000021a28b0 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Background               
    5 0c04 00000000053dd170 000000000153b9f0 0000000000000000:0000000000000000 MTA  Preemptive   0 Background|Wait          
    6 1614 0000000005506290 000000000153b9f0 0000000000000000:0000000000000000 MTA  Preemptive   0 Background|IOCPort       
    8 18d0 0000000005413800 000000000153c300 00000001c2355020:00000001c2356f18 MTA  Preemptive   0 Background               
    9 1ac4 0000000005413dd0 000000000153c300 0000000000000000:0000000000000000 MTA  Preemptive   0 Background               
   10 09e8 00000000054143a0 000000000153c300 00000001045f1340:00000001045f1ac0 MTA  Preemptive   1 Background|Worker         System.InvalidOperationException
   11 1aa8 0000000005414970 000000000153c300 0000000000000000:0000000000000000 MTA  Preemptive   0 Background               
   12 192c 0000000005416c50 000000000153c300 0000000000000000:0000000000000000 MTA  Preemptive   0                          
   14 15c0 0000000005573580 000000000153c300 0000000184ecb720:0000000184ecc310 MTA  Cooperative  1 Background|Worker        
   16 1668 0000000005574120 000000000153c300 00000001046439e0:0000000104643ba0 MTA  Cooperative  1 Background|Worker         System.ArgumentException
   17 1440 00000000055746f0 000000000153b9f0 00000001822fffd8:0000000182301ea8 NONE Preemptive   0 Background               
   18 1900 0000000005574cc0 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Background               
   19 10e4 0000000005575290 000000000153b9f0 00000001c12aa268:00000001c12ac1d8 NONE Preemptive   0 Background               
   23 0368 0000000005578110 000000000153c300 000000010458e450:000000010458efe0 MTA  Preemptive   3 Background|Worker        
   24 1168 0000000005576fa0 000000000153c300 0000000184ee3938:0000000184ee41d0 MTA  Cooperative  1 Background|Worker         System.UriFormatException
   25 1b50 0000000005577570 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Background               
   27 ---- 0000000005579850 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Worker|Terminated        
   28 0d6c 0000000005579e20 000000000153c300 00000001c4b13b78:00000001c4b141d0 MTA  Cooperative  1 Background|Worker         System.ArgumentException
   29 12d0 0000000005579280 000000000153c300 0000000144bbfb08:0000000144bc1008 MTA  Preemptive   1 Background|Worker         System.ArgumentException
   30 0a2c 000000000557a3f0 000000000153b9f0 00000001045c36b0:00000001045c5018 MTA  Cooperative  2 Background|Worker        
   31 13b0 000000000d8e06e0 000000000153c300 00000001045f54f0:00000001045f5ac0 MTA  Cooperative  1 Background|Worker        
   32 1224 0000000005578cb0 000000000153b9f0 0000000103cdcf60:0000000103cdea10 MTA  Preemptive   0 Background|Worker        
   33 1058 000000000d8e0cb0 000000000153c300 00000001c4afecb8:00000001c4b001d0 MTA  Preemptive   1 Background|Worker         System.UriFormatException
   34 ---- 000000000d8e1280 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Worker|Terminated        
   35 ---- 000000000d8e1850 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Worker|Terminated        
   36 141c 000000000d8e1e20 000000000153b9f0 0000000103dc1548:0000000103dc2500 MTA  Preemptive   0 Background|Worker        
   40 1480 000000000d8e3560 000000000153b9f0 00000001040dbdd0:00000001040dc2b0 MTA  Preemptive   0 Background|Worker        
   41 ---- 000000000d8e3b30 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Worker|Terminated        
   43 ---- 000000000d8fde20 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Worker|Terminated        
   44 0e8c 000000000d8fd850 000000000153c300 00000001038dd878:00000001038de8b8 MTA  Preemptive   0 Background|Worker        
   48 ---- 000000000d8fe9c0 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Worker|Terminated        
   50 1468 000000000d8ff560 000000000153c300 0000000103f94ea8:0000000103f95578 MTA  Cooperative  0 Background|Worker        
   52 1a54 000000001715be10 000000000153b9f0 0000000000000000:0000000000000000 NONE Preemptive   0 Background               



`


<a link id='wkeyvalue'></a>
*!wkeyvalue - Display pair key/value for objects that derives NameObjectCollectionBase*
`

Usage: !wkeyvalue <expr>
Where:
	<expr> is the address of the object derived from NameObjectCollectionBase. Required

Examples:
-------------

Listing Http forms
------------------------
0:000> !wkeyvalue 00000000eda53b98
[System.Web.HttpValueCollection]
=====================================
__SPSCEditMenu=true
=====================================
MSOTlPn_View=0
=====================================
MSOTlPn_ShowSettings=False
=====================================
MSOGallery_SelectedLibrary=
=====================================
MSOGallery_FilterString=
=====================================
MSOTlPn_Button=none
=====================================
__EVENTTARGET=
=====================================
__EVENTARGUMENT=
=====================================
__REQUESTDIGEST=0x57EE4FDFADDAC752F6531DE0D1446CAF0135B5DA5183698D255CFDB406D14128533F085AE1500ECCBCABA3E838E8F8DA4AA0BFF028FDDF0010C824F948F976C0,09 Feb 2014 15:41:30 -0000
=====================================
MSOSPWebPartManager_DisplayModeName=Browse
=====================================
MSOSPWebPartManager_OldDisplayModeName=Browse
(...)

*Pairs with complex types*
----------------------------------
0:000> !wkeyvalue 000000016299ebd0
[System.Configuration.ConfigurationValues]
=====================================
key=[000000016299edc8:System.Configuration.ConfigurationValue]{  { "SourceInfo":0000000100004600 } { "Value":CollaborationAggregator_WebServiceURL } { "ValueFlags":0n1 } }
=====================================
value=[000000016299ee50:System.Configuration.ConfigurationValue]{  { "SourceInfo":0000000100004720 } { "Value":https://portal.contoso.com/_layouts/collaboration/ws/webservice.asmx } { "ValueFlags":0n1 } }

`

<a link id='wcookie'></a>
*!wcookie - Dump all cookies for all Http context objects, a single context or matching a cookie filter criteria*
`
Usage: !wcookie [-summary] [-name <partial-name>] [-value <partial-value>]
                       [-min <expr>] <expr>
Where:
	-summary Shows only the summary (count by key=value). Optional
	-min <expr> Only shows when the count of cookies >= <expr> (e.g. -min 2). Optional
	-name <partial-name> ;Dump only cookies with this name (e.g -name FedAuth*). Optional
	-value <partial-value> Dump only if value match (e.g. -value a4ghj8abcd*). Optional
	<expr> is the HttpContext object address (you can use an expression). Optional. List all if not specified

Examples:
-------------

Listing Session Cookies that repeat
--------------------------------------------
0:000> !wcookie -summary -name ASP.NET_* -min 2
Action Total Finished Cookie=Value
======================================================================================
(list)     3         2 ASP.NET_SessionId=0qtahu45hokrcs45052cn0a5
(list)     6         5 ASP.NET_SessionId=1ezzsuipgbqkywrz3wr0mo55
(list)    13         9 ASP.NET_SessionId=x3zmuqi21zohzz55gyefx4nv

(Total is the number total of requests and Finished is the number of finished requests)

Listing requests with same cookie value
--------------------------------------------
0:000> !wcookie -summary -name ASP.NET_* -min 2
Action Total Finished Cookie=Value
======================================================================================
(list)     3         2 ASP.NET_SessionId=0qtahu45hokrcs45052cn0a5

0:000> !wcookie -name ASP.NET_SessionId -value 0qtahu45hokrcs45052cn0a5
00000000e341db08 https://portal.contoso.com:443/GMC/RC/Documents/Constoso.pdf (200 NULL) Finished
======================================================================================
ASP.NET_SessionId=0qtahu45hokrcs45052cn0a5

1 printed

======================================================================================
00000001206530a0 https://portal.contoso.com:443/GMC/RC/Webmasters/Pages/default.aspx (200 NULL) Finished
======================================================================================
ASP.NET_SessionId=0qtahu45hokrcs45052cn0a5

1 printed

======================================================================================
00000001229a82c8 https://portal.contoso.com:443/audit/countries/br/Pages/MUSIC.aspx (200 NULL) Running (00:01:02)
======================================================================================
ASP.NET_SessionId=0qtahu45hokrcs45052cn0a5

1 printed

======================================================================================

`

<a link id='wruntime'></a>
*!wruntime - Dump all active Http Runtime information*
`
This is equivalent to this command:
!wfrom -nospace -nofield -type System.Web.HttpRuntime where ((!_beforeFirstRequest) || _shutdownReason) "\n=========================================================================\n","Address         : ",$addr(),"\nFirst Request   : ",$tickstodatetime(_firstRequestStartTime.dateData),"\nApp Pool User   : ",_wpUserId,"\nTrust Level     : ",_trustLevel,"\nApp Domnain Id  : ",_appDomainId,"\nDebug Enabled   : ",$if(_debuggingEnabled,"True (Not recommended in production)","False"),"\nActive Requests : ",_activeRequestCount,"\nPath            : ",_appDomainAppPath,$if(_isOnUNCShare," (in a share)"," (local disk)"),"\nTemp Folder     : ",_tempDir,"\nCompiling Folder: ",_codegenDir,"\nShutdown Reason : ",$if(_shutdownReason,$enumname(_shutdownReason)+" at "+$tickstodatetime(_lastShutdownAttemptTime.dateData),"Not shutting down"),"\n\n",$if(_shutdownReason,_shutDownMessage+"n"+_shutDownStack,"")


Syntax:
---------
!wruntime
 
Examples:
---------

Dumps all Active Http Runtime
--------------------------------
0:00>!wruntime

=========================================================================
Address         : 000000011F7244F0
First Request   : 7/23/2014 5:13:36 PM
App Pool User   : US\us-go-svcedcApp
Trust Level     : Full
App Domnain Id  : /LM/W3SVC/8989/ROOT/Services/2013v3-1-130506092168812411
Debug Enabled   : True (Not recommended in production)
Active Requests : 0n0
Path            : G:\Internet\wwwroot\contoso\Services\ (local disk)
Temp Folder     : C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files
Compiling Folder: C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files\contoso_Services\998f0bec\b158a7c7
Shutdown Reason : HostingEnvironment at 7/23/2014 5:25:41 PM

Directory rename change notification for 'G:\Internet\wwwroot\contoso\Services\'.
Services dir change or directory rename
HostingEnvironment initiated shutdown
HostingEnvironment caused shutdownn   at System.Environment.GetStackTrace(Exception e, Boolean needFileInfo)
   at System.Environment.get_StackTrace()
   at System.Web.Hosting.HostingEnvironment.InitiateShutdownInternal()
   at System.Web.HttpRuntime.ShutdownAppDomain(String stackTrace)
   at System.Web.HttpRuntime.OnCriticalDirectoryChange(Object sender, FileChangeEvent e)
   at System.Web.FileChangesMonitor.OnSubdirChange(Object sender, FileChangeEvent e)
   at System.Web.DirectoryMonitor.FireNotifications()
   at System.Web.Util.WorkItem.CallCallbackWithAssert(WorkItemCallback callback)
   at System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean ignoreSyncCtx)
   at System.Threading.QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem()
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading._ThreadPoolWaitCallback.PerformWaitCallback()


`

<a link id='wtoken'></a>
*!wtoken - Display WIF tokens and claims*
`
Dump all claims for a WIF Tokens in memory or if an HttpContext is specified it lists all claims for that request

Usage: !wcookie [<expr>]
Where:
	<expr> is the HttpContext object address (you can use an expression). Optional. List all if not specified

Examples:
-------------

Listing all claims in a SharePoint request using claims authentication
-----------------------------------------------------------------------------
0:000>  !wtoken 0000000183ce6da8
HttpContext    :  0000000183ce6da8 http://sp.contoso.com:80/Lists/Announcements/DispForm.aspx?ID=1

000000013f536688 Microsoft.IdentityModel.Tokens.SessionSecurityToken

Authentication Type: Federation
Name Claim Type    : http://schemas.microsoft.com/sharepoint/2009/08/claims/userid
Role Claim Type    : http://schemas.microsoft.com/ws/2008/06/identity/claims/role
Bootstrap Token    : 00000000ff5b0a88

Claims
================================
Type           : http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
Issuer         : SharePoint
Original Issuer: SharePoint
Value          : contoso\administrator
============================================================================================================
Type           : http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid
Issuer         : SharePoint
Original Issuer: Windows
Value          : S-1-5-21-1385174992-979951090-295046656-500
============================================================================================================
Type           : http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid
Issuer         : SharePoint
Original Issuer: Windows
Value          : S-1-5-21-1385174992-979951090-295046656-513
============================================================================================================
Type           : http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn
Issuer         : SharePoint
Original Issuer: Windows
Value          : Administrator@contoso.com
============================================================================================================
Type           : http://schemas.microsoft.com/sharepoint/2009/08/claims/userlogonname
Issuer         : SharePoint
Original Issuer: Windows
Value          : CONTOSO\Administrator
============================================================================================================
Type           : http://schemas.microsoft.com/sharepoint/2009/08/claims/userid
Issuer         : SharePoint
Original Issuer: SecurityTokenService
Value          : 0#.w|contoso\administrator
============================================================================================================
(...)

`

<a link id='wdo'></a>
*!wdo - Dump an object or array*
`!wdo [/forcearray] [/shownull] [/noheader] [/noindex] [/mt <expr>]
     [/start <expr>] [/end <expr>] <expr>
  /mt <expr> - mt,Method table for value objects (space-delimited)
  <expr> - Address,Object Address
  /start <expr> - Starting index to show in an array (space-delimited)
  /end <expr> - Ending index to show in an array (space-delimited)
  /forcearray - For Byte[] and Char[] arrays show items not the string
  /shownull - For arrays will show items that are null
  /noheader - Display only object address and field and values
  /noindex - For arrays it will show values without the index
  /tokens - show class and type tokens for fields

Examples:

List a structure (value object)
--------------------------------
!wdo -mt 000007feefc47fb8 00000001957021c0
0:000> !wdo -mt 000007feefc0a380 000000015585ec70
Address: 000000015585ec70
EEClass: 000007feef80f1f8
Method Table: 000007feefc0a380
(...)
Assembly name: C:\Windows\assembly\GAC_64\mscorlib\2.0.0.0__b77a5c561934e089\mscorlib.dll
Inherits: System.ValueType System.RuntimeTypeHandle System.Object (000007FEEFC07470 000007FEEFC0A380 000007FEEFC07370)
400052c 000007feefc0a488        2000108 System.IntPtr +0000                m_ptr 7ff002126c0 (0n8791800227520)
400052b 000007feefc0a380 Static 2000108 System.RuntimeTypeHandle+0238 EmptyHandle NULL

List an array
--------------
0:000> !wdo 0x00000001956fc648
Address: 00000001956fc648
EEClass: 000007feef8b14b8
Method Table: 000007feefc0f5a0
Object Type: 3
Class Name: System.Collections.Hashtable+bucket[]
Size : 288
Rank: 1
Components: 11
[0]: 00000001956fc658
[1]: 00000001956fc670
[2]: 00000001956fc688
(...)

List a single object 
---------------------
0:000> !wdo -tokens 00000001956fc748
Address: 00000001956fc748
EEClass: 000007feef8b15f0
Method Table: 000007feefc0f708
Class Name: System.Collections.Hashtable+bucket
(...)
Assembly name: C:\Windows\assembly\GAC_64\mscorlib\2.0.0.0__b77a5c561934e089\mscorlib.dll
Inherits: System.ValueType System.Object System.Collections.Hashtable+bucket (000007FEEFC07470 000007FEEFC07370 000007FEEFC0F708)
4000999 000007feefc07370 2000266 System.Object +0000          key 00000001956FA518
400099a 000007feefc07370 2000266 System.Object +0008          val 00000001956FDDE8
400099b 000007feefc0ecf0 2000266 System.Int32 +0010           hash_coll 6bf311c4 (0n1811091908)

`
<a link id='wselect'></a>
*!wselect - select specific fields from an object or array (accepts wildcard ? and {"*"})*
`
!wselect [mt <expr>] <field1>, ..., <fieldN> from <obj-expr> | <array-expr>
<field1>, ..., <fieldN> List of fields to display (accepts * and ?)
                        Fields can include subfields in the format field1.subfield1.subsubfield1
mt <expr> (note it is not -mt or /mt) Method table it it is a value object/struct
<obj-expr> | <array-expr>  Address of an object or array

Note: !wselect unlike !wfrom DOES NOT accept contitionals and expressions

Examples:

List all fields of an object
----------------------------
0:000> !wselect * from 000000019568bd48
System.Object m_objref = 0000000000000000
(int32)System.Int32 m_data1 = 0 (0n0)
(int32)System.Int32 m_data2 = 0 (0n0)
(int32)System.Int32 m_flags = 0 (0n0)
static System.Type _voidPtr = 0000000000000000
static System.Object[] ClassTypes = 000000019568BDC0
static System.Variant Empty = -mt 000007FEEFBF6DD8 00000001D5660300
(...)


List url and response from a HttpContext
----------------------------------------
0:000> !wselect _request._url.m_String, _response._statusCode from 0000000195702098
System.String _request._url.m_String = 00000001559cb3a8 http://rviana-serv.contoso.com:80/TestClass/Service.svc/net
(int32)System.Int32 _response._statusCode = c8 (0n200)

List all urls for all HttpRequest types in heap (using !windex and .foreach)
-----------------------------------------------------------------------------
.foreach({$addr} {!windex -short -type *.HttpRequest}){!wselect _url.m_String from {$addr} }

Listing selected fields from an array of endpoints
---------------------------------------------------
0:000> !wselect id, address.uri.m_String from 000000015595faa0
***************
[0]: 000000015595f9a8
System.String id = 0000000155984ad8 1b5d922e-7a3e-4e00-8289-24260fc3dfaf
System.String address.uri.m_String = 000000015595dfd8 http://rviana-serv.contoso.com/TestClass/Service.svc/net
***************
[1]: 0000000155965390
System.String id = 000000015598c578 b6a9bc45-c2d2-4d16-b329-18f1406805b7
System.String address.uri.m_String = 0000000155963cb0 http://rviana-serv.contoso.com/TestClass/Service.svc/jar
***************
[2]: 0000000155968838
System.String id = 0000000155994270 5a3819bb-4a1d-444f-8ee7-244186704fbb
System.String address.uri.m_String = 0000000155967648 http://rviana-serv.contoso.com/TestClass/Service.svc/mex


Hint: You can get to the array of endpoints from a System.ServiceModel.Description.ServiceDescription using this
------------------------------------------------------------------------------------------------------------------
0:000> !wselect endpoints.items._items from 000000015585f5c8
System.Object[] endpoints.items._items = 000000015595FAA0

`
<a link id='windex'></a>
*!windex - Index and dump managed heap types enabling tree view and save index in disk (useful for large dumps)*
`
!windex [/quiet] [/enumtypes] [/tree] [/flush] [/short] [/ignorestate]
        [/withpointer] [/type <string>] [/fieldname <string>]
        [/fieldtype <string>] [/implement <string>] [/save <string>]
        [/load <string>] [/mt <string>]
  /quiet - Do not display index progress
  /enumtypes - Display types with link to objects
  /tree - Show types tree on a different window (WinDBG only)
  /flush - Fush index
  /short - Display addresses only
  /ignorestate - Ignore state when loading index
  /type <string> - type;List of types to include wildcards accepted (eg. -type
                   *HttRequest,system.servicemodel.*)
  /fieldname <string> - fieldname;List of field names that the type must
                        contain (eg. -fieldname *request,_wr)
  /fieldtype <string> - fieldtype;List of field types that the type must
                        contain (eg. -fieldtype *.String,*.object)
  /implement <string> - implement;List of parent types that the type must
                        implement (eg. -implement *.Exception,*.Array)
  /withpointer - List all types which include pointer fields
  /save <string> - Save index to file
  /load <string> - Load previously saved index file
  /mt <string> - List of Method Tables to include (eg. -mt 7012ab8,70ac080)

Examples:

Index managed heap in memory and show a list of all types found and the number of objects per type
---------------------------------------------------------------------------------------------------
0:000> !windex -enumtypes
Starting indexing at 17:20:25 PM
Indexing finished at 17:20:28 PM
4,715,107 Bytes in 55,010 Objects
Index took 00:00:02
000007ff00198b28 ASP.global_asax (2)
000007ff00212a00 CompositeType (1)
000007ff00199340 CustomHandle.RemoveBasic (2)
00000000013d4d40 Free (36)
000007ff00105020 Microsoft.VisualStudio.Diagnostics.ServiceModelSink.Behavior (2)
000007ff001055d8 Microsoft.VisualStudio.Diagnostics.ServiceModelSink.StubServerEventSink (3)
000007fee15ddd90 Microsoft.Web.Administration.ConfigurationAttribute (1)
000007fee15dd948 Microsoft.Web.Administration.ConfigurationElement (17)
000007fee15dde18 Microsoft.Web.Administration.ConfigurationElementCollection (5)
000007fee15df230 Microsoft.Web.Administration.ConfigurationSection (8)
(...)
Heap contains 55014 Objects in 1522 types

Creates a tree file with all types that can be viewed in WinDBG as command tree
--------------------------------------------------------------------------------
(you WILL have to copy and paste the command generated)
0:000> !windex -tree
Index is up to date
 If you believe it is not, use !windex -flush to force reindex
Copy, paste and run command below:
.cmdtree C:\Users\rviana\AppData\Local\Temp\HEA4DF9.tmp

Saves index to disk (very useful for reuse in SharePoint dumps)
---------------------------------------------------------------
0:000> !windex -save c:\temp\temp.idx
Index is up to date
(...)
Hash: 5be3fd81
Index saved succesfully on c:\temp\temp.idx

Load a previously saved index file (life safer for revisiting a large dump file)
---------------------------------------------------------------------------------
0:000> !windex -load c:\temp\temp.idx
Index file c:\temp\temp.idx was loaded

List all httpcontext objects using the index
0:000> !windex -type *.httpcontext
Index is up to date
00000001559dc070 000007feda232488 System.Web.HttpContext      336   0 0
00000001559e1fb0 000007feda232488 System.Web.HttpContext      336   0 0
(...)

List all objects derived from System.Exception
0:000> !windex -implement *.Exception
Index is up to date
 If you believe it is not, use !windex -flush to force reindex
0000000155829fb0 000007fee97c1348 System.Configuration.ConfigurationErrorsException      168   0 0
(...)
00000001556601c0 000007feefc08078 System.ExecutionEngineException      136   0 0
00000001556600b0 000007feefc07e58 System.OutOfMemoryException      136   0 0
0000000155660138 000007feefc07f68 System.StackOverflowException      136   0 0
0000000155660248 000007feefc08188 System.Threading.ThreadAbortException      136   0 0
00000001556602d0 000007feefc08188 System.Threading.ThreadAbortException      136   0 0


`
<a link id='wstack'></a>
*!wstack - Dump objects in the current thread*
`
!wstack takes no parameter
Due to the nature of the stack objects this approach is not deterministic.

Examples:

Dumps the current stack
------------------------
0:003> !wstack
Listing objects from: 0000000001069000 to 0000000001070000 from thread: 3 [22f4]
00000001556da9a8 000007feda22c8a8 System.Web.Hosting.HostingEnvironment 0 0
00000001559dd078 000007feefc08520 System.Threading.Thread 0 0
(...)

Dump stack of all threads
-------------------------
0:003> ~*e!wstack
Listing objects from: 0000000000284000 to 0000000000290000 from thread: 0 [21a0]
Listing objects from: 000000000088e000 to 0000000000890000 from thread: 1 [263c]
Listing objects from: 0000000000cfe000 to 0000000000d00000 from thread: 2 [2d0c]
Listing objects from: 0000000001069000 to 0000000001070000 from thread: 3 [22f4]
00000001556da9a8 000007feda22c8a8 System.Web.Hosting.HostingEnvironment 0 0
00000001559dd078 000007feefc08520 System.Threading.Thread 0 0
00000001559dd128 000007feefbf62e8 System.Threading.ThreadPoolRequestQueue 0 0
(...)

`
<a link id='wdict'></a>
*!wdict - Dump items in a dictionary type*
`
!wdict<address>
<address> - Address of the dictionary.

Examples:

Dumps a dictionary
------------------------
0:000> !wdict 00000001557d39f0
Items   : 1
[0]:==============================================
System.__Canon key = 00000001557d2a70 CompilerVersion
System.__Canon value = 00000001557d2aa8 v2.0

`
<a link id='whash'></a>
*!whash - Dump items in a hash table*
`
!whash <address>
<address> - Address of the hash table.

Examples:

Dumps a hashtable
------------------------
0:000> !whash 000000015568d0d0
Buckets : 000000015568d128

[0]:=========================================
System.Object key = 000000015568c658 utf-8
System.Object val = 000000015568d3d8

`
<a link id='wgchandles'></a>
*!wgchandles - List all rooted objects (handles) in heap*
`
!wgchandles

Example:

0:000> !wgchandles
GCHandles
    0: 00000001557aafc8 poi(0000000000b81000)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    1: 00000001557aa8e0 poi(0000000000b81008)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    2: 00000001557aa1d8 poi(0000000000b81010)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    3: 000000015579cf10 poi(0000000000b81018)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    4: 00000001557875e0 poi(0000000000b81020)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    5: 0000000155781330 poi(0000000000b81028)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    6: 0000000155780ae8 poi(0000000000b81030)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    7: 000000015577f9e0 poi(0000000000b81038)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    8: 000000015577ef08 poi(0000000000b81040)      144 System.RuntimeType+RuntimeTypeCache - Local Var
    9: 000000015577e058 poi(0000000000b81048)      144 System.RuntimeType+RuntimeTypeCache - Local Var
   10: 000000015577d700 poi(0000000000b81050)      144 System.RuntimeType+RuntimeTypeCache - Local Var
(...)
  435: 00000001957082a0 poi(00000000010815e0)       64 System.Net.Logging+NclTraceSource - Static Var
  436: 0000000195708218 poi(00000000010815e8)       64 System.Net.Logging+NclTraceSource - Static Var
  437: 00000001956fe830 poi(00000000010815f0)       72 System.Diagnostics.SourceSwitch - Static Var
  438: 00000001956f98a0 poi(00000000010815f8)       80 System.ServiceModel.Diagnostics.DiagnosticTraceSource - Static Var
  439: 0000000195700708 poi(00000000010817f8)      120 System.Threading.OverlappedData - Invalid


`
<a link id='whttp'></a>
*!whttp - Dump HttpContext objects*
`
!whttp [/order] [/running] [/withthread] [/status <decimal>] [/notstatus <decimal>] [/verb <string>] [<expr>] 
-------
 /order - If specified will show requests in chronological order and include the time stamp
 /running - If specified will show requests still running
 /withthread - If specified will show only requests with valid thread (it superseeds -running)
 /status - If specified will show only requests with the chosen status (the value is decimal not hex like 500)
 /notstatus - If specified will show only requests without the chosen status (the value is decimal not hex like 200)
 /verb - If specified will show only requests with the chosen verb (e.g. POST)
  <expr> - HttpContext Address. Optional. If not specified list all HttpContext objects

Examples:

List all HttpContext objects
----------------

0:000> !whttp
HttpContext    Thread Time Out Running  Status Verb     Url
0000000184346f98   53 00:01:50 00:00:41    200 GET      http://sp:80/Lists/Team Discussion/DispForm.aspx?ID=1
0000000184b4f248   -- 00:01:50 Finished    401 POST     http://sp.contoso.com:80/_vti_bin/Lists.asmx
0000000184d512b8   -- 00:01:50 Finished    401 GET      http://sp:80/Lists/Team Discussion/AllItems.aspx
0000000184d58f48   49 00:01:50 00:00:41    200 POST     http://sp.contoso.com:80/_vti_bin/Lists.asmx
0000000184e52280   55 00:01:50 00:00:41    200 GET      http://sp:80/Lists/Team Discussion/AllItems.aspx
0000000184ebf228   47 00:01:50 00:00:41    200 POST     http://sp.contoso.com:80/_vti_bin/Lists.asmx
00000001c0d186e0   -- 00:01:50 Finished    200 GET      http://sp.contoso.com:80/Lists/Announcements/DispForm.aspx?ID=1

List requests still running
----------------------------
    0:000> !whttp -running
	HttpContext    Thread Time Out Running  Status Verb     Url
	00000001027b9c40   52 00:01:50 00:00:41    200 GET      http://sp.contoso.com:80/_vti_bin/SiteData.asmx?disco
	0000000103f94698   -- Not set  00:00:36    200 POST     /_vti_bin/Lists.asmx
	00000001045f2200   58 00:01:50 00:00:41    200 GET      http://sp.contoso.com:80/FormServerTemplates/Forms/All Forms.aspx

List all failed requests
----------------------------
    0:000> !whttp -notstatus 200
	00000001030436d0   -- 00:01:50 Finished    401 GET      http://sp.contoso.com:80/Lists/Links/AllItems.aspx
	00000001045be510   -- 00:01:50 Finished    401 GET      http://sp.contoso.com:80/Style Library/Forms/AllItems.aspx

List requests with valid thread in order of requests
----------------------------------------------------
    0:000> !whttp -withthread -order
	HttpContext      Start Time                    Thread Time Out Running  Status Verb     Url
	0000000160450808 2/9/2011 3:39:30 PM            166 00:01:50 00:02:08    200 GET      https://portal.contoso.com:443/europe/Pages/CountryHomePage.aspx?Country=Germany&Language=German
	00000000e01dcfe8 2/9/2011 3:39:44 PM            177 00:01:50 00:01:54    200 GET      https://portal.contoso.com:443/europe/HR/hronly/Pages/TPromotions.aspx
	0000000100672238 2/9/2011 3:39:47 PM            169 00:01:50 00:01:51    200 GET      https://portal.contoso.com:443/markets/europe/mcci/crm/Pages/policiesandreferences.aspx
	00000000e0a49ed0 2/9/2011 3:39:48 PM            163 00:01:50 00:01:50    200 GET      https://portal.contoso.com:443/markets/europe/regions/germany/Pages/Marketing.aspx
	00000001209f0fc0 2/9/2011 3:39:51 PM            164 00:01:50 00:01:47    200 GET      https://portal.contoso.com:443/europe/Pages/ELLPCountryHomePage.aspx?Country=Germany&Language=German
	00000000c0c16608 2/9/2011 3:39:51 PM             71 00:01:50 00:01:47    200 GET      https://portal.contoso.com:443/Advisory/transactionsrestructuring/Pages/Homepage.aspx


List details of a HttpContext:
---------------------------------

0:067> !whttp 00000001c30d2878

Context Info
================================
Address           : 00000001c30d2878
Target/Dump Time  : 1/17/2013 10:45:51 PM
Request Time      : 1/17/2013 10:45:09 PM
Running time      : 00:00:41
Timeout           : 00:01:50
Timeout Start Time: 1/17/2013 10:45:09 PM
Timeout Limit Time: 1/17/2013 10:46:59 PM
Managed Thread Id : 9e8
Managed Thread Id : a
HttpContext.Items: 00000001c3109ae8

Request Info
================================
POST http://sp.contoso.com:80/_vti_bin/SiteData.asmx 
Content Type    : text/xml; charset=utf-8
Content Length  : 453
Target in Server: C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14\isapi\SiteData.asmx
Body:
[--- Start ---]
<?xml version="1.0" encoding="utf-8"?><soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><soap:Body><GetContent xmlns="http://schemas.microsoft.com/sharepoint/soap/"><objectType>Site</objectType><retrieveChildItems>true</retrieveChildItems><securityOnly>false</securityOnly><lastItemIdOnPage /></GetContent></soap:Body></soap:Envelope>
[---  End ---]

Response Info
================================
Warning: Response has not completed
Status          : 200 (NULL)

Server Variables
================================
ALL_HTTP: HTTP_CONTENT_LENGTH:453
HTTP_CONTENT_TYPE:text/xml; charset=utf-8
HTTP_AUTHORIZATION:NTLM TlRMTVNTUAADAAAAAAAAAFgAAAAAAAAAWAAAAAAAAABYAAAAAAAAAFgAAAAAAAAAWAAAAAAAAABYAAAANcKI4gYBsR0AAAAPtTVV10HFXv+LUOL9T1/wvQ==
HTTP_EXPECT:100-continue
HTTP_HOST:sp.contoso.com
HTTP_USER_AGENT:Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 2.0.50727.5448)
HTTP_SOAPACTION:"http://schemas.microsoft.com/sharepoint/soap/GetContent"

ALL_RAW: Content-Length: 453
Content-Type: text/xml; charset=utf-8
Authorization: NTLM TlRMTVNTUAADAAAAAAAAAFgAAAAAAAAAWAAAAAAAAABYAAAAAAAAAFgAAAAAAAAAWAAAAAAAAABYAAAANcKI4gYBsR0AAAAPtTVV10HFXv+LUOL9T1/wvQ==
Expect: 100-continue
Host: sp.contoso.com
User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 2.0.50727.5448)
SOAPAction: "http://schemas.microsoft.com/sharepoint/soap/GetContent"

APPL_MD_PATH: /LM/W3SVC/1067026433/ROOT
APPL_PHYSICAL_PATH: C:\inetpub\wwwroot\wss\VirtualDirectories\80\
LOGON_USER: CONTOSO\Administrator
CONTENT_LENGTH: 453
CONTENT_TYPE: text/xml; charset=utf-8
GATEWAY_INTERFACE: CGI/1.1
HTTPS: off
INSTANCE_ID: 1067026433
INSTANCE_META_PATH: /LM/W3SVC/1067026433
LOCAL_ADDR: 10.10.82.152
REMOTE_ADDR: 10.10.82.152
REMOTE_HOST: 10.10.82.152
REMOTE_PORT: 51593
REQUEST_METHOD: POST
SERVER_NAME: sp.contoso.com
SERVER_PORT: 80
SERVER_PORT_SECURE: 0
SERVER_PROTOCOL: HTTP/1.1
SERVER_SOFTWARE: Microsoft-IIS/7.5
HTTP_CONTENT_LENGTH: 453
HTTP_CONTENT_TYPE: text/xml; charset=utf-8
HTTP_AUTHORIZATION: NTLM TlRMTVNTUAADAAAAAAAAAFgAAAAAAAAAWAAAAAAAAABYAAAAAAAAAFgAAAAAAAAAWAAAAAAAAABYAAAANcKI4gYBsR0AAAAPtTVV10HFXv+LUOL9T1/wvQ==
HTTP_EXPECT: 100-continue
HTTP_HOST: sp.contoso.com
HTTP_USER_AGENT: Mozilla/4.0 (compatible; MSIE 6.0; MS Web Services Client Protocol 2.0.50727.5448)
HTTP_SOAPACTION: "http://schemas.microsoft.com/sharepoint/soap/GetContent"

You may also be interested in
================================
Dump HttpContext fields: !wselect * from 00000001c30d2878
Find all stack roots   : !wfrom -obj 00000001c30d2878 select $a("Count  ", $if($strsize($stackroot($addr())),$splitsize($stackroot($addr()),","),0)), $a("Threads",$stackroot($addr()))
Xml Formatted Request  : !wfrom -obj 00000001c30d2878 select $xml($rawfield(_request._rawContent._data))
Xml Tree of Request    : !wfrom -obj 00000001c30d2878 select $xmltree($rawfield(_request._rawContent._data))


`

<a link id='wservice'></a>
*!wservice - Dump all WCF Services or details about a specific service (System.ServiceModel.ServiceHost)*
`
!wservice [<expr>]

<address> - Address of the hash table.
<expr> - ServiceHost Address. Optional. If not specified dump all services.


Examples:


Enumerate all WCF services
-----------------------------

	0:000> !wservice
	Address		State        EndPoints BaseAddresses  Behaviors Throttled   Calls/Max   Sessions/Max    ConfigName,.NET Type
	000000015585ec80	Opened		0n3		0n2		0n7	True  	0n0/0n16	0n0/0n10	"Service",Service
	(...)

Detail a service:
-----------------------

	0:000> !wservice 000000015585ec80
	Service Info
	================================
	Address            : 000000015585EC80
	Configuration Name : Service
	State              : Opened
	EndPoints          : 0n3
	Base Addresses     : 0n2
	Behaviors          : 0n7
	Runtime Type       : Service
	Is Throttled?      : True
	Calls/Max Calls    : 0n0/0n16
	Sessions/Max       : 0n0/0n10
	Events Raised      : No Event raised
	Handles Called     : OnOpeningHandle OnOpenedHandle 
	Session Mode       : False

	Extensions         : 0000000155968980

	Service Base Addresses
	================================
	http://rviana-serv.contoso.com/TestClass/Service.svc
	https://rviana-serv.contoso.com/TestClass/Service.svc

	Channels
	================================
	Address            : 0000000155984430
	Listener URI       : http://rviana-serv.contoso.com/TestClass/Service.svc/net
	Binding Name       : http://tempuri.org/:wsHttpBindingConfiguration
	Aborted            : No
	State              : Opened
	Transaction Type   : No transaction
	Listener State     : Opened
	Timeout settings   : Open [00:01:00] Close [00:01:00] Receive: [00:10:00] Send: [00:01:00]
	Server Capabilities: SupportsServerAuth [Yes] SupportsClientAuth [Yes] SupportsClientWinIdent [Yes]
	Request Prot Level : None
	Response Prot Level: None
	Events Raised      : No Event raised
	Handles Called     : OnOpeningHandle OnOpenedHandle 
	Session Mode       : False

	Address            : 000000015598BF50
	Listener URI       : http://rviana-serv.contoso.com/TestClass/Service.svc/jar
	Binding Name       : http://tempuri.org/:basicHttpBindingConfigurationJava
	Aborted            : No
	State              : Opened
	Transaction Type   : No transaction
	Listener State     : Opened
	Timeout settings   : Open [00:01:00] Close [00:01:00] Receive: [00:10:00] Send: [00:01:00]
	Server Capabilities: SupportsServerAuth [No ] SupportsClientAuth [Yes] SupportsClientWinIdent [Yes]
	Request Prot Level : None
	Response Prot Level: None
	Events Raised      : No Event raised
	Handles Called     : OnOpeningHandle OnOpenedHandle 
	Session Mode       : False

	Address            : 0000000155993C48
	Listener URI       : http://rviana-serv.contoso.com/TestClass/Service.svc/mex
	Binding Name       : http://schemas.microsoft.com/ws/2005/02/mex/bindings:mexBinding
	Aborted            : No
	State              : Opened
	Transaction Type   : No transaction
	Listener State     : Opened
	Timeout settings   : Open [00:01:00] Close [00:01:00] Receive: [00:10:00] Send: [00:01:00]
	Server Capabilities: SupportsServerAuth [No ] SupportsClientAuth [No ] SupportsClientWinIdent [No ]
	Request Prot Level : None
	Response Prot Level: None
	Events Raised      : No Event raised
	Handles Called     : OnOpeningHandle OnOpenedHandle 
	Session Mode       : False

	Address            : 000000015599A400
	Listener URI       : http://rviana-serv.contoso.com/TestClass/Service.svc
	Binding Name       : ServiceMetadataBehaviorHttpGetBinding
	Aborted            : No
	State              : Opened
	Transaction Type   : No transaction
	Listener State     : Opened
	Timeout settings   : Open [00:01:00] Close [00:01:00] Receive: [00:10:00] Send: [00:01:00]
	Server Capabilities: SupportsServerAuth [No ] SupportsClientAuth [No ] SupportsClientWinIdent [No ]
	Request Prot Level : None
	Response Prot Level: None
	Events Raised      : No Event raised
	Handles Called     : OnOpeningHandle OnOpenedHandle 
	Session Mode       : False

	Address            : 00000001559A0570
	Listener URI       : https://rviana-serv.contoso.com/TestClass/Service.svc
	Binding Name       : ServiceMetadataBehaviorHttpGetBinding
	Aborted            : No
	State              : Opened
	Transaction Type   : No transaction
	Listener State     : Opened
	Timeout settings   : Open [00:01:00] Close [00:01:00] Receive: [00:10:00] Send: [00:01:00]
	Server Capabilities: SupportsServerAuth [Yes] SupportsClientAuth [No ] SupportsClientWinIdent [No ]
	Request Prot Level : EncryptAndSign
	Response Prot Level: EncryptAndSign
	Events Raised      : No Event raised
	Handles Called     : OnOpeningHandle OnOpenedHandle 
	Session Mode       : False


	Endpoints
	================================
	Address            : 000000015595F9A8
	URI                : http://rviana-serv.contoso.com/TestClass/Service.svc/net
	Is Anonymous       : False
	Configuration Name : IService
	Type Name          : IService
	Listening Mode     : Explicit
	Class Definition   : 000007ff00212620 IService
	Behaviors          : 000000015595fa58
	Binding            : 0000000155957a80

	Address            : 0000000155965390
	URI                : http://rviana-serv.contoso.com/TestClass/Service.svc/jar
	Is Anonymous       : False
	Configuration Name : IService
	Type Name          : IService
	Listening Mode     : Explicit
	Class Definition   : 000007ff00212620 IService
	Behaviors          : 000000015595fa58
	Binding            : 0000000155960740

	Address            : 0000000155968838
	URI                : http://rviana-serv.contoso.com/TestClass/Service.svc/mex
	Is Anonymous       : False
	Configuration Name : IMetadataExchange
	Type Name          : System.ServiceModel.Description.IMetadataExchange
	Listening Mode     : Explicit
	Class Definition   : 000007fed1792170 System.ServiceModel.Description.IMetadataExchange
	Behaviors          : 000000015595fa58
	Binding            : 0000000155965c98


`
<a link id='wclass'></a>
*!wclass - Dump the class defintion and let you set managed breakpoints *
`
!wclass <expr>
Where:
	<expr> is the Method Table of the class (you can use an expression).


Examples:

Dumps type System.Web.Configuration.HttpConfigurationSystem
------------------------------------------------------------
0:00> !wclass 00000001045f0c68
// Method Table: 00000001045f0c68
// Module Address: 000007feea930000
// Debugging Mode: IgnoreSymbolStoreSequencePoints
// Filename: C:\Windows\assembly\GAC_64\System.Web\2.0.0.0__b03f5f7f11d50a3a\System.Web.dll
namespace System.Web.Configuration {

 internal class HttpConfigurationSystem: System.Configuration.Internal.IInternalConfigSystem
 {
	//
	// Fields
	//


	//
	// Static Fields
	//

	private static System.Object s_initLock;
	private static System.Boolean s_inited;
	private static System.Web.Configuration.HttpConfigurationSystem s_httpConfigSystem;
	private static System.Configuration.Internal.IConfigSystem s_configSystem;
	private static System.Web.Configuration.IConfigMapPath s_configMapPath;
	private static System.Web.Configuration.WebConfigurationHost s_configHost;
(...)

	//
	// Properties
	//

	/* property * / IsSet
	{

		// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
		// Click for breakpoint: 000007feeb282870
		internal get  { } 
	}
	/* property * / MachineConfigurationDirectory
	{

		// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
		// Click for breakpoint: 000007feeab88740
		internal get  { } 
	}
	/* property * / MachineConfigurationFilePath
	{

		// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
		// Click for breakpoint: 000007feeab886d0
		internal get  { } 
	}
	/* property * / MsCorLibDirectory
	{

		// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
		// Click for breakpoint: 000007feeab887b0
		internal get  { } 
	}
	System.String RootWebConfigurationFilePath
	{

		// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
		// Click for breakpoint: 000007feeb282b40
		internal set  { } 

		// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
		// Click for breakpoint: 000007feeab8b060
		internal get  { } 
	}
(...)

	//
	// Methods
	//


	// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
	// Click for breakpoint: 000007fef2e4abe0
	public virtual ToString();

	// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
	// Click for breakpoint: 000007fef2e52560
	public Equals(System.Object);

	// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
	// Click for breakpoint: 000007fef2e4bc70
	private static GetHashCode();

	// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
	// Click for breakpoint: 000007fef2efe630
	public static Finalize();

	// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
	// Click for breakpoint: 000007feeab8fd10
	private virtual GetSection(System.String);

	// JIT MODE: None - THIS IS ONLY VALID FOR .NET 4.5 AND BEYOND
	// Click for breakpoint: 000007feeb282b20
	private virtual RefreshConfig(System.String);

(...)
 }
}


`
<a link id='wheap'></a>
*!wheap - Dump heap objects without indexing*
`
!wheap [/short] [/detailsonly] [/nothrottle] [/start <expr>] [/end <expr>]
       [/type <string>] [/mt <string>]
  /short - Dump object addresses only (for .foreach processing)
  /detailsonly - Show heap detail and areas (no object is shown)
  /nothrottle - No limit for objects dumped per area. Otherwise will
                show only first 500 per area if -type or -mt are not used
  /start <expr> start address
  /end <expr> - end address
  /type <string> - List of types to dump with wildcards accepted 
                   (eg. -type *HttRequest,system.servicemodel.*)
  /mt <string> - List of Method Tables to include (eg. -mt 7012ab8,70ac080)

- If you just use !windex it will only show the first 500 objects of each heap area
- If you use -nothrottle, -type or -mt all results will be shown
- Don't use !wheap if you plan to search heap later or use !wfrom (use !windex instead)

Examples:

Display heap details only
-------------------------
0:000> !wheap -detailsonly
Heaps: 2
Server Mode: 1
Heap [0]:
	Allocated: 00000001559e8fe8
	Card Table: 00000000024ba6b0
	Ephemeral Heap Segment: 0000000155660000
	Finalization Fill Pointers: 0000000004b7d348
	Heap Address: 00000000013803c0
	Lowest Address: 0000000155660000
	Highest Address: 00000001f5660000
	Generation Addresses:
		[0]:AllocStart(0000000155660098),AllocCxtLimit(0000000000000000),AllocCtxPtr(0000000000000000),StartSeg(0000000155660000)
		[1]:AllocStart(0000000155660080),AllocCxtLimit(0000000000000000),AllocCtxPtr(0000000000000000),StartSeg(0000000155660000)
		[2]:AllocStart(0000000155660068),AllocCxtLimit(0000000000000000),AllocCtxPtr(0000000000000000),StartSeg(0000000155660000)
		[3]:AllocStart(00000001d5660068),AllocCxtLimit(0000000000000000),AllocCtxPtr(0000000000000000),StartSeg(00000001d5660000)

Segments:
Segment: 0000000155660000 Start: 0000000155660068 End: 00000001556600b0 HiMark: 00000001559e8fe8 Next: 0000000000000000
Segment: 00000001d5660000 Start: 00000001d5660068 End: 00000001d56811e8 HiMark: 00000001d56811e8 Next: 0000000000000000
(...)

List only objects of type ServiceHost and HttpContext
-----------------------------------------------------

0:000> !wheap -type *.servicehost,*.httpcontext
(...)
000000015585ec80 000007fed1842608      248   0 0 System.ServiceModel.ServiceHost
00000001559dc070 000007feda232488      336   0 0 System.Web.HttpContext
00000001559e1fb0 000007feda232488      336   0 0 System.Web.HttpContext
00000001956905c0 000007feda232488      336   1 0 System.Web.HttpContext
00000001956e5360 000007feda232488      336   1 0 System.Web.HttpContext
0000000195702098 000007feda232488      336   1 0 System.Web.HttpContext

List only object addresses for a particular type (to use with .foreach)
------------------------------------------------
0:000> !wheap -short -mt 000007feda232488 
00000001559dc070
00000001559e1fb0
(...)

`
<a link id='wfrom'></a>
*!wfrom - Print objects based on a condition (where) and evaluate operations on fields*
`
!wfrom [/nofield] [/withpointer] [/type <string>]
       [/mt <string>] [/fieldname <string>] [/fieldtype <string>]
       [/implement <string>] [/obj <expr>] 
       [where (<condition>)] select <expr1>, ..., <exprN>

  /nofield - Do not include field name, just the value
  /nospace - Print without any space/new line between fields (use with /nofield)
  /type <string> - List of types to include wildcards accepted (eg. -type
                   *HttRequest,system.servicemodel.*)
  /mt <string> - mt;List of Method Tables to include (eg. -mt 7012ab8,70ac080)
  /fieldname <string> - List of field names that the type must
                        contain (eg. -fieldname *request,_wr)
  /fieldtype <string> - List of field types that the type must
                        contain (eg. -fieldtype *.String,*.object)
  /implement <string> - List of parent types that the type must
                        implement (eg. -implement *.Exception,*.Array)
  /withpointer - List all types which include pointer fields
  /obj <expr> - object address

  where (<condition>) - Condition than must evaluate to a true (non-zero) value
                        if it is evaluated as false, nothing is printed
                        eg.: ( (field1==0n194) || (field2=="rodney") ). Optional.
  select <expr1>, ..., <exprN> - List of expressions to be evaluated. 
                        Wildcard fields are not accepted.
                        eg.: select $sqrt(2+field1*(3+2))+field2.subfield1,
                        "Name: "+field2, $addr()

select must be present in the query. Use parenthesis with where, eg. where (field1==1).
You MUST run !windex before using !wfrom. where is optional.

Examples:

This example lists all failed requests with the object address
---------------------------------------------------------------
0:000> !wfrom -type *.HttpContext where ( _response._statuscode != 0n200 ) select $addr(), _request._url.m_String, _response._statusCode
calculated: 0n5731369072
_request._url.m_String: http://rviana-serv.contoso.com:80/TestClass/Service.svc/net
_response._statusCode: 0n401
calculated: 0n6802002784
_request._url.m_String: http://rviana-serv.contoso.com:80/TestClass/Service.svc/net
_response._statusCode: 0n401

Lists all strings containing system.web
---------------------------------------
0:000> !wfrom -type System.String where ( $contains($string(),"system.web") ) select $addr(), $string()
calculated: 0n5728819448
calculated: system.web/authorization
calculated: 0n5728821480
calculated: <system.webServer>
      <security>
        <authentication>
          <anonymousAuthentication enabled="false" />
          <basicAuthentication enabled="false" />
          <windowsAuthentication enabled="true" />
        </authentication>
      </security>
    </system.webServer>
calculated: 0n5728857592
calculated: system.web
(...)

Find all XML-like string
------------------------
0:000> !wfrom -type System.String where ( $wildcardmatch($string(),"*<*>*</*>*") ) select $addr(), $string()
calculated: 0n5728404240
calculated: <configProtectedData defaultProvider="RsaProtectedConfigurationProvider">
		<providers>
			<add name="RsaProtectedConfigurationProvider" type="System.Configuration.RsaProtectedConfigurationProvider,System.Configuration, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" description="Uses RsaCryptoServiceProvider to encrypt and decrypt" keyContainerName="NetFrameworkConfigurationKey" cspProviderName="" useMachineContainer="true" useOAEP="false"/>(...)
	</connectionStrings>
(...)

You can follow up with psscor's GCRef to find the owner of the string:
----------------------------------------------------------------------
0:000> !GCRef 0n5728404240
Object found: 0x0000000155708310
Parent List: 
0x0000000155708df0 
0:000> !wdo 0x0000000155708df0
Address: 0000000155708df0
EEClass: 000007fee976d538
Method Table: 000007fee97c5238
Object Type: 4
Class Name: System.Configuration.SectionXmlInfo
Size : 120
Assembly name: C:\Windows\assembly\GAC_MSIL\System.Configuration\2.0.0.0__b03f5f7f11d50a3a\System.Configuration.dll
Inherits: System.Object System.Configuration.SectionXmlInfo (000007FEEFC07370 000007FEE97C5238)
40003ca 07feefc07a80 2000099  System.String +0000             _configKey 00000001556f73f8 configProtectedData
40003cb 07feefc07a80 2000099  System.String +0008  _definitionConfigPath 00000001556eec90 machine
(...)
40003d5 07feefc07a80 2000099  System.String +0048                _rawXml 0000000155708310
<configProtectedData defaultProvider="RsaProtectedConfigurationProvider">
		<providers>
			<add name="RsaProtectedConfigurationProvider" 
(...)
		</providers>
	</configProtectedData>
(...)

This example print summary all HttpContext objects in memory
------------------------------------------------------------
0:000> !wfrom -nospace -nofield -type System.Web.HttpContext $addr(),"\t",$if((_timeoutState==1),"Yes", "No"), "\t", $if(_request._completed,"Yes      ", "Running"), "\t", _response._statusCode, "\t", $isnull(_request._httpMethod,"NA"), "\t", $isnull(_request._url.m_String,"Not set")
00000001559DC070	No	Running	0n401	POST	http://rviana-serv.contoso.com:80/TestClass/Service.svc/net
00000001559E1FB0	No	Running	0n200	POST	http://rviana-serv.northamerica.contoso.com:80/TestClass/Service.svc/net
00000001956905C0	No	Running	0n200	NA	Not set
00000001956E5360	No	Running	0n401	POST	http://rviana-serv.northamerica.contoso.com:80/TestClass/Service.svc/net
0000000195702098	No	Running	0n200	POST	http://rviana-serv.northamerica.contoso.com:80/TestClass/Service.svc/net

`
<a link id='weval'></a>
*!weval - evaluates expression ad-hoc*
`
!weval <expr1>,..,<expr2>

Where Expression is a valid ad-hoc expression

List type name and field from an object
(mt is the fist pointer in an object layout)
---------------------------------------
0:000> !weval $typefrommt($poi(000000015582a1a0)), $fieldfromobj(000000015582a1a0, "_firstFilename")
calculated: System.Configuration.ConfigurationErrorsException
_firstFilename: C:\inetpub\wwwroot\TestClass\web.config

`
<a link id='wconfig}
*!wconfig - Dump all content of .config files in memory*
`
!wconfig

This is equivalent to this command:
!wfrom -type System.Configuration.SectionXmlInfo -nospace -nofield where (_rawXml) select "<--\nKey: ", _configKey,"\nDefinition Config Path: ",_definitionConfigPath,"\nFilename: ", _filename, "\nLine: ",_lineNumber,"\n -->\n","\n",_rawXml


Dumps all .config lines that can be retrieved*
*------------------------------------------------*
0:00>!wconfig

<--
Key: system.web/membership
Definition Config Path: machine/webroot/3
Filename: C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14\WebServices\Root\web.config
Line: 0n17
 -->

<membership defaultProvider="i">
      <providers>
        <clear />
        <add name="i" type="Microsoft.SharePoint.Administration.Claims.SPClaimsAuthMembershipProvider, Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" />
      </providers>
    </membership>
<--
Key: system.web/roleManager
Definition Config Path: machine/webroot/3
Filename: C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14\WebServices\Root\web.config
Line: 0n23
 -->

<roleManager enabled="true" defaultProvider="c">
      <providers>
        <clear />
        <add name="c" type="Microsoft.SharePoint.Administration.Claims.SPClaimsAuthRoleProvider, Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" />
      </providers>
    </roleManager>
<--
Key: microsoft.sharepoint/serviceApplications
Definition Config Path: machine/webroot/3
Filename: C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14\WebServices\Root\web.config
Line: 0n32
 -->

<serviceApplications applicationId="171fdc63-2115-4975-ad5f-952bf737a7d6" />


`
<a link id='expression'></a>
*!wfrom expression syntax*

`
Boolean (self-describing):
--------------------------

!=
>=
<=
>
<
&& (logical and)
|| (logical or)


Arithmetics by precedence
--------------------------
( )
^ (power 3^2 = 9)
`*`
/
\ (module 5\2 = 1)
+
& (bitwise and 20xff & 0xf = 0xf)
| (bitwise or 0xf | 0x10 = 0x1f)



Examples: 2^0+5*(2+8), field1 - field2 * 5


Strings:


--------
"" - it accepts C++ escapes
+ - concatenate
==
!=
>
<

Examples: "abc"+"def", field1=="rodney", lastname+", "+firstname
`

<a link id='functions'></a>
*Functions*
`
The ability to use functions is the most powerful feature of the extension.
All functions start with '$' sign.
Functions are what enable the data mining operations on the heap.
Below is the list by group.

Syntax:
------------
$<function-name>([<expr1[,...,[exprN]]])


Where:
------------
<exprN> - it is an expression that can be:
	- An integer, pointer or float number literal or expression
	- A string literal or expression
	- A boolean expression or literal (0=false, not 0=true)
	- A field with or without inner fields separated by '.'
	- A function or function expression




General Purpose:
----------------
$sqrt(<expr>) - Return the square root of the expression
$int(<expr>) - Return the integer part of a float number expression
$max(<expr1>, <expr2>) - Return the bigger value of two expressions
$if(<cond>, <expr1>, <expr2>) - If cond is true returns expr1 or expr2 if false
$poi(<expr>) - Read pointer from memory address <expr>
$isnull(<expr1>, <expr2>) - Return <expr2> if expr1 is null or <expr1> otherwise
$modulename(<expr>) - Return module path from module address
$typefrommt(<expr>) - Return type name from the method table in <expr>
$methodfrommd(<expr>) - Return method signature from method definition

Examples:
-----------
0:000> !weval $sqrt(6),5.00/2.00-$int(5.00/2.00), $max(5, 2), $if(5 > 1,"5 is greater than 1", "5 is not  greater than 1"), $poi(0xff366de0), $isnull($poi(0xff366de0), "(NULL)")
calculated: 2.449490
calculated: 0.500000
calculated: 0n5
calculated: 5 is greater than 1
calculated: 00000000000E1780
calculated: 00000000000E1780

0:000> !wdo 0000000195741170
Address: 0000000195741170
String: ns
EEClass: 000007feef80e530
Method Table: 000007feefc07a80
Class Name: System.String
(...)
Module: 000007feef7d1000
(...)

0:000> !weval $typefrommt(000007feefc07a80), $modulename(000007feef7d1000)
calculated: System.String
calculated: C:\Windows\assembly\GAC_64\mscorlib\2.0.0.0__b77a5c561934e089\mscorlib.dll

0:000> !weval $methodfrommd(000007fed9ebbb00)
calculated: System.Web.HttpContext.Unroot()


Interface with Debugger:
-------------------------
$todbgvar(<i>, <expr>) - Set the debugger pseudo register $t<i> to the value of <expr> and
	returns <i>
$dbgeval(<str>) - Return the debugger evaluated integer for the expression in <str>
$dbgrun(<str>) - Run command in <str> and return the string result (it cannot be used to run
	command from the extension like, for example, run a !wfrom inside a !wfrom)
*new* $env(<str>) - Return the environment variable (from PEB)



Example
------------
0:000> !weval $dbgrun("r @ebp"), $todbgvar(1, $dbgeval("@ebp")), $tohexstring($dbgeval("@$t1")), $dbgeval("@@(sizeof(void*))"), $env("USERNAME")
calculated: ebp=ff366de0

calculated: 0n1
calculated: ff366de0
calculated: 0n8
calculated: SPSvc

Xml
----------
*new* $xml(<expr>) - Return the XML-indented version of the Xml string (pretty print)
*new* $xmltree(<expr>) - Return the XML tree version of the Xml string
*new* $html(<expr>) - Html encode a string

Example
------------
0:000> !wfrom -nofield -nospace -fieldname _rawXml where (_rawXml) select "\n",$addr(),"\n", $xml(_rawXml), $xmltree(_rawXml)

00000000FFBB2C80
<serverRuntime>
    <hostTypes>
        <add type="Microsoft.SharePoint.Client.SPClientServiceHost, Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" />
    </hostTypes>
</serverRuntime>
DOCUMENT
  +- ELEMENT "serverRuntime"
    +- ELEMENT "hostTypes"
      +- ELEMENT "add", type: Microsoft.SharePoint.Client.SPClientServiceHost, Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c
(...)

String
-------------
$contains(<str1>, <str2>) - Return 1 if str2 is part of string str1, 0 otherwise
$substr(<str>, <start>, <size>) - returns part of the string str 
	starting at <start> with <size> characters
$wildcardmatch(<str>, <pat>) - Return true if <str> contains pattern <pat> (case insensitive). pattern can use * and ?
$tostring(<expr>) - Return the string representation of the <expr>
$tonumberstring(<expr>) - Return the string from the numeric expression (<expr>)
$toformattednumberstring(<expr>) - Return the comma separated string from the numeric expression (<expr>)
$tohexstring(<expr>) - Return the hexadecimal string from the numeric expression (<expr>)
*new* $val(<str>) - Return the integer representation of the string (not compatible with 0x and 0n)
*new* $strsize(<str>) - Return the number of characters of the string
*new* $replace(<str> <pat>, <new-pat>) - Replace a pattern in a string by another
*new* $split(<str> <pat>, <index>) - Return the ith element of the string split by a pattern
*new* $splitsize(<str> <pat>) - Return the number of elements of the string split by a pattern
*new* $ltrim(<str>) - Remove leading spaces (left trim)
*new* $rtrim(<str>) - Remove trailing spaces (right trim)
*new* $lpad(<str>, <count>) - Add leading spaces (left pad)
*new* $rpad(<str>, <count>) - Add trailing spaces (right pad)
*new* $tokenize(<str> <index>) - Return the ith token from the string
*new* $regex(<str>, <pat>, <replace-pat>) - Replace <str> following the regex pattern <pat> into <replace-pat> ($0 = full regex match, $1=match 1, $n=match n)

Examples
------------
0:000> !weval $contains("abcdefg","cd"), $val("255"), $substr("abcdef",2,2), $wildcardmatch("System.Net.String", "*net*"), $tostring(100), $tonumberstring(100), $toformattednumberstring(100), $tohexstring(100), $strsize("abcdef"), $replace("555-210-1212","-", "."), $split("555-210-1212","-",2), $splitsize("555-210-1212","-"),"["+$ltrim("      1234      ")+"]","["+$rtrim("      1234     ")+"]","["+$lpad("1234",0n10)+"]", "["+$rpad("1234",0n10)+"]"
calculated: 0n1                   $contains("abcdefg","cd")
calculated: 0n255                 $val("255")
calculated: cd                    $substr("abcdef",2,2)
calculated: 0n1                   $wildcardmatch("System.Net.String", "*net*")
calculated: 0n256                 $tostring(100)
calculated: 0n256                 $tonumberstring(100) 
calculated: 256.00                $toformattednumberstring(100)
calculated: 100					  $tohexstring(100)
calculated: 0n6                   $strsize("abcdef")
calculated: 555.210.1212          $replace("555-210-1212","-", ".")
calculated: 1212				  $split("555-210-1212","-",2)
calculated: 0n3					  $splitsize("555-210-1212","-")
calculated: [1234      ]          "["+$ltrim("      1234      ")+"]"
calculated: [      1234]          "["+$rtrim("      1234     ")+"]"
calculated: [      1234]          "["+$lpad("1234",0n10)+"]"
calculated: [1234      ]          "["+$rpad("1234",0n10)+"]"

0:000> !weval $tokenize("token0,token1,token2,,,,,token3    token4",3),$tokenize("token0,token1,token2,,,,,token3    token4",4),$regex("000p1(2000)[3]x400:500","(\\d+)\\D+(\\d+)\\D+(\\d+)\\D+(\\d+)\\D+(\\d+)\\D+(\\d+)","$1,$2,$3,$4,$5,$6 - full match: $0")
calculated: token3  $tokenize("token0,token1,token2,,,,,token3    token4",3)
calculated: token4  $tokenize("token0,token1,token2,,,,,token3    token4",4)
calculated: 000,1,2000,3,400,500 - full match: 000p1(2000)[3]x400:500 $regex("000p1(2000)[3]x400:500","(\\d+)\\D+(\\d+)\\D+(\\d+)\\D+(\\d+)\\D+(\\d+)\\D+(\\d+)","$1,$2,$3,$4,$5,$6 - full match: $0")

For more on regex syntax, see: http://msdn.microsoft.com/en-us/library/az24scfc.aspx
NOTE: this extension is using C++ TR1 regex implementation and differences may occur

Current Object:
------------------
When using !wfrom with a filter every match will be send to the where and select list.
Sometimes you need to have control over the properties of the current object. Note that object functions
	normally take no parameters

$addr() - Address of the current object
$typename() - Type name of the object
$mt() - Method table address of the object
$isobj() - if current object is not value type it returns 1
$isvalue() - if current object is value type it returns 1
$fieldat(<i>) - Return the current object's field with index <i>
$token() - Return metadata token of the current object
$module() - Return module address for the current object's type
$string() - Return first 1024 characters of the string if it is a string object
$chain() - Return the inheritance chain of the type of the current object
$containpointer(<address>) - Return 1 if any of the fields point to <address>
$containfieldoftype(<field-name>) - Return 1 if the object has at least a field with <field-name>
$implement(<type-name>) - Return 1 if the object inherits from <type-name>
$rawobj() - Return the string for Byte[] or Char[] array types


Examples:
0:000> !wfrom -nofield -nospace -obj 00000001559e1fb0 select "Address: ",$addr(),"\nTypename: ",$typename(),"\nMethod Table: ",$mt(), "\nType: ", $if($isobj(), "\nObject","Value"), "\nToken: ",$token(),"\nModule: ",$module(), "\nInherits: ",$chain()
Address: 00000001559E1FB0
Typename: System.Web.HttpContext
Method Table: 000007FEDA232488
Type: Value
Token: 0000000002000064
Module: 000007FED9E41000
Inherits: System.Web.HttpContext System.Object

1 Object(s) listed

0:000> !wfrom -obj 00000001957141a8 select $addr(),$typename(),$rawobj()
calculated: 00000001957141A8
calculated: System.Char[]
calculated: _lm_w3svc_1_root_testclass-1-129641373688819734[2264]

1 Object(s) listed


Array Objects:
-------------------
$isarray() - Return 1 if current object is array or 0 otherwise
$arraysize() - Return the number of items of the current array
$arraystart() - Return the address of the first element of the array
$arrayitemsize() - Return the size of the array element
$items(i) - Return the i-th linear element of the array (range 0 - $arraysize()-1)
$rank() - Return the number of dimensions in the array
$arraydim(i) - Return the number of elements for the i-th dimension of the array


Examples:
------------

0:000> !wfrom -obj 00000001957141a8 select $addr(),$typename(),$isarray(), $arraystart(), $arraysize(), $arrayitemsize()
calculated: 00000001957141A8
calculated: System.Char[]
calculated: 0n1
calculated: 00000001957141B8
calculated: 0n54
calculated: 0n2

0:000> !wfrom -obj 00000001956CC850 select $typename(), $arraysize(), $arraydim(0), $arraydim(1), $items($arraydim(0)*3+4), $rank()
calculated: System.Object[,]
calculated: 0n66 (note: total items)
calculated: 0n6 (note: size of dimension 0)
calculated: 0n11 (note: size of dimension 1)
calculated: 00000001956ce760 (note: non-linear object[3][4] = linear object[22])
calculated: 0n2 (note: number of dimensions)


Field Functions:
----------------
$fieldoffset(<field>) - Return the offset of the field within the object (it does not add the method table
	offset)
$fieldaddress(<field>) - Return the absolute address of the field 
$isstaticfield(<field>) - Return 1 if field is static, 0 otherwise
$isvaluefield(<field>) - Return 1 if field is value type, 0 otherwise
$isobjfield(<field>) - Return 1 if field is object type, 0 otherwise
$fieldtypename(<field>) - Return type name of the field
$fieldmt(<field>) - Return method table of the field
$fieldtoken(<field>) - Return metadata token of the field
$fieldmodule(<field>) - Return module address for the field
$fieldfromobj(<address>, <field-str>) - Return the field defined in the string at <address>
$fieldfrommt(<address>, <mt-address>, <field-str>) - Return the field defined in the string at <address>
	for a value type with Method Table <mt-address>
$rawfield(<field>) - Return the string for Byte[] or Char[] array fields
$toguid(<field>) - Return guid string for a guid field
$enumname(<field>) - Return the enumeration name for the field
*new* $isinstack(<field>) - Return true if field is rooted in stack (you can use $addr() to check current object)
*new* $stackroot(<field>) - Return the list of threads where the field is rooted in stack (you can use $addr() to check current object)
*new* $a(<field>) - Attribute a name to a field or expression (useful to avoid "calculated")
*new* $thread(<field>) - Return the thread id of a managed thread object
*new* $hexstr(<field>) - Return the hex string for Byte[], Char[] or Uint16 array fields (useful to print thumbprint)
*new* $ipaddress(<field>) - Return the ipv4 or ipv6 representation for System.Net.IPAddress fields

Examples:
-------------
0:000> !wfrom -obj 000000015577dc38 select $addr(), $typename(), $fieldtypename(_extenderProviderKey), $toguid(_extenderProviderKey)
calculated: 000000015577DC38
calculated: System.ComponentModel.ReflectTypeDescriptionProvider
calculated: System.Guid
calculated: {5577a7b8-0001-0000-d8a7-775501000000}

1 Object(s) listed

0:000> !wfrom -obj 00000001559e1fb0 select $addr(), $typename(), $rawfield(_request._rawContent._data)
calculated: 00000001559E1FB0
calculated: System.Web.HttpContext
calculated: <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/"><s:Body><GetDataUsingDataContract xmlns="http://tempuri.org/"><composite xmlns:a="http://schemas.datacontract.org/2004/07/" xmlns:i="http://www.w3.org/2001/XMLSchema-instance"><a:BoolValue>false</a:BoolValue><a:StringValue>66</a:StringValue></composite></GetDataUsingDataContract></s:Body></s:Envelope>

0:000> !wfrom -type *.httpcontext where ($isinstack($addr())) select $a("Address  ",$addr()),$a("Thread(s)",$stackroot($addr()))
Address  : 00000001015A5198
Thread(s): 63
Address  : 0000000102281AF8
Thread(s): 52
Address  : 00000001027B9C40
Thread(s): 52
Address  : 00000001030436D0
Thread(s): 65
Address  : 00000001034C99D8
Thread(s): 59
Address  : 0000000103F94698
Thread(s): 65

0:000> !wfrom -type *.HttpContext select $a("Address: ",$addr()),$if(!_thread, "  --",$thread(_thread.DONT_USE_InternalThread))
Address: : 0000000184D58F48
calculated: 49
Address: : 0000000184E1E8D0
calculated:   --
Address: : 0000000184E3DC00
calculated:   --
Address: : 0000000184E46F68
calculated:   --
Address: : 0000000184E52280
calculated: 55
Address: : 0000000184EBF228
calculated: 47
(...)

0:000> !wfrom -type System.Security.Cryptography.X509Certificates.X509Certificate2 select $hexstr(m_serialNumber),$hexstr(m_thumbprint), m_issuerName!wfrom -type System.Security.Cryptography.X509Certificates.X509Certificate2 select $hexstr(m_serialNumber),$hexstr(m_thumbprint), m_issuerName
calculated: 43 6c d2 ff 84 56 9a 40 88 37 ea 67 8e 14 85 27
calculated: 
m_issuerName: CN=SharePoint Root Authority, OU=SharePoint, O=Microsoft, C=US
calculated: 
calculated: f3 7d 4e 05 b1 e4 71 70 02 90 0d 93 77 d6 ea 40 a4 72 79 bd
m_issuerName: NULL
(...)

0:000> !wfrom -nospace -nofield -type System.Net.Sockets.Socket select $ipaddress(m_RightEndPoint.m_Address),":",$replace(m_RightEndPoint.m_Port,"0n","")
10.192.12.3:8731
127.0.0.1:9000

Date and Time:
------------------
$tickstotimespan(<ticks>) - Return the string representation of the time span from ticks
$tickstodatetime(<ticks>) - Return the string representation of date and time from ticks
$timespantoticks(<h>, <m>, <s>) - Return number of ticks for <h>, <m> and <s>
$datetoticks(<year>, <month>, <day>) - Return number of ticks for <year>, <month> and <day>
$now() - Current time or time the dump was captured in ticks

Example:
-------------

0:000> !wfrom -obj 00000001559e1fb0 select $addr(), $typename(), $tickstodatetime($now()), $tickstodatetime(_utcTimestamp.dateData), $tickstotimespan($now()-_utcTimestamp.dateData)
calculated: 00000001559E1FB0
calculated: System.Web.HttpContext
calculated: 10/26/2011 9:20:24 PM
calculated: 10/26/2011 9:18:54 PM
calculated: 00:01:29

1 Object(s) listed



`