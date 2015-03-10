/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

#ifndef ver_major
#define ver_major 2
#define ver_minor 0
#define ver_release 1
#define ver_build 5000
#define ver_all(a,b,c,d) a,b,c,d
#define ver_expand(s) #s
#define ver_all_string(a,b,c,d) ver_expand(a) "." ver_expand(b) "." ver_expand(c) "." ver_expand(d)
#define ver_date __DATE__
#endif
