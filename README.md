# Sistema de Turnos para Barbería

Proyecto final de **Programación III** — Universidad Abierta para Adultos (UAPA), República Dominicana.

Aplicación web hecha con **ASP.NET Core MVC (.NET 8)**, **Entity Framework Core** y **SQL Server** para administrar turnos, clientes, barberos, servicios, productos, ventas/facturación y reportes (PDF y Excel).

> **Estado del código:** el proyecto fue escrito sin acceso a un SDK de .NET, por lo que **todavía no ha sido compilado ni ejecutado**. Antes de entregarlo, siga los pasos de la sección 4 (incluida la generación de la migración) y ejecute las pruebas (sección 7). Si el compilador reporta algún error, corríjalo y regístrelo en un commit; en la sección 10 hay una lista de puntos a vigilar.

La documentación académica completa (análisis, diagramas, plan de pruebas, bibliografía) está en [`docs/DOCUMENTACION.md`](docs/DOCUMENTACION.md).

---

## 1. Requisitos

| Herramienta | Versión | Nota |
|---|---|---|
| Windows 10/11 | — | |
| Visual Studio 2022 (17.8 o superior) | Community | Cargas de trabajo: **ASP.NET y desarrollo web** y **Almacenamiento y procesamiento de datos** |
| .NET SDK | **8.0** (LTS) | Se instala con Visual Studio 2022 |
| SQL Server | LocalDB (viene con VS) **o** SQL Server Express/Developer local | |
| Git | cualquiera reciente | |
| Conexión a internet | — | La primera vez para restaurar NuGet; además la interfaz carga Bootstrap desde CDN (ver sección 9) |

## 2. Paquetes NuGet (ya declarados en los `.csproj`)

Proyecto web `src/BarberiaTurnos`:

| Paquete | Versión | Uso |
|---|---|---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.10 | Usuarios, roles y contraseñas con hash |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.10 | Acceso a SQL Server |
| Microsoft.EntityFrameworkCore.Design | 8.0.10 | Herramientas de migraciones |
| Microsoft.EntityFrameworkCore.Tools | 8.0.10 | `Add-Migration` / `Update-Database` en Visual Studio |
| QuestPDF | 2024.10.2 | Reportes PDF (licencia *Community*) |
| ClosedXML | 0.102.3 | Reportes Excel (.xlsx) |

Proyecto de pruebas `tests/BarberiaTurnos.Tests`: `xunit` 2.9.2, `xunit.runner.visualstudio` 2.8.2, `Microsoft.NET.Test.Sdk` 17.11.1, `Microsoft.EntityFrameworkCore.Sqlite` 8.0.10 (base de datos en memoria para las pruebas).

Se restauran automáticamente al abrir la solución o con `dotnet restore`. Si una versión exacta ya no estuviera disponible, actualice a la última 8.0.x / estable equivalente.

## 3. Clonar el repositorio

```bash
git clone https://github.com/<usuario>/<repositorio>.git
cd <repositorio>
dotnet restore
```

## 4. Base de datos y primer arranque

### 4.1 Cadena de conexión

Está en `src/BarberiaTurnos/appsettings.json`, clave `ConnectionStrings:DefaultConnection`. Por defecto usa **LocalDB** (marcador de posición; ajústela a su equipo):

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BarberiaTurnosDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Alternativas habituales:

- **SQL Server Express local:** `Server=.\\SQLEXPRESS;Database=BarberiaTurnosDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`
- **SQL Server con usuario/contraseña:** no ponga la contraseña en el repositorio; guárdela con user-secrets:
  `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;User Id=...;Password=...;TrustServerCertificate=True" --project src/BarberiaTurnos`

Para saber si LocalDB está disponible: `sqllocaldb info` (debe listar `MSSQLLocalDB`).

### 4.2 Crear la cuenta de administrador (sin contraseñas en el repositorio)

La aplicación **no trae ninguna contraseña predeterminada**. El administrador se crea al arrancar usando valores que usted define localmente con *user-secrets* (se guardan fuera del repositorio):

```bash
cd src/BarberiaTurnos
dotnet user-secrets set "Seed:AdminEmail" "su-correo@ejemplo.com"
dotnet user-secrets set "Seed:AdminPassword" "UnaClaveFuerte123"
```

La contraseña debe tener mínimo 8 caracteres, con mayúscula, minúscula y número. Cada integrante del equipo define la suya en su propio equipo. En Visual Studio también puede usar clic derecho en el proyecto → *Administrar secretos de usuario*.

### 4.3 Generar la migración inicial (**paso obligatorio una sola vez en el equipo**)

El repositorio incluye el modelo de datos, pero la migración debe generarse con las herramientas de EF Core. **Solo un integrante** (el de base de datos) la genera y la sube a GitHub; los demás la reciben con `git pull`.

Desde la carpeta raíz de la solución:

```bash
dotnet tool install --global dotnet-ef --version 8.0.10
dotnet ef migrations add InitialCreate --project src/BarberiaTurnos --output-dir Data/Migrations
```

En Visual Studio (Consola del Administrador de paquetes, proyecto predeterminado `BarberiaTurnos`):

```powershell
Add-Migration InitialCreate -OutputDir Data/Migrations
```

Luego haga `git add`, `commit` y `push` de la carpeta `src/BarberiaTurnos/Data/Migrations`.

### 4.4 Crear/actualizar la base de datos

La aplicación aplica las migraciones automáticamente al iniciar. Si prefiere hacerlo a mano:

```bash
dotnet ef database update --project src/BarberiaTurnos
```

En entorno *Development* se cargan además datos de demostración no sensibles (servicios, 2 barberos, 2 clientes y 3 productos) porque `appsettings.Development.json` activa `Seed:CargarDatosDemo`. Para desactivarlo, póngalo en `false`.

## 5. Ejecutar

```bash
dotnet dev-certs https --trust        # solo la primera vez
dotnet run --project src/BarberiaTurnos
```

o en Visual Studio: abra `BarberiaTurnos.sln`, proyecto de inicio `BarberiaTurnos`, **F5**. La aplicación abre en `https://localhost:7180`. Inicie sesión con el correo y la contraseña del paso 4.2.

Si no existe el administrador, en la consola aparece una advertencia indicando qué configurar.

## 6. Roles y permisos

| Función | Administrador | Empleado |
|---|:-:|:-:|
| Inicio (dashboard), turnos, clientes, ventas, reportes | ✔ | ✔ |
| Consultar barberos, servicios y productos | ✔ | ✔ |
| Crear/editar/activar barberos y servicios; crear/editar/eliminar productos | ✔ | ✖ |
| Crear usuarios, activar/desactivar usuarios, configurar impuesto | ✔ | ✖ |

Todas las páginas exigen sesión; las acciones administrativas se protegen en el servidor (no solo ocultando botones).

## 7. Pruebas

Automatizadas (xUnit, base SQLite en memoria):

```bash
dotnet test
```

Cubren: turnos (creación, fechas pasadas, solapamientos, cancelación, reprogramación), ventas (stock insuficiente, cálculo de subtotal/impuesto/total, descuento de inventario, precio histórico, anti-duplicado), validaciones de modelos y generación de PDF/Excel. El plan completo de pruebas manuales (13 casos con datos y resultados esperados) está en la sección 15 de la documentación. **Registre los resultados reales usted mismo; este repositorio no incluye resultados de ejecución.**

## 8. Estructura

```
BarberiaTurnos.sln
src/BarberiaTurnos/
  Controllers/   Account, Home, Clientes, Barberos, Servicios, Productos, Turnos, Ventas, Reportes, Usuarios, Configuracion
  Models/        Entidades (Cliente, Barbero, Servicio, Turno, Producto, Venta, DetalleVenta, ConfiguracionSistema, ApplicationUser)
  Data/          ApplicationDbContext, DbInitializer (y Migrations/ cuando se genere)
  Services/      TurnoService, VentaService, ReporteService, DashboardService
  ViewModels/    Modelos de formularios y del dashboard
  Views/         Razor en español (Bootstrap)
  wwwroot/       css/site.css, js/site.js, js/ventas.js
  Program.cs, appsettings.json (sin secretos)
tests/BarberiaTurnos.Tests/   Pruebas xUnit
docs/DOCUMENTACION.md         Documentación académica
```

## 9. Notas técnicas

- **Bootstrap e iconos** se cargan desde un CDN (jsDelivr); se necesita internet al usar la aplicación. Para trabajar sin conexión, descargue `bootstrap.min.css`, `bootstrap.bundle.min.js` y `bootstrap-icons` a `wwwroot/lib/` y cambie las rutas en `Views/Shared/_Layout.cshtml`.
- **Impuesto:** el administrador lo define en *Configuración*. El valor inicial sembrado es 18 %, solo como punto de partida editable; cada factura guarda el porcentaje con el que se emitió. La factura es **interna y demostrativa**, no un comprobante fiscal.
- **Cultura:** se fija `en-US` para que los decimales se escriban con punto (350.50) y no fallen los formularios numéricos.
- **Concurrencia:** turnos y ventas se registran dentro de transacciones (aislamiento *Serializable*) para evitar solapamientos y ventas con stock inconsistente.
- **QuestPDF** usa licencia *Community* (gratuita para proyectos académicos/pequeños). Revise los términos vigentes en <https://www.questpdf.com/license/>.

## 10. Si no compila: puntos a revisar

1. Que esté instalado el SDK de .NET 8 (`dotnet --list-sdks`).
2. Que se hayan restaurado los paquetes (`dotnet restore`) y que haya conexión a NuGet.
3. Que la migración exista (`src/BarberiaTurnos/Data/Migrations`); sin ella la aplicación se detiene con un mensaje explicativo.
4. Que la cadena de conexión apunte a una instancia disponible.
5. Si alguna API de QuestPDF/ClosedXML cambió entre versiones, ajuste `Services/ReporteService.cs` (es el único archivo que las usa).

## 11. Trabajo en equipo con GitHub

**Reparto sugerido**

| Integrante | Responsabilidad | Carpetas principales |
|---|---|---|
| 1 | Backend, turnos y coordinación | `Services/TurnoService.cs`, `Controllers/Turnos*`, `Program.cs`, `Views/Turnos` |
| 2 | Base de datos, productos y ventas | `Models/`, `Data/`, `Services/VentaService.cs`, `Controllers/Productos*`, `Controllers/Ventas*`, `Views/Productos`, `Views/Ventas` |
| 3 | Interfaz, reportes, pruebas y documentación | `Views/Shared`, `wwwroot/`, `Services/ReporteService.cs`, `tests/`, `docs/`, `README.md` |

**Estrategia de ramas**

- `main`: versión estable y entregable. Nadie hace *push* directo.
- `develop`: integración del trabajo.
- `feature/<integrante>-<tema>` (ej. `feature/ana-turnos`): una rama por tarea, creada desde `develop`.

**Flujo diario**

```bash
git checkout develop && git pull
git checkout -b feature/ana-turnos
# ... trabajar ...
git add . && git commit -m "Turnos: validar solapamientos"
git fetch origin && git merge origin/develop     # traer lo de los demás ANTES de subir
git push -u origin feature/ana-turnos
# abrir Pull Request hacia develop; otro integrante lo revisa y lo aprueba
```

**Cómo evitar sobrescribir trabajo ajeno**

1. Haga `pull` de `develop` antes de empezar y antes de abrir cada Pull Request.
2. Cada quien edita principalmente sus carpetas; si necesita tocar un archivo compartido (`_Layout.cshtml`, `Program.cs`, `ApplicationDbContext.cs`), avise por el grupo.
3. Si hay conflictos, resuélvalos en su rama (nunca con `push --force`) y ejecute `dotnet build` y `dotnet test` antes de subir.
4. **Migraciones:** cambios del modelo → solo un integrante a la vez crea la migración (`dotnet ef migrations add NombreDescriptivo ...`), la sube de inmediato y avisa; los demás hacen `pull` y reinician la app (se aplica sola). Si dos personas generan migraciones en paralelo y el archivo `ApplicationDbContextModelSnapshot.cs` entra en conflicto: conserve la versión de `develop`, elimine su migración (`dotnet ef migrations remove`) y vuelva a crearla.
5. Nunca suba `bin/`, `obj/`, contraseñas ni `secrets.json` (el `.gitignore` ya los excluye).
