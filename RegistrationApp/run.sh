#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
export ASPNETCORE_ENVIRONMENT=Development
dotnet restore
dotnet build --no-restore
dotnet run --no-build --launch-profile http
