#!/usr/bin/env bash

set -eu -o pipefail

# You should start the scanner prior building your project and running your tests
if [ -n "${PR_SOURCE_BRANCH:-}" ]; then
  dotnet sonarscanner begin \
    /d:sonar.login="$SONAR_TOKEN" \
    /k:"juntossomosmais_AspNetCore.RestFramework" \
    /o:"juntossomosmais" \
    /d:sonar.host.url="https://sonarcloud.io" \
    /d:sonar.cs.opencover.reportsPaths="**/*/coverage.opencover.xml" \
    /d:sonar.cs.vstest.reportsPaths="**/*/*.trx" \
    /d:sonar.pullrequest.base="$PR_TARGET_BRANCH" \
    /d:sonar.pullrequest.branch="$PR_SOURCE_BRANCH" \
    /d:sonar.pullrequest.key="$GITHUB_PR_NUMBER"
else
  dotnet sonarscanner begin \
    /d:sonar.login="$SONAR_TOKEN" \
    /v:"$PROJECT_VERSION" \
    /k:"juntossomosmais_AspNetCore.RestFramework" \
    /o:"juntossomosmais" \
    /d:sonar.host.url="https://sonarcloud.io" \
    /d:sonar.cs.opencover.reportsPaths="**/*/coverage.opencover.xml" \
    /d:sonar.cs.vstest.reportsPaths="**/*/*.trx" \
    /d:sonar.branch.name="$SOURCE_BRANCH_NAME"
fi

# Now we can run our tests as usual
./scripts/start-tests.sh

# Now we can collect the results 👍
dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"
