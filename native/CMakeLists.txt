cmake_minimum_required(VERSION 3.24)
project(elegoo_link_dotnet_bridge LANGUAGES CXX)

include(FetchContent)

set(BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(ENABLE_CLOUD_FEATURES OFF CACHE BOOL "" FORCE)

# Pinned so SDK state/substate values and the managed projection stay reproducible.
FetchContent_Declare(
    elegoolink_sdk
    GIT_REPOSITORY https://github.com/elegoo-repo/elegoo-link.git
    GIT_TAG 46c7b814e055cf9675d58482d79f43d0bd2280da
)
FetchContent_MakeAvailable(elegoolink_sdk)

set_target_properties(elegoolink PROPERTIES POSITION_INDEPENDENT_CODE ON)

add_library(elegoo_link_bridge SHARED src/elegoo_link_bridge.cpp)
target_compile_features(elegoo_link_bridge PRIVATE cxx_std_17)
target_include_directories(
    elegoo_link_bridge
    PRIVATE
        "${elegoolink_sdk_SOURCE_DIR}/include"
)
target_link_libraries(elegoo_link_bridge PRIVATE elegoolink)

if(MSVC)
    # C4100/C4505 originate in no-op and header-local helpers in the upstream SDK.
    target_compile_options(elegoo_link_bridge PRIVATE /utf-8 /W4 /wd4100 /wd4505)
else()
    target_compile_options(elegoo_link_bridge PRIVATE -Wall -Wextra -Wpedantic)
endif()

set_target_properties(
    elegoo_link_bridge
    PROPERTIES
        OUTPUT_NAME "elegoo_link_bridge"
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)
