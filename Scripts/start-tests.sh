#!/bin/bash

dotnet test TestProject1 --configuration Release --logger trx --logger "console;verbosity=normal" --settings "./runsettings.xml"
