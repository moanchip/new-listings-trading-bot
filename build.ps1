# Step 1: Check if migration exists
Write-Host "Checking for existing migrations..."
$migrationName = "createDB"
$migrations = dotnet ef migrations list | Out-String

if ($migrations -match $migrationName) {
    Write-Host "Migration '$migrationName' already exists. Skipping migration step."
} else {
    # Step 2: Run migrations
    Write-Host "Running migrations..."
    dotnet ef migrations add $migrationName

    # Check if migration was successful
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Migration failed."
        exit 1
    } else {
        Write-Host "Migration '$migrationName' added successfully."
    }
}

# Step 3: Run Docker Compose based on the -dev parameter
$dockerComposeFile = "docker-compose.yml"

# Check if -dev argument is passed
if ($args -contains "-dev") {
    Write-Host "Using development Docker Compose file..."
    $dockerComposeFile = "docker-compose.development.yml"
} else {
    Write-Host "Using production Docker Compose file..."
}

Write-Host "Starting Docker Compose with $dockerComposeFile..."

docker-compose -f $dockerComposeFile up -d --build

# Check if Docker Compose ran successfully
if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker Compose failed to start."
    exit 1
} else {
    Write-Host "Docker Compose started successfully."
}
