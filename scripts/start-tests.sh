#!/bin/bash

set -e

dotnet ef database update --project /app/samples/AspNetCore.RestFramework.Sample/AspNetCore.RestFramework.Sample.csproj

dotnet test tests/AspNetCore.RestFramework.Core.Test \
--configuration Release \
--logger trx \
--logger "console;verbosity=normal" \
--settings "./runsettings.xml"
