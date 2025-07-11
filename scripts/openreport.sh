#!/bin/bash
set -euo pipefail

cd ../validator.Tests/coveragereport

# Open the report in Google Chrome
open -a "Google Chrome" index.html
