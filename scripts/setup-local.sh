#!/bin/sh
set -eu

# Run from repository root through make setup. Never source or print .env.
if [ -e .env ]; then
    echo '.env existente preservado. Execute make config-check para validar.'
    exit 0
fi

command -v openssl >/dev/null 2>&1 || {
    echo 'Instale OpenSSL para gerar segredos locais.' >&2
    exit 1
}

database_password=$(openssl rand -hex 24)
rabbit_password=$(openssl rand -hex 24)
jwt_key=$(openssl rand -hex 48)

# noclobber also protects against another setup creating .env concurrently.
umask 077
set -C
sed \
    -e "s/^POSTGRES_PASSWORD=.*/POSTGRES_PASSWORD=$database_password/" \
    -e "s/^RABBITMQ_PASSWORD=.*/RABBITMQ_PASSWORD=$rabbit_password/" \
    -e "s/^JWT_SIGNING_KEY=.*/JWT_SIGNING_KEY=$jwt_key/" \
    .env.example > .env
echo '.env criado com segredos locais. Execute make config-check e make full.'
