#!/bin/bash

set -ex
dotnet ef database update --project /app/AspNetCore.RestFramework.Sample/AspNetCore.RestFramework.Sample.csproj

dotnet test AspNetCore.RestFramework.Test --configuration Release --logger trx --logger "console;verbosity=normal" --settings "./runsettings.xml"
