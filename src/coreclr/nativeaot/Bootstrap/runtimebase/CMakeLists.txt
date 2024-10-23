project(bootstrapperruntimebase)

set(SOURCES
    ../runtimebase.cpp
)

add_library(bootstrapperruntimebase STATIC ${SOURCES})

install_bootstrapper_object(bootstrapperruntimebase aotsdk)

if (CLR_CMAKE_TARGET_WIN32)
  add_library(bootstrapperruntimebase.GuardCF STATIC ${SOURCES})
  install_bootstrapper_object(bootstrapperruntimebase.GuardCF aotsdk)
  set_target_properties(bootstrapperruntimebase.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)
endif()
