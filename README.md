# 🧵 Fábrica de Hilos - Sistema de Gestión Empresarial

Sistema web de gestión empresarial para una fábrica de hilos/textilería, desarrollado con **ASP.NET Core MVC (.NET 8)**.

## 📋 Módulos Disponibles

| Módulo | Descripción |
|--------|-------------|
| 🔐 **Autenticación** | Login con roles: Admin, Gerencia, Supervisor, Trabajador |
| 📊 **Dashboard** | Panel de resumen con métricas y gráficos |
| 📦 **Inventario** | Materia prima y productos terminados |
| 🏭 **Producción** | Órdenes de producción |
| 💰 **Ventas** | Clientes y pedidos |
| 👥 **Recursos Humanos** | Empleados y asistencia |

## ⚙️ Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Entity Framework Core Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

```bash
dotnet tool install --global dotnet-ef
```

## 🚀 Instalación y Ejecución

```bash
# 1. Clonar el repositorio
git clone https://github.com/amatencioc/FabricaHilos.git
cd FabricaHilos

# 2. Restaurar dependencias
dotnet restore

# 3. Aplicar migraciones y crear la base de datos
dotnet ef database update --project FabricaHilos

# 4. Ejecutar la aplicación
dotnet run --project FabricaHilos
```

Luego abrir el navegador en: `https://localhost:5001` o `http://localhost:5000`

## 🔑 Credenciales por Defecto

| Usuario | Contraseña | Rol |
|---------|-----------|-----|
| admin@fabricahilos.com | Admin123! | Admin |

El usuario admin puede crear nuevos usuarios desde **Administración > Registrar Usuario**.

## 📁 Estructura del Proyecto

```
FabricaHilos/
├── Controllers/          # Controladores MVC
├── Data/                 # DbContext de Entity Framework
├── Migrations/           # Migraciones de base de datos
├── Models/               # Modelos de datos
│   ├── Inventario/
│   ├── Produccion/
│   ├── Ventas/
│   └── RecursosHumanos/
├── Views/                # Vistas Razor
├── wwwroot/              # Archivos estáticos (CSS, JS)
├── Program.cs            # Configuración y punto de entrada
└── appsettings.json      # Configuración de la aplicación
```

## 🛠️ Tecnologías Utilizadas

- **Framework:** ASP.NET Core MVC (.NET 8)
- **ORM:** Entity Framework Core con SQLite
- **Autenticación:** ASP.NET Core Identity
- **UI:** Bootstrap 5 + Bootstrap Icons
- **Gráficos:** Chart.js
- **Base de datos:** SQLite (desarrollo) / compatible con SQL Server

## 📊 Roles del Sistema

- **Admin**: Acceso total al sistema
- **Gerencia**: Acceso a todos los módulos + administración
- **Supervisor**: Creación y edición en todos los módulos operativos
- **Trabajador**: Solo lectura de los módulos operativos
