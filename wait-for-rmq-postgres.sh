#!/bin/bash
set -e

# Function to wait for a service
wait_for_service() {
  echo "Waiting for $1:$2 to be ready..."
  until nc -z $1 $2; do
    echo "Service $1:$2 unavailable - sleeping"
    sleep 2
  done
  echo "$1:$2 is ready!"
}

# Get services to wait for from environment variables, or use defaults
WAIT_HOSTS=${WAIT_HOSTS:-"rabbitmq:5672,postgres_db:5432"}

echo "Waiting for services: $WAIT_HOSTS"
IFS=',' read -ra HOSTS <<< "$WAIT_HOSTS"
for host in "${HOSTS[@]}"; do
  IFS=':' read -ra HOSTPORT <<< "$host"
  wait_for_service ${HOSTPORT[0]} ${HOSTPORT[1]}
done

# Start the actual service
echo "Starting application..."
exec "$@"