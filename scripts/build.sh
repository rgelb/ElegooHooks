#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
native_root="$project_root/native"
tools_root="$project_root/.tools"
native_build="$tools_root/native-build/$(uname -s | tr '[:upper:]' '[:lower:]')-$(uname -m)"
vcpkg_root="${VCPKG_ROOT:-$tools_root/vcpkg}"

if ! command -v cmake >/dev/null 2>&1; then
  echo "CMake 3.24+ is required: https://cmake.org/download/" >&2
  exit 1
fi

if [[ ! -f "$vcpkg_root/scripts/buildsystems/vcpkg.cmake" ]]; then
  mkdir -p "$tools_root"
  git clone https://github.com/microsoft/vcpkg.git "$vcpkg_root"
  "$vcpkg_root/bootstrap-vcpkg.sh" -disableMetrics
fi

case "$(uname -s)-$(uname -m)" in
  Linux-x86_64) triplet="x64-linux" ;;
  Darwin-arm64) triplet="arm64-osx" ;;
  Darwin-x86_64) triplet="x64-osx" ;;
  *)
    echo "Unsupported host $(uname -s)-$(uname -m)." >&2
    exit 1
    ;;
esac

cmake \
  -S "$native_root" \
  -B "$native_build" \
  -DCMAKE_BUILD_TYPE="$configuration" \
  -DCMAKE_TOOLCHAIN_FILE="$vcpkg_root/scripts/buildsystems/vcpkg.cmake" \
  -DVCPKG_TARGET_TRIPLET="$triplet"
cmake --build "$native_build" --config "$configuration" --target elegoo_link_bridge

artifact_directory="$native_root/artifacts/$configuration"
mkdir -p "$artifact_directory"
if [[ "$(uname -s)" == "Darwin" ]]; then
  cp "$native_build/bin/libelegoo_link_bridge.dylib" "$artifact_directory/"
else
  cp "$native_build/bin/libelegoo_link_bridge.so" "$artifact_directory/"
fi

dotnet build "$project_root/ElegooHooks.sln" --configuration "$configuration"
dotnet test "$project_root/ElegooHooks.sln" --configuration "$configuration" --no-build

echo "Build complete."
echo "Run console: dotnet run --project src/ElegooLink.EventConsole -c $configuration --no-build"
echo "The WinForms desktop application can be run from a Windows build."
