#!/bin/bash
set -e

# Configuration
RESOURCE_GROUP="${RESOURCE_GROUP:-chesster-rg}"
ACR_NAME="${ACR_NAME:-chessteracr}"
CONTAINERAPPS_ENV="${CONTAINERAPPS_ENV:-chesster-env}"
FRONTEND_APP="${FRONTEND_APP:-chesster-frontend}"
BACKEND_APP="${BACKEND_APP:-chesster-api}"
LOCATION="${LOCATION:-eastus}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Check required tools
check_requirements() {
    log_info "Checking requirements..."
    
    if ! command -v az &> /dev/null; then
        log_error "Azure CLI is not installed"
        exit 1
    fi
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    log_info "Requirements check passed"
}

# Login to Azure
azure_login() {
    log_info "Logging in to Azure..."
    az login --output none
    az account set --subscription "$(az account show --query id -o tsv)" || true
}

# Create resource group if not exists
create_resource_group() {
    log_info "Creating resource group if not exists..."
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none 2>/dev/null || true
}

# Create ACR if not exists
create_acr() {
    log_info "Setting up Azure Container Registry..."
    az acr create \
        --resource-group "$RESOURCE_GROUP" \
        --name "$ACR_NAME" \
        --location "$LOCATION" \
        --sku Standard \
        --output none 2>/dev/null || true
    
    az acr login --name "$ACR_NAME" --output none
}

# Build and push images
build_and_push() {
    log_info "Building and pushing Docker images..."
    
    # Build frontend
    log_info "Building frontend image..."
    docker build -f frontend/Dockerfile -t "$ACR_NAME.azurecr.io/frontend:latest" ./frontend
    
    # Build backend
    log_info "Building backend image..."
    docker build -f backend/Chesster.Api/Dockerfile -t "$ACR_NAME.azurecr.io/backend:latest" ./backend
    
    # Push images
    log_info "Pushing images to ACR..."
    docker push "$ACR_NAME.azurecr.io/frontend:latest"
    docker push "$ACR_NAME.azurecr.io/backend:latest"
}

# Deploy to Container Apps
deploy_container_apps() {
    log_info "Deploying to Azure Container Apps..."
    
    # Create environment if not exists
    az containerapp env create \
        --name "$CONTAINERAPPS_ENV" \
        --resource-group "$RESOURCE_GROUP" \
        --output none 2>/dev/null || true
    
    # Set environment variables for backend
    az containerapp env set \
        --name "$BACKEND_APP" \
        --resource-group "$RESOURCE_GROUP" \
        --environment "$CONTAINERAPPS_ENV" \
        --secret "connection-string" "Host=${POSTGRES_HOST};Database=chesster;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}" \
        --secret "jwt-key" "${JWT_KEY:-DefaultSecretKey12345678901234567890}" \
        --output none 2>/dev/null || true
    
    # Deploy backend
    log_info "Deploying backend..."
    az containerapp create \
        --name "$BACKEND_APP" \
        --resource-group "$RESOURCE_GROUP" \
        --environment "$CONTAINERAPPS_ENV" \
        --image "$ACR_NAME.azurecr.io/backend:latest" \
        --target-port 8080 \
        --ingress external \
        --cpu 0.25 --memory 0.5Gi \
        --min-replicas 1 \
        --max-replicas 1 \
        --output none 2>/dev/null || az containerapp update \
        --name "$BACKEND_APP" \
        --resource-group "$RESOURCE_GROUP" \
        --image "$ACR_NAME.azurecr.io/backend:latest" \
        --output none
    
    # Deploy frontend
    log_info "Deploying frontend..."
    az containerapp create \
        --name "$FRONTEND_APP" \
        --resource-group "$RESOURCE_GROUP" \
        --environment "$CONTAINERAPPS_ENV" \
        --image "$ACR_NAME.azurecr.io/frontend:latest" \
        --target-port 80 \
        --ingress external \
        --cpu 0.25 --memory 0.5Gi \
        --min-replicas 1 \
        --max-replicas 1 \
        --output none 2>/dev/null || az containerapp update \
        --name "$FRONTEND_APP" \
        --resource-group "$RESOURCE_GROUP" \
        --image "$ACR_NAME.azurecr.io/frontend:latest" \
        --output none
}

# Main execution
main() {
    log_info "Starting Azure deployment..."
    
    check_requirements
    azure_login
    create_resource_group
    create_acr
    build_and_push
    deploy_container_apps
    
    log_info "Deployment complete!"
    log_info "Frontend URL: https://$(az containerapp show -n $FRONTEND_APP -g $RESOURCE_GROUP --query properties.provisioningState -o tsv 2>/dev/null || echo $FRONTEND_APP.azurecontainerapps.io)"
}

main "$@"