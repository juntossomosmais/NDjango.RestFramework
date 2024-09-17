#!/usr/bin/env bash

set -e

dotnet test tests/NDjango.RestFramework.Test \
--configuration Release \
--logger trx \
--logger "console;verbosity=normal" \
--settings "./runsettings.xml"
