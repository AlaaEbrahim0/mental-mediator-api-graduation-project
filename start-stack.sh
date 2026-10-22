#!/usr/bin/env bash
# start-stack.sh — unified local development workflow.
#
# Spins up the entire Mental Mediator local stack with a single command.
#   ./start-stack.sh up       # build and start (detached)
#   ./start-stack.sh up -d    # same, detached (recommended)
#   ./start-stack.sh down     # tear down
#   ./start-stack.sh logs     # follow logs
#
# The frontend's data-fetching layer targets http://api.local:5184,
# so the API container is exposed on the host at 5184 and advertised as
# api.local inside docker-compose. No port or env conflicts:
#   API  -> http://localhost:5184
#   Frontend -> http://localhost:3000
#   SQL  -> localhost:1433 (SA / YourStrongPassword123!)

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

CMD="${1:-up}"

case "$CMD" in
  up)
    echo "==> Building and starting the Mental Mediator local stack..."
    docker compose up --build -d
    echo ""
    echo "==> Stack is up."
    echo "    API : http://localhost:5184"
    echo "    Web : http://localhost:3000"
    echo "    SQL : localhost:1433 (sa/YourStrongPassword123!)"
    echo "    Logs: ./start-stack.sh logs"
    ;;
  down)
    echo "==> Stopping and removing the local stack..."
    docker compose down
    ;;
  logs)
    docker compose logs -f
    ;;
  *)
    echo "Usage: $0 {up|down|logs}" >&2
    exit 1
    ;;
esac
