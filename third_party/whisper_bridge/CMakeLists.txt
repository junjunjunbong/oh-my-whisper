cmake_minimum_required(VERSION 3.14)
project(whisper_bridge C CXX)

# whisper.cpp를 서브디렉토리로 추가
set(BUILD_SHARED_LIBS ON CACHE BOOL "Build whisper as shared library" FORCE)
set(WHISPER_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(WHISPER_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(WHISPER_BUILD_SERVER OFF CACHE BOOL "" FORCE)
add_subdirectory(${CMAKE_CURRENT_SOURCE_DIR}/../whisper.cpp ${CMAKE_CURRENT_BINARY_DIR}/whisper.cpp)

# 브릿지 DLL
add_library(whisper_bridge SHARED whisper_bridge.c)
target_include_directories(whisper_bridge PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/../whisper.cpp/include
)
target_link_libraries(whisper_bridge PRIVATE whisper)

# Windows에서 DLL 이름 접두사 제거
if(WIN32)
    set_target_properties(whisper_bridge PROPERTIES PREFIX "")
endif()
