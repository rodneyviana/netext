Page at GitHub:
==================
https://github.com/rodneyviana/netext

**** The GitHub page includes the full help

Getting Started:
==================
No matter whether you are a WinDbg debugger already or not, check this out:
http://blogs.msdn.com/b/rodneyviana/archive/2015/03/10/getting-started-with-netext.aspx

Caution:
=================
Be aware that you need WinDbg 32-bits to debug managed 32-bit process or dump file (even if running in a 64-bits OS)
Of course, you'll need WinDbg 64-bits to debug managed 64-bit process or dump file (your OS must also be 64-bits)

Installing:
==================
Copy the contents of 64Bits folder (\x64) to your 64-bits version WinDBG
Copy the contents of 32Bits folder (\x86) to your 32-bits version WinDBG

In doubt, run in WinDBG after loading the extension:
====================================================
.browse !whelp

You need to set the symbol path appropriatelly:
==============================================
See http://support.microsoft.com/kb/311503


If you are having problems finding the correct .NET symbol file, try to run this from WinDBG:
=============================================================================================
.sympath SRV*c:\localsymbols*http://msdl.microsoft.com/download/symbols

*** Create the folder c:\localsymbols first


For "canned" advanced queries:
==============================
.cmdtree netext.tl

*** If the extension is not in your WinDBG folder, use the full path


For questions to the developer:
=======================================
http://netext.codeplex.com/discussions

Full training:
=======================================
http://netext.codeplex.com/documentation
