# 🚛 Sistema de Control de Balanza y Cartas de Porte (Agro-Industrial)

Este proyecto es una solución de software híbrida diseñada específicamente para optimizar la logística, el pesaje y la descarga de camiones en acopios de grano, molinos y plantas industriales. 

El sistema resuelve problemas críticos de conectividad en zonas rurales mediante un flujo de captura doble (Lector QR para Cartas de Porte Electrónicas y un módulo de IA con OCR inteligente como alternativa offline/contingencia).

---

## 🚀 Características Principales

* **Ingreso Híbrido de CPE:** Escaneo de código QR (vía AFIP) o captura fotográfica con extracción automática de datos mediante **IA (OCR Inteligente)** para contextos de baja conectividad.
* **Gestión de Estados en Tiempo Real:** Flujo de trabajo segmentado por roles (Balanza Entrada ➡️ Playón/Descarga ➡️ Balanza Salida).
* **Panel de Control Interactivos:** Interfaz dinámica en Angular para que los operarios de silos gestionen las prioridades desde dispositivos móviles.
* **Panel de Administración:** Historial completo de pesajes con filtros avanzados por productor, grano, patentes y fechas para auditorías rápidas.

---

## 🛠️ Stack Tecnológico

El proyecto está construido sobre una arquitectura desacoplada y moderna:

* **Backend:** .NET 10.0 / C# (ASP.NET Core MVC)
* **Frontend:** Angular 17+ (SPA con arquitectura reactiva)
* **Persistencia:** SQL Server mediante Entity Framework Core (Code First)
* **Inteligencia Artificial:** Azure AI Document Intelligence / OpenAI API (Mapeo de documentos a JSON estructurado)

---

## 📐 Arquitectura de Datos y Ciclo de Vida

El sistema mitiga errores humanos controlando estrictamente las transacciones del camión a través de los siguientes estados:

```text
[ Camión llega ] ➡️ (Balanza Entrada: Peso Bruto) ➡️ [ En Espera ] ➡️ [ En Descarga (Silos) ] ➡️ [ Listo para Salir ] ➡️ (Balanza Salida: Peso Tara) ➡️ [ Completado ]
