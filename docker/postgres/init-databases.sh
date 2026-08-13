#!/bin/bash
set -e

echo "Creating FinanceHub microservices databases..."

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE DATABASE financehub_authconsent;
    CREATE DATABASE financehub_itauintegration;
    CREATE DATABASE financehub_mercadopagointegration;
    CREATE DATABASE financehub_interintegration;
    CREATE DATABASE financehub_transactionaggregator;
EOSQL

echo "All FinanceHub databases created successfully."
