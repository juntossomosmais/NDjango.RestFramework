#!/bin/bash

set -ex
dotnet ef database update --project /app/WebApplication2/WebApplication2.csproj

dotnet test TestProject1 --configuration Release --logger trx --logger "console;verbosity=normal" --settings "./runsettings.xml"
