# Company Hierarchy

Aplicación en ASP.NET Core .NET 8 para mostrar y administrar una jerarquía de plazas/empleados.

## Proyectos

- `src/CompanyHierarchy.Api`: Web API REST para la lógica de negocio y acceso a SQL Server.
- `src/CompanyHierarchy.Web`: Frontend MVC que consume la API.
- `database/Setup.sql`: script de creación de base de datos, tabla y stored procedures.

## Requisitos

- .NET 8 SDK
- SQL Server

## Configuración

1. Ejecuta `database/Setup.sql` en SQL Server.

## Ejecución

1. Inicia la API desde `src/CompanyHierarchy.Api`.
2. Inicia la aplicación MVC desde `src/CompanyHierarchy.Web`.
3. Abre la web (usualmente puerto 5051) y usa el árbol para crear, editar y eliminar plazas.

