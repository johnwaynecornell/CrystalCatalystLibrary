// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST__UNDER_H
#define CRYSTALCATALYST__UNDER_H

#include <cstdint>
#include <cstddef>

#define P_INSTANCE(type) type*
#define P_IN_OUT(type) type*
#define P_OUT(type) type*
#define P_ELEMENTS(type) type*

#include "utf8_string.h"
#include "SingleLink_Node.h"
#include "Under_Buffering/ReturnBuffer.h"
#include "Hasher/HasherFactory.h"

#endif //CRYSTALCATALYST__UNDER_H
