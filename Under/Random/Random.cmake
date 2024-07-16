cmake_policy(SET CMP0076 NEW)

function(Random_add_sources target)
    
    target_sources("${target}" PUBLIC
            Random/Random_Generator.h
            Random/Random_Generator.cpp
            Random/Random_MT19937.h
            Random/Random_MT19937.cpp

    )
endfunction()