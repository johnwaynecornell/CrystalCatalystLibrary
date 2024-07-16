# PlatformConfig.cmake

set(ALL_TARGETS_LIST "")

function(isDebug target_name result_var)
    # Check for single-configuration generators
    if(CMAKE_BUILD_TYPE)
        if(CMAKE_BUILD_TYPE STREQUAL "Debug")
            set(${result_var} TRUE PARENT_SCOPE)
        else()
            set(${result_var} FALSE PARENT_SCOPE)
        endif()
    else()
        # For multi-configuration generators, we need to rely on generator expressions.
        # This creates a property on the target which will contain the value "1" if
        # it's a debug build and "0" otherwise.
        set_property(TARGET ${target_name} PROPERTY DEBUG_BUILD $<CONFIG:Debug>)
        get_target_property(is_debug ${target_name} DEBUG_BUILD)
        if(is_debug)
            set(${result_var} TRUE PARENT_SCOPE)
        else()
            set(${result_var} FALSE PARENT_SCOPE)
        endif()
    endif()
endfunction()

function(get_full_target_output_name target output_var)
    # Get OUTPUT_NAME of the target
    get_target_property(OUT_NAME ${target} OUTPUT_NAME)
    if(OUT_NAME STREQUAL "OUT_NAME-NOTFOUND")
        # If OUTPUT_NAME is not set, it defaults to the target name
        set(OUT_NAME ${target})
    endif()

    # Determine the target type
    get_target_property(TARGET_TYPE ${target} TYPE)
    if(TARGET_TYPE STREQUAL "SHARED_LIBRARY")
        set(OUT_NAME "${CMAKE_SHARED_LIBRARY_PREFIX}${OUT_NAME}${CMAKE_SHARED_LIBRARY_SUFFIX}")
    elseif(TARGET_TYPE STREQUAL "STATIC_LIBRARY")
        set(OUT_NAME "${CMAKE_STATIC_LIBRARY_PREFIX}${OUT_NAME}${CMAKE_STATIC_LIBRARY_SUFFIX}")
    elseif(TARGET_TYPE STREQUAL "EXECUTABLE")
        set(OUT_NAME "${CMAKE_EXECUTABLE_PREFIX}${OUT_NAME}${CMAKE_EXECUTABLE_SUFFIX}")
    else()
        message(WARNING "Unsupported target type for target ${target}.")
    endif()

    # Return the result via the output_var argument
    set(${output_var} ${OUT_NAME} PARENT_SCOPE)
endfunction()

function(set_target_output target_name MAJOR MINOR BUILD)
    set(TARGET_VERSION "${MAJOR}.${MINOR}.${BUILD}")
    set(TARGET_OUTPUT_NAME "${target_name}.${TARGET_VERSION}")

    set_target_properties(${target_name} PROPERTIES
            OUTPUT_NAME ${TARGET_OUTPUT_NAME})

    isDebug($target_name ISDEBUG)

    if (ISDEBUG)
        set(PTH "bin/Debug")
    else()
        set(PTH "bin/Release")
    endif()

    set(PTH "${PTH}/${CMAKE_SYSTEM_NAME}/${CMAKE_SYSTEM_PROCESSOR}")

    set(SHORT_NAME ${TARGET_PREFIX}${TARGET_OUTPUT_NAME})

    get_full_target_output_name(${target_name} SHORT_NAME2)

    get_target_property(OUTPUT_DIR ${target_name} OUTPUT_DIRECTORY)
    # get_target_property(OUTPUT_DIR ${target_name} PTH)

    if(OUTPUT_DIR STREQUAL "OUTPUT_DIR-NOTFOUND")
        # If OUTPUT_DIRECTORY is not set, it defaults to CMAKE_CURRENT_BINARY_DIR
        set(OUTPUT_DIR ${CMAKE_CURRENT_BINARY_DIR})
    endif()

    set(TARGET_FULL_PATH "${OUTPUT_DIR}/${SHORT_NAME}.${TARGET_SUFFIX}")

    target_link_directories(${target_name} BEFORE PRIVATE $ENV{NewAge}/C/Libs/${PTH})

    set_target_properties(${target_name} PROPERTIES NewAge_lib_dir $ENV{NewAge}/C/Libs/${PTH})

    add_custom_command(TARGET ${target_name} POST_BUILD
            COMMAND echo "copy ${OUTPUT_DIR}/${SHORT_NAME2} ${CMAKE_SOURCE_DIR}/${PTH}/"
            COMMAND echo "copy ${OUTPUT_DIR}/${SHORT_NAME2} $ENV{NewAge}/C/Libs/${PTH}/"

            COMMAND ${CMAKE_COMMAND} -E make_directory ${CMAKE_SOURCE_DIR}/${PTH}
            COMMAND ${CMAKE_COMMAND} -E make_directory $ENV{NewAge}/C/Libs/${PTH}

            COMMAND ${CMAKE_COMMAND} -E copy ${OUTPUT_DIR}/${SHORT_NAME2} ${CMAKE_SOURCE_DIR}/${PTH}/
            COMMAND ${CMAKE_COMMAND} -E copy ${OUTPUT_DIR}/${SHORT_NAME2} $ENV{NewAge}/C/Libs/${PTH}/
    )


    if (ISDEBUG)
        if(CRYSTAL_PLATFORM STREQUAL "Windows")
        add_custom_command(TARGET ${target_name} POST_BUILD
                COMMAND ${CMAKE_COMMAND} -E copy ${OUTPUT_DIR}/${SHORT_NAME}.pdb ${CMAKE_SOURCE_DIR}/${PTH}/
                COMMAND ${CMAKE_COMMAND} -E copy ${OUTPUT_DIR}/${SHORT_NAME}.pdb $ENV{NewAge}/C/Libs/${PTH}/
        )
        endif()
    endif()


    #
    #
    #            if(NOT CMAKE_SCRIPT_MODE_FILE)
    #        message(STATUS Configure)
    #
    #    else()
    #
    #        add_custom_command(TARGET ${target_name} POST_BUILD
    #                COMMAND ${CMAKE_COMMAND} -E message(STATUS "copy ${TARGET_FULL_PATH} ${PTH}/${SHORT_NAME}") ,
    #                COMMAND ${CMAKE_COMMAND} -E make_directory ${PTH} ,
    #                COMMAND ${CMAKE_COMMAND} -E copy ${TARGET_FULL_PATH} ${PTH}/${SHORT_NAME}
    #        )
    #        message(STATUS Build)
    #    endif()
endfunction()
#
#    get_target_property(TARGET_OUTPUT_DIR ${target_name} RUNTIME_OUTPUT_DIRECTORY)
#    get_target_property(TARGET_OUTPUT_NAME ${target_name} OUTPUT_NAME)
#    get_target_property(TARGET_PREFIX ${target_name} PREFIX)
#    get_target_property(TARGET_SUFFIX ${target_name} SUFFIX)
#
#    set(SHORT_NAME ${TARGET_PREFIX}${TARGET_OUTPUT_NAME}.${TARGET_SUFIX})
#
#    set(TARGET_FULL_PATH "${TARGET_OUTPUT_DIR}/${SHORT_NAME}")
#
#    add_custom_command(TARGET CrystalCatalyst POST_BUILD
#            COMMAND ${CMAKE_COMMAND} -E make_directory ${VERSIONED_DIR}
#            COMMAND ${CMAKE_COMMAND} -E copy ${TARGET_FULL_PATH} ${VERSIONED_DIR}/${SHORT_NAME}
#            COMMAND ${CMAKE_COMMAND} -E copy ${TARGET_FULL_PATH} ${CMAKE_BINARY_DIR}/${SHORT_NAME}
#    )
#
#endfunction()

# Override the add_executable function
function(add_executable target_name)
    _add_executable(${target_name} ${ARGN})
    #set_target_output(target_name)
    list(APPEND ALL_TARGETS_LIST ${target_name})
    set(ALL_TARGETS_LIST ${ALL_TARGETS_LIST} PARENT_SCOPE)
endfunction()
# Override the add_library function
function(add_library target_name)
    _add_library(${target_name} ${ARGN})
    #set_target_output(${target_name})
    list(APPEND ALL_TARGETS_LIST ${target_name})
    set(ALL_TARGETS_LIST ${ALL_TARGETS_LIST} PARENT_SCOPE)
endfunction()

function(consolidate_targets)

endfunction()

# Determine the platform

#elseif(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
#    set(PLATFORM_DIR "linux")
#elseif(${CMAKE_SYSTEM_NAME} MATCHES "Darwin")
#    set(PLATFORM_DIR "macos")
#else()
#    set(PLATFORM_DIR ${CMAKE_SYSTEM_NAME})
#endif()
#
#if(CMAKE_SYSTEM_PROCESSOR MATCHES "amd64|AMD64|x86_64|X86_64")
#    set(ARCH_DIR "x64")
#elseif(CMAKE_SYSTEM_PROCESSOR MATCHES "i386|i486|i586|i686|IA32|x86|X86")
#    set(ARCH_DIR "x86")
#else()
#    set(ARCH_DIR ${CMAKE_SYSTEM_PROCESSOR})  # default to the system processor string
#endif()
#
#
#Set the output directories
#set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/out/${PLATFORM_DIR}/${ARCH_DIR}/bin)
#set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/out/${PLATFORM_DIR}/${ARCH_DIR}/lib)
#set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/out/${PLATFORM_DIR}/${ARCH_DIR}/lib/static)
#
#foreach(OUTPUTCONFIG ${CMAKE_CONFIGURATION_TYPES})
#    string(TOUPPER ${OUTPUTCONFIG} OUTPUTCONFIG)
#    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_${OUTPUTCONFIG} ${CMAKE_BINARY_DIR}/out/${PLATFORM_DIR}/${ARCH_DIR}/bin/${OUTPUTCONFIG})
#    set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_${OUTPUTCONFIG} ${CMAKE_BINARY_DIR}/out/${PLATFORM_DIR}/${ARCH_DIR}/lib/${OUTPUTCONFIG})
#    set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_${OUTPUTCONFIG} ${CMAKE_BINARY_DIR}/out/${PLATFORM_DIR}/${ARCH_DIR}/lib/${OUTPUTCONFIG}/static)
#endforeach(OUTPUTCONFIG CMAKE_CONFIGURATION_TYPES)

function(initPlatformConfig)
    set(CRYSTAL_PLATFORM "${CMAKE_SYSTEM_NAME}" PARENT_SCOPE)
    set(CRYSTAL_ARCH "${CMAKE_SYSTEM_PROCESSOR}" PARENT_SCOPE)

    add_definitions(
            -DCRYSTAL_PLATFORM=${CMAKE_SYSTEM_NAME}
            -DCRYSTAL_ARCH=${CMAKE_SYSTEM_PROCESSOR}
    )

endfunction()

function(link_full_paths target_name)
    set(resulting_paths "")

    get_target_property(lib_dir ${target_name} NewAge_lib_dir)

    foreach(lib_name IN LISTS ARGN)
        list(APPEND resulting_paths "${lib_dir}/lib${lib_name}.a")
    endforeach()

    target_link_libraries(${target_name} ${resulting_paths})
endfunction()
