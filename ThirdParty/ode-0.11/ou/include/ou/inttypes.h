/*************************************************************************
 *                                                                       *
 * ODER's Utilities Library. Copyright (C) 2008 Oleh Derevenko.          *
 * All rights reserved.  e-mail: odar@eleks.com (change all "a" to "e")  *
 *                                                                       *
 * This library is free software; you can redistribute it and/or         *
 * modify it under the terms of EITHER:                                  *
 *   (1) The GNU Lesser General Public License as published by the Free  *
 *       Software Foundation; either version 3 of the License, or (at    *
 *       your option) any later version. The text of the GNU Lesser      *
 *       General Public License is included with this library in the     *
 *       file LICENSE-LESSER.TXT. Since LGPL is the extension of GPL     *
 *       the text of GNU General Public License is also provided for     *
 *       your information in file LICENSE.TXT.                           *
 *   (2) The BSD-style license that is included with this library in     *
 *       the file LICENSE-BSD.TXT.                                       *
 *   (3) The zlib/libpng license that is included with this library in   *
 *       the file LICENSE-ZLIB.TXT                                       *
 *                                                                       *
 * This library is distributed WITHOUT ANY WARRANTY, including implied   *
 * warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.      *
 * See the files LICENSE.TXT and LICENSE-LESSER.TXT or LICENSE-BSD.TXT   *
 * or LICENSE-ZLIB.TXT for more details.                                 *
 *                                                                       *
 *************************************************************************/

#ifndef __OU_INTTYPES_H_INCLUDED
#define __OU_INTTYPES_H_INCLUDED


#include <ou/namespace.h>
#include <ou/platform.h>


BEGIN_NAMESPACE_OU();

/*
 *	Implementation Note:
 *	Standard [u]int??_t type names are avoided to prevent possibility
 *	of conflict with system types which might be the preprocessor defines.
 *	If you know that all your target platforms do not use defines for
 *	integer types, you can typedef them to convenient names after inclusion
 *	of "ou" library.
 */


#if _OU_COMPILER == _OU_COMPILER_MSVC

typedef __int8 int8ou;
typedef unsigned __int8 uint8ou;

typedef __int16 int16ou;
typedef unsigned __int16 uint16ou;

typedef __int32 int32ou;
typedef unsigned __int32 uint32ou;

typedef __int64 int64ou;
typedef unsigned __int64 uint64ou;


#else // #if _OU_TARGET_OS != _OU_TARGET_OS_WINDOWS


END_NAMESPACE_OU();


#include <inttypes.h>

typedef int8_t __ou_global_int8;
typedef uint8_t __ou_global_uint8;

typedef int16_t __ou_global_int16;
typedef uint16_t __ou_global_uint16;

typedef int32_t __ou_global_int32;
typedef uint32_t __ou_global_uint32;

typedef int64_t __ou_global_int64;
typedef uint64_t __ou_global_uint64;


BEGIN_NAMESPACE_OU();


typedef ::__ou_global_int8 int8ou;
typedef ::__ou_global_uint8 uint8ou;

typedef ::__ou_global_int16 int16ou;
typedef ::__ou_global_uint16 uint16ou;

typedef ::__ou_global_int32 int32ou;
typedef ::__ou_global_uint32 uint32ou;

typedef ::__ou_global_int64 int64ou;
typedef ::__ou_global_uint64 uint64ou;


#endif // #if _OU_TARGET_OS == ...


#define OU_BITS_IN_BYTE		8

#define OU_UINT8_BITS		(sizeof(uint8ou) * OU_BITS_IN_BYTE)
#define OU_INT8_BITS		(sizeof(int8ou) * OU_BITS_IN_BYTE)
#define OU_UINT8_MAX		((uint8ou)(-1))
#define OU_UINT8_MIN		((uint8ou)0)
#define OU_INT8_MAX			((int8ou)(OU_UINT8_MAX >> 1))
#define OU_INT8_MIN			((int8ou)(OU_UINT8_MAX - OU_INT8_MAX))

#define OU_UINT16_BITS		(sizeof(uint16ou) * OU_BITS_IN_BYTE)
#define OU_INT16_BITS		(sizeof(int16ou) * OU_BITS_IN_BYTE)
#define OU_UINT16_MAX		((uint16ou)(-1))
#define OU_UINT16_MIN		((uint16ou)0)
#define OU_INT16_MAX		((int16ou)(OU_UINT16_MAX >> 1))
#define OU_INT16_MIN		((int16ou)(OU_UINT16_MAX - OU_INT16_MAX))

#define OU_UINT32_BITS		(sizeof(uint32ou) * OU_BITS_IN_BYTE)
#define OU_INT32_BITS		(sizeof(int32ou) * OU_BITS_IN_BYTE)
#define OU_UINT32_MAX		((uint32ou)(-1))
#define OU_UINT32_MIN		((uint32ou)0)
#define OU_INT32_MAX		((int32ou)(OU_UINT32_MAX >> 1))
#define OU_INT32_MIN		((int32ou)(OU_UINT32_MAX - OU_INT32_MAX))

#define OU_UINT64_BITS		(sizeof(uint64ou) * OU_BITS_IN_BYTE)
#define OU_INT64_BITS		(sizeof(int64ou) * OU_BITS_IN_BYTE)
#define OU_UINT64_MAX		((uint64ou)(-1))
#define OU_UINT64_MIN		((uint64ou)0)
#define OU_INT64_MAX		((int64ou)(OU_UINT64_MAX >> 1))
#define OU_INT64_MIN		((int64ou)(OU_UINT64_MAX - OU_INT64_MAX))


END_NAMESPACE_OU();


#endif // #ifndef __OU_INTTYPES_H_INCLUDED
