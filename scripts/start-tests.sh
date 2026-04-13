#!/bin/bash

set -e

# Run all tests with full output; log is used for failure extraction at the end
dotnet test --configuration Release --logger trx --logger "console;verbosity=normal" --settings "./runsettings.xml" 2>&1 | tee /tmp/dotnet-test.log
EXIT_CODE=${PIPESTATUS[0]}

# Show only failures: extract failure blocks from the log (no Python, container-safe)
if [ "$EXIT_CODE" -ne 0 ]; then
  echo ""
  echo "=== FAILED TESTS ==="
  awk '
    # Start of a failure block: line like "  Failed   TestName [123 ms]" — always print this line (test name)
    /^[[:space:]]*Failed[[:space:]]+[^[:space:]]/ {
      print
      in_fail = 1
      line_count = 1
      next
    }
    in_fail {
      print
      line_count++
      # End of stack trace marker — stop after this line
      if (/End of stack trace from previous location/) { in_fail = 0; print ""; next }
      if (line_count >= 60) { in_fail = 0; print "" }
      if (line_count > 1 && /^[[:space:]]*(Passed|Failed)[[:space:]]+[^[:space:]]/) { in_fail = 0; print "" }
    }
  ' /tmp/dotnet-test.log
fi

# Always print a clear completion summary so you know the run finished
echo ""
echo "--- Tests finished ---"
echo "Exit code: $EXIT_CODE"

exit "$EXIT_CODE"
