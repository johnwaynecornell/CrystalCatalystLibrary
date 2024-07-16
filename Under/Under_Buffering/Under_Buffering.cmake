cmake_policy(SET CMP0076 NEW)

function(Under_Buffering_add_sources target)
    
    target_sources("${target}" PUBLIC
            Under_Buffering/BufferHelper.h
            Under_Buffering/BufferHelper.cpp

            Under_Buffering/ReturnBuffer.h
            Under_Buffering/ReturnBuffer.cpp
    )
endfunction()
