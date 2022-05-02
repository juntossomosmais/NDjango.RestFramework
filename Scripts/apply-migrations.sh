#!/usr/bin/env bash
set -ex

PROJECT=WebApplication2/WebApplication2.csproj
BUILD_PROJECT=WebApplication2/WebApplication2.csproj
SQL_CONTEXT_CLASS=WebApplication2.Context.ApplicationDbContext
[[ $@ =~ "-v" || $@ =~ "--verbose" ]] && VERBOSE_PARAM="--verbose" || VERBOSE_PARAM=""

echo "Applying migrations..."
dotnet ef database update --project ${PROJECT} --startup-project ${BUILD_PROJECT} --context ${SQL_CONTEXT_CLASS} $VERBOSE_PARAM
