#!/usr/bin/env bash
# Compares the hash of the Codex generators' files from a build with the committed hash. The hash is the name of the
# <hash>.sha256 file in src/FellowshipSim.Core/Generated/FellowshipSim.Codex.Generators/, which FellowshipSim.Core's build
# writes. A difference is a Codex export change, or a generator change committed without its files.
#
#     tools/check-codex-generated.sh clear    # before the build: the build writes every file again
#     tools/check-codex-generated.sh check    # after the build: fails when the build's hash is not the committed hash
set -euo pipefail

cd "$(dirname "$0")/.."
folder=src/FellowshipSim.Core/Generated/FellowshipSim.Codex.Generators

case "${1:-}" in
  clear)
    rm -rf "$folder"
    ;;
  check)
    committed=$(git ls-files -- "$folder/*.sha256")
    built=$(ls "$folder"/*.sha256 2>/dev/null || true)
    if [ -n "$committed" ] && [ "$(basename "$committed")" = "$(basename "$built")" ]; then
      echo "The Codex generators' files match the committed files: $(basename "$built" .sha256)"
      exit 0
    fi
    message="The Codex generators' files differ from the committed files. Run tools/download-codex-export.sh, delete $folder, build, and commit the folder."
    echo "::error::$message"
    echo "Committed: ${committed:-none}"
    echo "Built:     ${built:-none}"
    # The files whose hash changed, from the two listings.
    changes=$(diff <(git show "HEAD:$committed" 2>/dev/null || true) <(cat "$built" 2>/dev/null || true) || true)
    echo "$changes"
    if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
      {
        echo "### $message"
        echo '```'
        echo "$changes"
        echo '```'
      } >> "$GITHUB_STEP_SUMMARY"
    fi
    exit 1
    ;;
  *)
    echo "usage: $0 clear|check" >&2
    exit 2
    ;;
esac
