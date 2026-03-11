# FabricaHilos

Sistema de Gestión para Fábrica de Hilos — ASP.NET Core 8 MVC con Oracle Database.

## Tecnologías

- **Framework:** ASP.NET Core 8 MVC
- **UI:** Razor + Bootstrap 5
- **Base de datos:** Oracle (Oracle.ManagedDataAccess.Core 23.7)
- **Patrón:** Repository Pattern + Dependency Injection

## Módulos

| Módulo      | Descripción                                       |
|-------------|---------------------------------------------------|
| Hilos       | Catálogo de hilos con código, color, stock, precio |
| Materiales  | Control de materias primas e inventario           |
| Clientes    | Registro y administración de clientes             |
| Pedidos     | Órdenes de compra con ítems y seguimiento de estado |

## Configuración

### 1. Base de datos Oracle

Ejecutar el script de creación de tablas:

```sql
-- Ejecutar con privilegios DBA primero:
CREATE USER FABRICA_HILOS IDENTIFIED BY "contraseña_segura";
GRANT CONNECT, RESOURCE TO FABRICA_HILOS;
```

Luego ejecutar `FabricaHilos/Scripts/01_crear_tablas.sql`.

### 2. Cadena de conexión

**Desarrollo (recomendado con User Secrets):**

```bash
cd FabricaHilos
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:OracleConnection" "User Id=FABRICA_HILOS;Password=TuContraseña;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=servidor)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=orcl)));"
```

**Producción:** Configurar la variable de entorno `ConnectionStrings__OracleConnection` o usar un proveedor seguro (Azure Key Vault, AWS Secrets Manager).

### 3. Ejecutar la aplicación

```bash
cd FabricaHilos
dotnet run
```

La aplicación estará disponible en `https://localhost:5001`.

## Estructura del Proyecto

```
FabricaHilos/
├── Controllers/          # Controladores MVC
├── Data/
│   ├── Interfaces/       # Contratos de repositorios
│   ├── Repositories/     # Implementaciones con ADO.NET + Oracle
│   └── OracleDbContext.cs
├── Models/               # Modelos de dominio
├── Views/                # Vistas Razor por módulo
├── Scripts/              # Scripts SQL para Oracle
└── wwwroot/              # Archivos estáticos (Bootstrap, jQuery)
```
