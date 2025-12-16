#!/usr/bin/env bash

set -e

cd -- "$( dirname -- "${BASH_SOURCE[0]}" )"

IMAGE_NAME=ilasm-antlr

docker build -t ${IMAGE_NAME} .
docker run -it --rm -u $(id -u ${USER}):$(id -g ${USER}) -v `pwd`:/work ${IMAGE_NAME} -package ILAssembler -visitor -Dlanguage=CSharp CIL.g4
