#!/usr/bin/env bash
set -euo pipefail

APP_NAME="staffnex-api"
REPO_URL="${REPO_URL:-https://github.com/jiteshryecsyadav-cmyk/staffnex.git}"
SRC_DIR="/opt/${APP_NAME}-src"
PUBLISH_DIR="/var/www/${APP_NAME}"
ENV_DIR="/etc/${APP_NAME}"
ENV_FILE="${ENV_DIR}/${APP_NAME}.env"
SERVICE_FILE="/etc/systemd/system/${APP_NAME}.service"
NGINX_FILE="/etc/nginx/sites-available/${APP_NAME}"

require_root() {
    if [ "${EUID}" -ne 0 ]; then
        echo "Run this script as root."
        exit 1
    fi
}

detect_os() {
    if [ ! -f /etc/os-release ]; then
        echo "Unsupported OS: /etc/os-release not found."
        exit 1
    fi

    . /etc/os-release
    if [ "${ID}" != "ubuntu" ]; then
        echo "This script currently supports Ubuntu VPS only."
        exit 1
    fi
}

install_dependencies() {
    apt-get update
    apt-get install -y git nginx unzip curl

    if ! dotnet --list-sdks 2>/dev/null | grep -q '^8\.'; then
        curl -fsSL "https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb" -o packages-microsoft-prod.deb
        dpkg -i packages-microsoft-prod.deb
        rm -f packages-microsoft-prod.deb
        apt-get update
        apt-get install -y dotnet-sdk-8.0
    fi
}

sync_repo() {
    if [ -d "${SRC_DIR}/.git" ]; then
        git -C "${SRC_DIR}" fetch origin
        git -C "${SRC_DIR}" reset --hard origin/main
    else
        rm -rf "${SRC_DIR}"
        git clone "${REPO_URL}" "${SRC_DIR}"
    fi
}

publish_app() {
    mkdir -p "${PUBLISH_DIR}"
    dotnet publish "${SRC_DIR}/staffnex.Api/staffnex.Api.csproj" -c Release -o "${PUBLISH_DIR}"
    chown -R www-data:www-data "${PUBLISH_DIR}"
}

escape_env_value() {
    printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

contains_placeholder() {
    case "$1" in
        *YOUR_*|*your_*|*YOUR-*|*your-*|*example*|*EXAMPLE*)
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}

create_env_file() {
    mkdir -p "${ENV_DIR}"

    if [ -f "${ENV_FILE}" ]; then
        echo "Using existing environment file: ${ENV_FILE}"
        return
    fi

    echo "Create production configuration for staffnex API"
    while true; do
        read -r -p "SQL Server connection string: " connection_string
        if [ -z "${connection_string}" ]; then
            echo "Connection string cannot be empty."
            continue
        fi

        if contains_placeholder "${connection_string}"; then
            echo "Connection string still contains placeholder text. Enter the real production value."
            continue
        fi

        break
    done

    jwt_key=""
    while [ "${#jwt_key}" -lt 32 ]; do
        read -r -p "JWT key (minimum 32 characters): " jwt_key
        if [ "${#jwt_key}" -lt 32 ]; then
            echo "JWT key too short. Try again."
            continue
        fi

        if contains_placeholder "${jwt_key}" || printf '%s' "${jwt_key}" | grep -q ';'; then
            echo "JWT key looks invalid. Enter only the JWT secret, not a connection string or placeholder text."
            jwt_key=""
        fi
    done

    read -r -p "JWT issuer [staffnex]: " jwt_issuer
    jwt_issuer="${jwt_issuer:-staffnex}"

    read -r -p "JWT audience [staffnex-clients]: " jwt_audience
    jwt_audience="${jwt_audience:-staffnex-clients}"

    read -r -p "Frontend origin(s), comma-separated, leave blank if not needed now: " cors_origins

    {
        echo "ASPNETCORE_ENVIRONMENT=Production"
        echo "ASPNETCORE_URLS=http://127.0.0.1:5000"
        echo "ConnectionStrings__DefaultConnection=\"$(escape_env_value "${connection_string}")\""
        echo "Jwt__Key=\"$(escape_env_value "${jwt_key}")\""
        echo "Jwt__Issuer=\"$(escape_env_value "${jwt_issuer}")\""
        echo "Jwt__Audience=\"$(escape_env_value "${jwt_audience}")\""
        echo "Jwt__AccessTokenMinutes=60"
        echo "Jwt__RefreshTokenDays=7"
        echo "SeedData__Enabled=false"
    } > "${ENV_FILE}"

    if [ -n "${cors_origins}" ]; then
        index=0
        OLDIFS="${IFS}"
        IFS=','
        for origin in ${cors_origins}; do
            trimmed_origin="$(echo "${origin}" | xargs)"
            if [ -n "${trimmed_origin}" ]; then
                echo "Cors__AllowedOrigins__${index}=\"$(escape_env_value "${trimmed_origin}")\"" >> "${ENV_FILE}"
                index=$((index + 1))
            fi
        done
        IFS="${OLDIFS}"
    fi

    chmod 600 "${ENV_FILE}"
}

install_service() {
    cp "${SRC_DIR}/deploy/hostinger-vps/staffnex-api.service.template" "${SERVICE_FILE}"
    systemctl daemon-reload
    systemctl enable "${APP_NAME}"
    systemctl restart "${APP_NAME}"
}

configure_nginx() {
    read -r -p "Domain name (leave blank to use server IP): " domain_name
    server_name="${domain_name:-$(hostname -I | awk '{print $1}')}"
    server_name="$(echo "${server_name}" | xargs)"

    sed "s/__SERVER_NAME__/${server_name}/g" \
        "${SRC_DIR}/deploy/hostinger-vps/nginx.conf.template" > "${NGINX_FILE}"

    ln -sf "${NGINX_FILE}" "/etc/nginx/sites-enabled/${APP_NAME}"
    rm -f /etc/nginx/sites-enabled/default
    nginx -t
    systemctl enable nginx
    systemctl restart nginx
}

show_status() {
    echo
    echo "Deployment completed."
    echo "Health check: http://$(hostname -I | awk '{print $1}')/health"
    echo "Service status command: systemctl status ${APP_NAME}"
    echo "Service logs command: journalctl -u ${APP_NAME} -n 100 --no-pager"
}

require_root
detect_os
install_dependencies
sync_repo
publish_app
create_env_file
install_service
configure_nginx
show_status