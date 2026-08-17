#!/bin/bash
set -e

echo "Creating FinanceHub microservices databases..."

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE DATABASE financehub_transactionaggregator;
EOSQL

echo "All FinanceHub databases created successfully."
