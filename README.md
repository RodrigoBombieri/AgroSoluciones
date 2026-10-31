# 🚛 Sistema de Control de Balanza y Cartas de Porte (Agro-Industrial)

Este proyecto es una solución de software híbrida diseñada específicamente para optimizar la logística, el pesaje y la descarga de camiones en acopios de grano, molinos y plantas industriales. 

El sistema resuelve problemas críticos de conectividad en zonas rurales: la Carta de Porte Electrónica se lee por QR, desde su PDF o con OCR de una foto, todo en el propio equipo, sin internet y sin servicios pagos.

---

## 🚀 Características Principales

* **Lectura de la CPE sin costo:** QR con la cámara (da el CTG), PDF de la CPE (se lee el texto directo) o foto del papel (OCR local con Tesseract.js). Los datos precompletan el formulario y se validan: CUIT con dígito verificador, formato de patente, CTG del QR vs. el impreso, CPE vencida, destino a otra planta y CPE ya ingresada. El operario confirma.
* **Gestión de Estados por Roles:** Balanza Entrada ➡️ Playón/Descarga ➡️ Balanza Salida. Cada operario ve solo las pantallas de su rol.
* **Panel del Playón en Angular:** Pensado para celular/tablet. Muestra los camiones en espera y descargando, se actualiza solo cada 15 segundos y permite asignar silo y registrar observaciones.
* **Dashboard:** Camiones en planta por estado, ingresos y neto del día, demora promedio y alertas de diferencia contra lo declarado en la CPE.
* **Panel de Administración:** Historial de pesajes con filtros por fechas, productor, grano, patente, CPE y estado; exportación a CSV (Excel); ABM de proveedores, productos y usuarios.
* **Validaciones de negocio:** CUIT con dígito verificador, patentes AAA999 / AA999AA, CPE única, un camión no puede estar dos veces en planta, tara menor que bruto y tolerancia de diferencia configurable.
* **Funciona sin internet:** fuentes, íconos, OCR y librerías se sirven desde el propio sitio.

---

## 🛠️ Stack Tecnológico

* **Backend:** .NET 10.0 / C# (ASP.NET Core MVC + Identity con roles)
* **Frontend:** Razor Views + Angular 21 (panel del playón, montado dentro de una vista Razor)
* **Persistencia:** SQL Server mediante Entity Framework Core (Code First, migraciones)
* **Lectura de CPE (en el navegador):** html5-qrcode (QR), pdf.js (texto del PDF), Tesseract.js con idioma español (OCR)

---

## 📐 Ciclo de vida del camión

```text
[ Camión llega ] ➡️ (Balanza Entrada: CPE + Peso Bruto) ➡️ [ En Espera ] ➡️ (Playón: silo) ➡️ [ En Descarga ]
   ➡️ (Playón: fin de descarga) ➡️ [ Listo para Salir ] ➡️ (Balanza Salida: Peso Tara) ➡️ [ Completado ] ➡️ Ticket
```

Todas las transiciones pasan por `Services/PesajeWorkflowService.cs`, que valida el estado actual y registra el usuario y la hora de cada etapa. El peso neto lo calcula SQL Server (columna calculada `PesoBruto - PesoTara`).

| Rol | Pantallas |
|---|---|
| `BalanzaEntrada` | Balanza entrada (lectura de CPE + peso bruto) |
| `Playon` | Panel del playón (Angular) |
| `BalanzaSalida` | Balanza salida (tara) y ticket imprimible |
| `Admin` | Todo, más reportes, correcciones, proveedores, productos y usuarios |

Todos los usuarios logueados ven el dashboard y la lista de camiones en planta. No hay registro público: los usuarios los crea un Admin en **Administración → Usuarios** (desactivar un usuario bloquea su ingreso sin borrar su historial).

---

## ▶️ Puesta en marcha

1. **Connection string:** en `SolucionesAgro/ControlBalanza/appsettings.json` → `ConnectionStrings:DbConnection`.
2. **Ejecutar:** abrir `SolucionesAgro/SolucionesAgro.slnx` en Visual Studio y presionar **F5**. Al arrancar, la app aplica las migraciones pendientes y crea los roles y datos iniciales.
3. **Ingresar** con `admin@admin.com` / `Admin123`.

En entorno **Development** se crean además usuarios de prueba (contraseña `Agro1234`): `entrada@agro.local`, `playon@agro.local` y `salida@agro.local`.

> La cámara para leer el QR solo funciona con **HTTPS** (o `localhost`). Usar el perfil `https` de `launchSettings.json`.

### Lectura de la Carta de Porte

Probado con CPE reales de ARCA:

| Fuente | Qué se obtiene | Tiempo aprox. |
|---|---|---|
| **QR** (cámara) | Solo el **CTG** (11 dígitos): ARCA no pone otros datos en el QR. | instantáneo |
| **PDF original de ARCA** | Todos los datos, leyendo el texto del PDF (exacto, sin OCR). | < 1 s |
| **PDF escaneado o foto** | Todos los datos por OCR. Revisar especialmente CUIT y patente (el OCR puede confundir 3/5/8 y O/0; el CUIT se valida con el dígito verificador). | 5-6 s |

La interpretación del texto está en `Services/Cpe/CpeTextoParser.cs` y los controles contra la base en `Services/Cpe/CpeIngresoService.cs`.

### Parámetros de balanza

En `appsettings.json`, sección `Balanza`:

* `ToleranciaDiferenciaPorcentaje` (por defecto `0.5`): diferencia máxima entre el neto y lo declarado en la CPE antes de marcar alerta.
* `PesoMaximoKg` (por defecto `75000`): tope del peso bruto para detectar errores de tipeo.
* `NumeroPlantaRuca` (por defecto `null`): N° de planta (RUCA) de esta planta en ARCA. Si se carga, al leer una CPE se avisa cuando su destino es otra planta.

### Recompilar el panel Angular

La app ya incluye el panel compilado en `wwwroot/angular-app`. Si se modifica el código de `ClientApp`:

**Dónde:** terminal en la carpeta `SolucionesAgro\ControlBalanza\ClientApp`:

```powershell
npm install      # solo la primera vez o si cambia package.json
npm run build    # compila a ..\wwwroot\angular-app
npm test         # tests del panel (Vitest)
```

---

## 🗂️ Estructura principal

```text
ControlBalanza/
├─ Controllers/          MVC (Pesajes, Reportes, Usuarios, Proveedores, Productos, Home)
│  └─ Api/PlayonController.cs   API JSON del panel del playón (cookie + antiforgery)
├─ Services/             Flujo de estados, reportes, lectura de CPE (QR / PDF / OCR)
├─ Validacion/           CUIT, patentes y CTG
├─ Seguridad/Roles.cs    Nombres de los roles
├─ Areas/Identity/       Login en español (reemplaza al de Identity UI)
├─ Migrations/           Migraciones de EF Core
├─ ClientApp/            Código fuente Angular (panel del playón)
└─ wwwroot/              Estáticos (incluye la build de Angular, fuentes y librerías)
```

---

## 📝 Próximos pasos

* **Integración con el web service de ARCA (WSCPE):** con el CTG se pueden consultar los datos oficiales de la CPE (`consultarCPEAutomotor`) y confirmar el arribo y la descarga con los pesos reales (`confirmarArriboCPE`, `confirmacionDefinitivaCPEAutomotor`). Requiere certificado digital del CUIT de la planta y dar de alta el servicio `wscpe` en ARCA.
