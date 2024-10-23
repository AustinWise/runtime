project(bootstrapperoutplace)

add_definitions(-DNATIVEAOT_CONSUME_OUTPLACE)

set(SOURCES
    ../main.cpp
)

add_library(bootstrapperoutplace OBJECT ${SOURCES})

install_bootstrapper_object(bootstrapperoutplace aotsdk)
