.PHONY: build test publish ci-publish

# Variables
PROJECT_NAME = Sorry.SourceGens
PROJECT_PATH = $(PROJECT_NAME)/$(PROJECT_NAME).csproj
VERSION ?= 1.0.0

build:
	@dotnet restore
	@dotnet build --no-restore --configuration Release

test: build
	@dotnet test --no-build --configuration Release --verbosity normal

publish: test
	@dotnet pack $(PROJECT_PATH) --no-build -c Release -p:PackageVersion=$(VERSION) && \
	 dotnet nuget push $(PROJECT_NAME)/bin/Release/$(PROJECT_NAME).$(VERSION).nupkg \
	 -k $(NUGET_KEY) -s https://api.nuget.org/v3/index.json --skip-duplicate || true

ci-publish:
	@$(MAKE) publish VERSION=$(REF_NAME)