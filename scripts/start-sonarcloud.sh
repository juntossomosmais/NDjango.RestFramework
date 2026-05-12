#!/usr/bin/env bash

set -eu -o pipefail

PROJECT_KEY="juntossomosmais_NDjango.RestFramework"
ORGANIZATION="juntossomosmais"
SONAR_SETTINGS_PATH="$(pwd)/sonar-project.xml"

# You should start the scanner prior building your project and running your tests
if [ -n "${PR_SOURCE_BRANCH:-}" ]; then
  dotnet sonarscanner begin \
    /d:sonar.login="$SONAR_TOKEN" \
    /k:"$PROJECT_KEY" \
    /o:"$ORGANIZATION" \
    /d:sonar.host.url="https://sonarcloud.io" \
    /d:sonar.pullrequest.base="$PR_TARGET_BRANCH" \
    /d:sonar.pullrequest.branch="$PR_SOURCE_BRANCH" \
    /d:sonar.pullrequest.key="$GITHUB_PR_NUMBER" \
    /s:"$SONAR_SETTINGS_PATH"
else
  dotnet sonarscanner begin \
    /d:sonar.login="$SONAR_TOKEN" \
    /v:"$PROJECT_VERSION" \
    /k:"$PROJECT_KEY" \
    /o:"$ORGANIZATION" \
    /d:sonar.host.url="https://sonarcloud.io" \
    /d:sonar.branch.name="$SOURCE_BRANCH_NAME" \
    /s:"$SONAR_SETTINGS_PATH"
fi

# Now we can run our tests as usual
./scripts/start-tests.sh

# Now we can collect the results 👍
dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
