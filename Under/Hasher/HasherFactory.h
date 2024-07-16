#ifndef FARMHASH_HASHERFACTORY_H
#define FARMHASH_HASHERFACTORY_H

#include "HasherClass/HasherClass32.h"
#include "HasherClass/HasherClass64.h"
#include "Hasher_PRNG/Hasher_PRNG32.h"
#include "Hasher_PRNG/Hasher_PRNG64.h"
#include "Hasher_BufferedForCallback/Hasher_BufferedForCallback32.h"
#include "Hasher_BufferedForCallback/Hasher_BufferedForCallback64.h"

extern "C"
{
_EXPORT_ P_INSTANCE(HasherClass) HasherFactory_Get(utf8_string_const Name);
}

#endif //FARMHASH_HASHERFACTORY_H
