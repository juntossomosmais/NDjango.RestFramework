#!/bin/bash

set -e

dotnet test tests/AspNetCore.RestFramework.Core.Test \
--configuration Release \
--logger trx \
--logger "console;verbosity=normal" \
--settings "./runsettings.xml"
