cmake_policy(SET CMP0076 NEW)

function(Hasheradd_sources target)

    target_sources("${target}" PUBLIC
            Hasher/HasherFactory.cpp
            Hasher/HasherFactory.h

            Hasher/HasherClass/HasherClass.cpp
            Hasher/HasherClass/HasherClass.h

            Hasher/HasherClass/HasherClass32.cpp
            Hasher/HasherClass/HasherClass32.h

            Hasher/HasherClass/HasherClass64.cpp
            Hasher/HasherClass/HasherClass64.h

            Hasher/Hasher_PRNG/Hasher_PRNG32.cpp
            Hasher/Hasher_PRNG/Hasher_PRNG32.h

            Hasher/Hasher_PRNG/Hasher_PRNG64.cpp
            Hasher/Hasher_PRNG/Hasher_PRNG64.h

            Hasher/Hasher_BufferedForCallback/Hasher_BufferedForCallback32.cpp
            Hasher/Hasher_BufferedForCallback/Hasher_BufferedForCallback32.h

            Hasher/Hasher_BufferedForCallback/Hasher_BufferedForCallback64.cpp
            Hasher/Hasher_BufferedForCallback/Hasher_BufferedForCallback64.h
    )
endfunction()