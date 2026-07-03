# Despensa Isabel - POS Local

Aplicación de punto de venta de escritorio para Windows, orientada a la operación diaria de una despensa o comercio minorista. El sistema permite vender productos por código de barras, gestionar carrito, registrar ventas, administrar productos, compras, inventario, reportes, transferencias pendientes y opciones locales de configuración.

## Descripción del Proyecto

Despensa Isabel es una aplicación POS nativa de Windows construida con **WPF sobre .NET**. La interfaz usa un tema oscuro, navegación lateral y pantallas especializadas para venta, catálogo, compras, inventario, reportes y configuración del sistema.

La arquitectura principal sigue el patrón **MVVM**:

- **Views**: pantallas y diálogos WPF definidos en XAML.
- **ViewModels**: estado de UI, comandos y lógica de presentación, apoyados en `CommunityToolkit.Mvvm`.
- **Services**: operaciones de negocio, acceso a datos, diálogos y persistencia local.
- **Data**: inicialización y conexión SQLite mediante `PosDbContext`.
- **Models**: entidades principales del dominio, como productos, ventas, compras, proveedores e inventario.

El almacenamiento local se implementa con **SQLite** y el acceso a datos se realiza con **Dapper** y `Microsoft.Data.Sqlite`. La aplicación no requiere un servidor de base de datos externo: todo queda guardado en archivos locales del usuario de Windows.

Tecnologías principales:

- WPF / .NET Windows Desktop
- MVVM con `CommunityToolkit.Mvvm`
- SQLite local
- Dapper
- Inyección de dependencias con `Microsoft.Extensions.DependencyInjection`
- LiveCharts para visualizaciones de reportes

## Requisitos Previos

Para compilar y ejecutar el proyecto se necesita:

- Windows 10 o Windows 11.
- Visual Studio Code.
- .NET SDK **10.0** o superior compatible con `net10.0-windows`.
- Soporte de escritorio Windows/WPF incluido en el SDK de .NET para Windows.

Verificar la instalación del SDK:

```powershell
dotnet --info
```

El proyecto apunta a:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
```

Si `dotnet --info` no muestra un SDK 10.0 o superior, instalar la versión correspondiente del SDK antes de compilar.

## Guía de Instalación y Ejecución

Estas instrucciones asumen que se trabaja desde Visual Studio Code usando la terminal integrada.

1. Abrir Visual Studio Code.

2. Abrir la carpeta del proyecto:

```text
C:\Users\Ale\Documents\Codex\2026-07-01\act-a-como-un-desarrollador-senior\outputs\PosLocal
```

3. Abrir la terminal integrada:

```text
Terminal > New Terminal
```

4. Restaurar dependencias NuGet:

```powershell
dotnet restore
```

5. Compilar el proyecto:

```powershell
dotnet build
```

6. Ejecutar la aplicación:

```powershell
dotnet run
```

También se puede ejecutar indicando explícitamente el archivo de proyecto:

```powershell
dotnet run --project .\PosLocal.csproj
```

Para generar una compilación de Release:

```powershell
dotnet build -c Release
```

El ejecutable compilado queda dentro de:

```text
bin\Debug\net10.0-windows\
```

o, para Release:

```text
bin\Release\net10.0-windows\
```

## Gestión de la Base de Datos

La aplicación inicializa automáticamente la base de datos SQLite durante el primer arranque.

En `App.xaml.cs`, al iniciar la aplicación, se registra `IPosDbContext` y luego se ejecuta:

```csharp
InitializeAsync()
```

Ese proceso crea el archivo SQLite si no existe y ejecuta sentencias `CREATE TABLE IF NOT EXISTS` para preparar las tablas principales del sistema, incluyendo categorías, proveedores, productos, ventas, detalles de venta, compras, movimientos de inventario y combos.

El archivo físico de base de datos se guarda en el perfil local del usuario:

```text
%LocalAppData%\DespensaIsabel\pos-local.db
```

En una instalación típica de Windows, la ruta expandida será similar a:

```text
C:\Users\<usuario>\AppData\Local\DespensaIsabel\pos-local.db
```

La configuración local de la aplicación, incluyendo switches de la pantalla `Configuración > Sistema`, se guarda en:

```text
%LocalAppData%\DespensaIsabel\appsettings.json
```

Esto permite que las preferencias del sistema persistan entre sesiones sin depender de una base de datos remota.

## Manual de Usuario de Alto Nivel

### Flujo de venta y carrito con lector de código de barras

1. Iniciar la aplicación y permanecer en la pantalla **Punto de Venta**.
2. Escanear el código de barras del producto con el lector.
3. El sistema procesa el código y busca el producto en la base local.
4. Si el producto existe y tiene stock disponible, se agrega automáticamente al carrito.
5. Si el producto ya estaba en el carrito, se incrementa la cantidad.
6. Revisar el total, descuentos o recargos si corresponde.
7. Presionar **Procesar Venta** o usar el flujo de doble Enter para finalizar.
8. Seleccionar el método de pago en el checkout.

El carrito muestra cantidad, precio unitario, stock y total por línea. Desde cada ítem se puede aumentar, disminuir o eliminar la cantidad.

### Ingreso de peso manual

El flujo de peso se usa para productos configurados como venta por peso.

1. Buscar o escanear un producto marcado como venta por peso.
2. Usar el botón **Ingresar Peso**.
3. Cargar el peso manualmente en gramos o kilos según el diálogo.
4. Confirmar la operación.
5. El sistema calcula el subtotal de la línea según el precio del producto y el peso ingresado.

La disponibilidad de esta función depende del switch **Sistema de balanza** en `Configuración > Sistema`. Si está desactivado, el botón de ingreso de peso queda deshabilitado para evitar ventas por peso manuales.

### Conciliación de transferencias

Las ventas pagadas por transferencia pueden quedar pendientes de validación.

1. Ir a la sección **Validar Transferencias** desde la barra lateral.
2. Revisar la lista de ventas pendientes.
3. Verificar número de venta, fecha, cliente, total y estado.
4. Seleccionar **Aprobar** cuando la transferencia haya sido conciliada.
5. El sistema actualiza el estado de la venta y refresca el listado.

Este flujo separa el registro inicial de la venta de la validación posterior del pago, permitiendo operar ventas por transferencia sin bloquear la caja.

## Configuración del Sistema

La pantalla `Configuración > Sistema` centraliza switches locales para activar o desactivar funcionalidades, como:

- Sistema de caja.
- Guardado de ítems removidos.
- Productos con imágenes.
- Cálculo de precio por costo.
- Configuración de impresión.
- Redondeo de precios por peso o volumen.
- Catálogo online.
- Listas de precios.
- Sistema de balanza.
- Módulo IA.

Cada cambio se guarda inmediatamente en `appsettings.json`, por lo que se conserva al cerrar y volver a abrir la aplicación.

