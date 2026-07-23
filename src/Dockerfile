# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy solution and project files first — layer cache busts only on .csproj changes.
COPY BSE.slnx ./
COPY BSE.SharedKernel/BSE.SharedKernel.csproj                               BSE.SharedKernel/
COPY BSE.Infrastructure/BSE.Infrastructure.csproj                           BSE.Infrastructure/
COPY BSE.Host/BSE.Host.csproj                                               BSE.Host/
COPY BSE.Modules.AdnsExport/BSE.Modules.AdnsExport.csproj                   BSE.Modules.AdnsExport/
COPY BSE.Modules.AnimalRelations/BSE.Modules.AnimalRelations.csproj         BSE.Modules.AnimalRelations/
COPY BSE.Modules.AuditLog/BSE.Modules.AuditLog.csproj                       BSE.Modules.AuditLog/
COPY BSE.Modules.Batch/BSE.Modules.Batch.csproj                             BSE.Modules.Batch/
COPY BSE.Modules.BsessIntegration/BSE.Modules.BsessIntegration.csproj       BSE.Modules.BsessIntegration/
COPY BSE.Modules.CaseManagement/BSE.Modules.CaseManagement.csproj           BSE.Modules.CaseManagement/
COPY BSE.Modules.CaseWork/BSE.Modules.CaseWork.csproj                       BSE.Modules.CaseWork/
COPY BSE.Modules.FarmManagement/BSE.Modules.FarmManagement.csproj           BSE.Modules.FarmManagement/
COPY BSE.Modules.OssExport/BSE.Modules.OssExport.csproj                     BSE.Modules.OssExport/
COPY BSE.Modules.ReferenceData/BSE.Modules.ReferenceData.csproj             BSE.Modules.ReferenceData/
COPY BSE.Modules.Search/BSE.Modules.Search.csproj                           BSE.Modules.Search/
COPY BSE.Modules.UserManagement/BSE.Modules.UserManagement.csproj           BSE.Modules.UserManagement/

RUN dotnet restore BSE.slnx

# Copy all source (excluding test projects — they're not needed in the image).
COPY BSE.SharedKernel/          BSE.SharedKernel/
COPY BSE.Infrastructure/        BSE.Infrastructure/
COPY BSE.Host/                  BSE.Host/
COPY BSE.Modules.AdnsExport/    BSE.Modules.AdnsExport/
COPY BSE.Modules.AnimalRelations/ BSE.Modules.AnimalRelations/
COPY BSE.Modules.AuditLog/      BSE.Modules.AuditLog/
COPY BSE.Modules.Batch/         BSE.Modules.Batch/
COPY BSE.Modules.BsessIntegration/ BSE.Modules.BsessIntegration/
COPY BSE.Modules.CaseManagement/ BSE.Modules.CaseManagement/
COPY BSE.Modules.CaseWork/      BSE.Modules.CaseWork/
COPY BSE.Modules.FarmManagement/ BSE.Modules.FarmManagement/
COPY BSE.Modules.OssExport/     BSE.Modules.OssExport/
COPY BSE.Modules.ReferenceData/ BSE.Modules.ReferenceData/
COPY BSE.Modules.Search/        BSE.Modules.Search/
COPY BSE.Modules.UserManagement/ BSE.Modules.UserManagement/

RUN dotnet publish BSE.Host/BSE.Host.csproj \
    --no-restore \
    --configuration Release \
    --output /publish

# ── Runtime stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for container security.
RUN groupadd --system appuser \
    && useradd --system --gid appuser appuser \
    && mkdir -p /app/logs \
    && chown -R appuser:appuser /app

USER appuser

COPY --from=build --chown=appuser:appuser /publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BSE.Host.dll"]
