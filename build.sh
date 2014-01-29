#!/bin/sh

if [ ! -d "external/json.net" ] || [ ! -d "external/log4net" ]; then
	git submodule init
fi

git submodule update

MONO_IOMAP=case xbuild /p:TargetFrameworkProfile="" /verbosity:minimal backend/backend.csproj
