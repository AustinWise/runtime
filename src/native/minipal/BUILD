load("@rules_cc//cc:cc_library.bzl", "cc_library")

cc_library(
    name = "log",
    srcs = ["log.c"],
    hdrs = ["log.h"],
    visibility = ["//visibility:public"],
    deps = [
        ":minipalconfig",
        ":types",
    ],
)

cc_library(
    name = "types",
    hdrs = ["types.h"],
    include_prefix = "minipal",
)

cc_library(
    name = "minipalconfig",
    hdrs = [":cmake_minipalconfig"],
    includes = ["."],
)

# TODO: Remove dependency on CMake.
genrule(
    name = "cmake_minipalconfig",
    srcs = [
        "minipalconfig.h.in",
        "configure.cmake",
        "CMakeLists.txt",
    ] + glob(["*.c"]),
    outs = ["minipalconfig.h"],
    cmd = "mkdir artifacts && cmake -S " + package_name() + " -B artifacts && cp artifacts/minipalconfig.h $@",
)
